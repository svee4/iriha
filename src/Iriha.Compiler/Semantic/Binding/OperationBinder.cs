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

		var visitor = new Visitors.ExpressionVisitor<IOperation>
		{
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
				var ident = _binder.IdentHelper.FromIdentifier(expr.Identifier);

				if (_binder.SymbolManager.LocalVariablesManager.TryGet(expr.Identifier.Value, out var sym))
				{
					return new LocalVariableReferenceOperation(sym);
				}
				else if (_binder.SymbolManager.GlobalVariableTable.TryGet(ident, out var sym2))
				{
					return new GlobalVariableReferenceOperation(sym2);
				}
				else
				{
					return _binder.SymbolManager.GlobalStructTable.TryGet(ident, out var sym3)
						? new StructReferenceOperation(sym3)
						: _binder.SymbolManager.GlobalTraitTable.TryGet(ident, out var sym4)
											? new TraitReferenceOperation(sym4)
											: _binder.SymbolManager.GlobalFunctionTable.TryGet(ident, out var sym5)
																? (IOperation)new FunctionReferenceOperation(sym5)
																: throw new SyntaxTreeBinder.CouldNotBindTypeException(expr.Identifier.ToString());
				}
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

			BitwiseAndExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new BitwiseAndOperation(l, r)),
			BitwiseOrExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new BitwiseOrOperation(l, r)),
			BitwiseXorExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new BitwiseXorOperation(l, r)),
			BitwiseLeftShiftExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new BitwiseLeftShiftOperation(l, r)),
			BitwiseRightShiftExpressionVisitor = (expr, v) => v.BinaryVisit(expr, (l, r) => new BitwiseRightShiftOperation(l, r)),

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
