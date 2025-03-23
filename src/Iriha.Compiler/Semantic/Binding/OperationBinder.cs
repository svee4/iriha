using Iriha.Compiler.Infra;
using Iriha.Compiler.Parsing.Nodes;
using System.Diagnostics;

namespace Iriha.Compiler.Semantic.Binding;

public sealed class OperationBinder(SyntaxTreeBinder binder)
{
	private readonly SyntaxTreeBinder _binder = binder;

	public IOperation ParseOperation(IExpression expr)
	{
		using var scope = _binder.Logger.BeginScope(nameof(ParseOperation));

		// implicit variables from pipes are stored in this
		// because it needs specific functionality
		// because the variables of previous pipes should not be accessible in a nested pipe.
		// scuffed.
		var pipeVariablesManager = new Stack<LocalVariableTable>();

		var visitor = new Visitors.ExpressionVisitor<IOperation>
		{
			PipeExpressionVisitor = (expr, v) =>
			{
				var source = v.Visit(expr.From);
				var table = new LocalVariableTable(null);
				pipeVariablesManager.Push(table);

				try
				{
					// create implicit variables and push to be in scope when binding the destination expression
					var sourceLocalIdent = IdentHelper.ForLocalVariable("$0");
					var sourceLocalSymbol = new LocalVariableSymbol(sourceLocalIdent, source.Type, VariableModifiers.None);
					table.Add(sourceLocalSymbol);

					if (source.Type is TupleTypeReferenceSymbol tuple)
					{
						foreach (var (index, value) in tuple.Values.Index())
						{
							var ident = IdentHelper.ForLocalVariable("$" + (index + 1));
							var symbol = new LocalVariableSymbol(ident, value, VariableModifiers.None);
							table.Add(symbol);
						}
					}

					var dest = v.Visit(expr.To);
					return new PipeOperation(source, dest);
				}
				finally
				{
					_ = pipeVariablesManager.Pop();
				}
			},

			TupleCreationExpressionVisitor = (expr, v) =>
				new TupleCreationOperation([.. expr.Expressions.Select(v.Visit)]),

			FunctionCallExpressionVisitor = (expr, v) =>
			{
				IFunctionSymbol function;
				switch (expr.FunctionExpression)
				{
					case IdentifierExpression idexpr:
					{
						var functionIdent = _binder.IdentHelper.FromIdentifier(idexpr.Identifier);
						function = _binder.SymbolManager.GlobalFunctionTable.Get(functionIdent);
						break;
					}
					case MemberAccessExpression memxpr:
					{
						var op = (MemberAccessOperation)v.Visit(memxpr);
						function = (StructMethodSymbol)op.Member;
						break;
					}
					case { } ex: throw new UnreachableException($"Unhandled function call expression type {expr.GetType()}");
					case null: throw new InvalidOperationException("Function call expression argument must not be null");
				}

				var args = expr.Arguments.Select(arg => v.Visit(arg.Value)).ToList();
				return new InvocationOperation(function, args);
			},
			AssignmentExpressionVisitor = (expr, v) => new AssignmentOperation(v.Visit(expr.Target), v.Visit(expr.Value)),
			MemberAccessExpressionVisitor = (expr, v) =>
			{
				var source = v.Visit(expr.Source);

				var sourceTypeIdent = GetUnderlyingGlobalType(source.Type);

				static GlobalTypeReferenceSymbol GetUnderlyingGlobalType(TypeReferenceSymbol symbol) =>
					symbol switch
					{
						GlobalTypeReferenceSymbol g => g,
						GenericTypeReferenceSymbol g => GetUnderlyingGlobalType(g.UnderlyingType),
						PointerTypeReferenceSymbol p => GetUnderlyingGlobalType(p.UnderlyingType),
						var x => throw new InvalidOperationException($"Type {x?.GetType()} does not have members")
					};

				var sourceSymbol = _binder.SymbolManager.GlobalStructTable.Get(sourceTypeIdent.Ident);

				var targetIdentName = expr.Member.Identifier.Value;
				var targetIdent = new MemberSymbolIdent(targetIdentName, sourceSymbol.Ident);

				IMemberSymbol member;
				if (sourceSymbol.Fields.FirstOrDefault(f => f.Ident == targetIdent) is { } field)
				{
					member = field;
				}
				else if (sourceSymbol.Methods.FirstOrDefault(m => m.Ident == targetIdent) is { } method)
				{
					member = method;
				}
				else
				{
					throw new InvalidOperationException(
						$"Struct {sourceSymbol.Ident} does not have a member named {targetIdent}");
				}

				return new MemberAccessOperation(source, member);
			},
			StringLiteralExpressionVisitor = (expr, v) =>
			{
				var stringSymbol = _binder.SymbolManager.GlobalStructTable.Get(GlobalSymbolIdent.CoreLib("String"));
				var stringSymbolRef = new StructTypeReferenceSymbol(stringSymbol);
				return new StringLiteralOperation(expr.Value, stringSymbolRef);
			},
			IntLiteralExpressionVisitor = (expr, v) =>
			{
				var intSymbol = _binder.SymbolManager.GlobalStructTable.Get(GlobalSymbolIdent.CoreLib("Int32"));
				var intSymbolRef = new StructTypeReferenceSymbol(intSymbol);
				return new IntegerLiteralOperation(expr.Value, intSymbolRef);
			},

			IdentifierExpressionVisitor = (expr, v) =>
			{
				var localIdent = IdentHelper.ForLocalVariable(expr.Identifier.Value);
				var globalIdent = _binder.IdentHelper.FromIdentifier(expr.Identifier);

				if (pipeVariablesManager.TryPeek(out var table)
					&& table.TryGet(localIdent, out var sym0))
				{
					return new LocalVariableReferenceOperation(sym0);
				}
				else if (_binder.SymbolManager.LocalVariablesManager.TryGet(localIdent, out var sym1))
				{
					return new LocalVariableReferenceOperation(sym1);
				}
				else if (_binder.SymbolManager.GlobalVariableTable.TryGet(globalIdent, out var sym2))
				{
					return new GlobalVariableReferenceOperation(sym2);
				}
				else if (_binder.SymbolManager.GlobalStructTable.TryGet(globalIdent, out var sym3))
				{
					return new StructReferenceOperation(sym3);
				}
				else if (_binder.SymbolManager.GlobalTraitTable.TryGet(globalIdent, out var sym4))
				{
					return new TraitReferenceOperation(sym4);
				}
				else if (_binder.SymbolManager.GlobalFunctionTable.TryGet(globalIdent, out var sym5))
				{
					return new FunctionReferenceOperation(sym5);
				}

				throw new SyntaxTreeBinder.CouldNotBindTypeException(expr.Identifier.ToString());
			},

			IndexerCallExpressionVisitor = (expr, v) =>
				throw new NotImplementedException("Indexers are not supported (yet)"),

			BlockExpressionVisitor = (expr, v) =>
			{
				List<IOperation> operations = [];
				TypeReferenceSymbol? type = null;

				foreach (var st in expr.Statements)
				{
					var op = v.Visit(st);
					operations.Add(op);

					if (op is YieldOperation yieldOp)
					{
						if (type is null)
						{
							type = yieldOp.Type;
						}
						else
						{
							if (yieldOp.Type != type)
							{
								throw new InvalidOperationException($"Expected type {type}, got type {yieldOp.Type}");
							}
						}
					}
				}

				return type is null
					? throw new InvalidOperationException("Block expression must yield")
					: (IOperation)new BlockExpressionOperation(operations, type);
			},
			DiscardExpressionVisitor = (expr, v) => new DiscardOperation(v.Visit(expr)),
			ReturnExpressionVisitor = (expr, v) => new ReturnOperation(expr.Value is { } val ? v.Visit(val) : null),
			YieldExpressionVisitor = (expr, v) => new YieldOperation(expr.Value is { } val ? v.Visit(val) : null),

			VariableDeclarationStatementVisitor = (expr, v) => _binder.ParseLocalVariableDeclaration(expr, v.Visit),

			AdditionExpressionVisitor = (expr, v) => new AdditionOperation(v.Visit(expr.Left), v.Visit(expr.Right)),
			SubtractionExpressionVisitor = (expr, v) => new SubtractionOperation(v.Visit(expr.Left), v.Visit(expr.Right)),
			MultiplicationExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new MultiplicationOperation(l, r)),
			DivisionExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new DivisionOperation(l, r)),

			LogicalAndExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new LogicalAndOperation(l, r)),
			LogicalOrExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new LogicalOrOperation(l, r)),
			LogicalLessThanExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new LogicalLessThanOperation(l, r)),
			LogicalGreaterThanExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new LogicalGreaterThanOperation(l, r)),
			LogicalEqualExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new LogicalEqualOperation(l, r)),
			LogicalNotEqualExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new LogicalNotEqualOperation(l, r)),

			LogicalNegationExpressionVisitor = (expr, v) =>
			{
				var op = v.Visit(expr.Source);
				return new LogicalNegationOperation(op, op.Type);
			},
			MathematicalNegationExpressionVisitor = (expr, v) =>
			{
				var op = v.Visit(expr.Source);
				return new MathematicalNegationOperation(op, op.Type);
			},
			BitwiseNotExpressionVisitor = (expr, v) =>
			{
				var op = v.Visit(expr.Source);
				return new BitwiseNotOperation(op, op.Type);
			},
			DereferenceExpressionVisitor = (expr, v) =>
			{
				var op = v.Visit(expr.Source);
				return new DereferenceOperation(op, op.Type);
			},
		};

		return visitor.Visit(expr);
	}
}
