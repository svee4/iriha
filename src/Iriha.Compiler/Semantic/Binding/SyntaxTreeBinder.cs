using Iriha.Compiler.Infra;
using Iriha.Compiler.Parsing;
using Iriha.Compiler.Parsing.Nodes;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Iriha.Compiler.Semantic.Binding;

public sealed class SyntaxTreeBinder
{
	public ILogger<SyntaxTreeBinder> Logger { get; }
	public Compilation Compilation { get; }
	public SymbolTableManager SymbolManager { get; }
	public IdentHelper IdentHelper { get; }

	public SyntaxTreeBinder(Compilation compilation, ILogger<SyntaxTreeBinder> logger)
	{
		Compilation = compilation;
		Logger = logger;
		SymbolManager = new SymbolTableManager(this);
		IdentHelper = new IdentHelper(this);
	}

	public void Bind(SyntaxTree tree)
	{
		CreateSymbols(tree);
		BindSymbols(tree);
	}

	private void CreateSymbols(SyntaxTree tree)
	{
		using var scope = Logger.BeginScope(nameof(CreateSymbols));

		foreach (var statement in tree.TopLevelStatements)
		{
			switch (statement)
			{
				case FunctionDeclaration st:
				{
					CreateFunctionDeclarationSymbols(st);
					break;
				}
				case StructDeclaration st:
				{
					CreateStructDeclarationSymbols(st);
					break;
				}
				default: throw new UnreachableException($"Unhandled statement {statement?.GetType()}");
			}
		}
	}

	private void CreateFunctionDeclarationSymbols(FunctionDeclaration st)
	{
		using var scope = Logger.BeginScope(nameof(CreateFunctionDeclarationSymbols));

		var returnType = BindTypeSyntaxToTypeSymbolOrGetUnbound(st.ReturnType);

		var parameters = st.Parameters
			.Select(p => new FunctionParameterSymbol(
				new LocalSymbolIdent(p.Name.Value),
				BindTypeSyntaxToTypeSymbolOrGetUnbound(p.Type),
				VariableModifiers.None))
			.ToEquatableArray();

		var fbase = new FunctionSymbolBase(returnType, [], parameters, []);
		var symbol = new GlobalFunctionSymbol(IdentHelper.ForNamedFuncCurComp(st), fbase, st);

		Logger.LogDebug("Created function {Name}({Parameters}): {ReturnType}",
			symbol.Ident,
			string.Join(", ", parameters.Select(p => $"{p.Ident}: {p.Type}")),
			returnType);

		SymbolManager.GlobalFunctionTable.Add(symbol);
	}

	private void CreateStructDeclarationSymbols(StructDeclaration st)
	{
		using var scope = Logger.BeginScope(nameof(CreateStructDeclarationSymbols));

		var symbol = new StructSymbol(IdentHelper.ForStructCurComp(st), [], [], [], st);

		var fields = st.Fields
			.Select(f => new StructFieldSymbol(
				new MemberSymbolIdent(f.Name.Value, symbol.Ident),
				BindTypeSyntaxToTypeSymbolOrGetUnbound(f.Type)))
			.ToList();

		symbol = symbol with { Fields = fields };

		Logger.LogDebug("Created struct {Name} {{ {Fields} }}", symbol.Ident, string.Join(", ", fields));

		SymbolManager.GlobalStructTable.Add(symbol);
	}

	private void BindSymbols(SyntaxTree tree)
	{
		using var scope = Logger.BeginScope(nameof(BindSymbols));

		foreach (var statement in tree.TopLevelStatements)
		{
			switch (statement)
			{
				case FunctionDeclaration st:
				{
					BindFunctionStatementSymbols(st);
					break;
				}
				case StructDeclaration st:
				{
					BindStructDeclarationSymbols(st);
					break;
				}
				default: throw new InvalidOperationException($"Unhandled statement {statement.GetType()}");
			}
		}
	}

	private void BindFunctionStatementSymbols(FunctionDeclaration func)
	{
		var funcIdent = IdentHelper.ForNamedFuncCurComp(func);

		using var scope = Logger.BeginScope(nameof(BindFunctionStatementSymbols) + " [{FunctionIdentifier}]", funcIdent);

		// bind parameter symbols and return type
		SymbolManager.GlobalFunctionTable.Update(funcIdent,
			prev => prev with
			{
				Function = prev.Function with
				{
					Parameters = prev.Parameters.Select(p => p with { Type = BindUnboundTypeSymbol(p.Type) }).ToList(),
					ReturnType = BindUnboundTypeSymbol(prev.ReturnType),
				}
			});

		var parser = new OperationBinder(this);
		List<IOperation> operations = [];

		SymbolManager.LocalVariablesManager.EnsureNoScope();
		SymbolManager.LocalVariablesManager.PushScope();

		using (Logger.BeginScope("[parameters]"))
		{
			var symbol = SymbolManager.GlobalFunctionTable.Get(funcIdent);
			foreach (var parameter in symbol.Parameters)
			{
				var ident = parameter.Ident;
				var type = parameter.Type;
				SymbolManager.LocalVariablesManager.AddLocal(new LocalVariableSymbol(ident, type, VariableModifiers.None));
			}
		}

		using (Logger.BeginScope("[operations]"))
		{
			foreach (var statement in func.Statements)
			{
				IOperation op;
				switch (statement)
				{
					case FunctionCallExpression call:
					{
						op = parser.ParseOperation(call);
						break;
					}
					case AssignmentExpression ass:
					{
						op = parser.ParseOperation(ass);
						break;
					}
					case VariableDeclarationStatement decl:
					{
						op = ParseLocalVariableDeclaration(decl, parser.ParseOperation);
						break;
					}
					case DiscardExpression st:
					{
						op = parser.ParseOperation(st.Expression);
						break;
					}
					case ReturnExpression ret:
					{
						op = ret.Value is { } v
							? new ReturnOperation(parser.ParseOperation(v))
							: new ReturnOperation(null);
						break;
					}

					case EmptyStatement: continue;
					default: throw new InvalidOperationException($"Unexpected statement {statement}");
				}

				Logger.LogTrace("Added operation {Operation}", op);
				operations.Add(op);
			}
		}

		SymbolManager.GlobalFunctionTable.Update(IdentHelper.ForNamedFuncCurComp(func),
			prev => prev with
			{
				Function = prev.Function with
				{
					Operations = operations,
				}
			});

		SymbolManager.LocalVariablesManager.PopScope();
	}

	/// <summary>
	/// Parses a variable declaration into an operation and adds the symbol to the local scope
	/// </summary>
	/// <param name="decl"></param>
	/// <param name="initializerParser"></param>
	/// <returns></returns>
	public VariableDeclarationOperation ParseLocalVariableDeclaration(
		VariableDeclarationStatement decl,
		Func<IExpression, IOperation> initializerParser)
	{
		var ident = IdentHelper.ForLocalVariable(decl.Name.Value);
		var type = BindUnboundTypeSymbol(BindTypeSyntaxToTypeSymbolOrGetUnbound(decl.Type));

		var mods = decl.Kind switch
		{
			VariableKind.Let => VariableModifiers.None,
			VariableKind.Mut => VariableModifiers.Mutable,
			_ => throw new UnreachableException()
		};

		var sym = new LocalVariableSymbol(ident, type, mods);
		var init = initializerParser(decl.Initializer);

		SymbolManager.LocalVariablesManager.AddLocal(sym);

		return new VariableDeclarationOperation(sym, init);
	}

	private void BindStructDeclarationSymbols(StructDeclaration declaration)
	{
		using var scope = Logger.BeginScope(nameof(BindStructDeclarationSymbols));

		var structIdent = IdentHelper.ForStructCurComp(declaration);

		List<StructFieldSymbol> fields = [];

		using (Logger.BeginScope("[{StructIdentifier} fields]", structIdent))
		{
			foreach (var field in declaration.Fields)
			{
				var typeReferenceSymbol = BindUnboundTypeSymbol(BindTypeSyntaxToTypeSymbolOrGetUnbound(field.Type));
				var fieldSymbol = new StructFieldSymbol(
					new MemberSymbolIdent(field.Name.Value, structIdent),
					typeReferenceSymbol);
				fields.Add(fieldSymbol);
			}
		}

		SymbolManager.GlobalStructTable.Update(structIdent,
			prev => prev with
			{
				Fields = fields
			});
	}

	private TypeReferenceSymbol BindUnboundTypeSymbol(TypeReferenceSymbol possiblyUnboundSymbol)
	{
		using var scope = Logger.BeginScope(nameof(BindUnboundTypeSymbol));
		return RecursivelyBindSymbol(possiblyUnboundSymbol, this);

		static TypeReferenceSymbol RecursivelyBindSymbol(TypeReferenceSymbol symbol, SyntaxTreeBinder binder)
		{
			using var scope = binder.Logger.BeginScope("[{TypeSymbol}]", symbol);

			switch (symbol)
			{
				case UnboundTypeReferenceSymbol unbound:
				{
					return binder.SymbolManager.GlobalStructTable.TryGet(unbound.Ident, out var structSymbol)
						? new StructTypeReferenceSymbol(structSymbol)
						: throw new CouldNotBindTypeException(unbound.Ident);
				}
				case StructTypeReferenceSymbol sref:
				{
					return sref;
				}
				case GenericTypeReferenceSymbol gen:
				{
					var underlying = RecursivelyBindSymbol(gen.UnderlyingType, binder);

					List<TypeReferenceSymbol> genericArgs = [];
					foreach (var arg in gen.GenericArguments)
					{
						genericArgs.Add(RecursivelyBindSymbol(arg, binder));
					}

					return new GenericTypeReferenceSymbol(underlying, genericArgs);
				}
				case PointerTypeReferenceSymbol ptr:
				{
					var underlying = RecursivelyBindSymbol(ptr.UnderlyingType, binder);
					return new PointerTypeReferenceSymbol(underlying);
				}
				case FunctionTypeReferenceSymbol f:
				{
					var parameters = f.Parameters.Select(p => RecursivelyBindSymbol(p, binder));
					var returnType = RecursivelyBindSymbol(f.ReturnType, binder);
					// TODO: bind type parameter constraints
					return new FunctionTypeReferenceSymbol(f.TypeParameters, parameters.ToEquatableArray(), returnType);
				}

				case null: throw new ArgumentNullException(nameof(symbol));
				default: throw new UnreachableException($"Unhandled type reference kind {symbol.GetType()}");
			}
		}
	}

	private TypeReferenceSymbol BindTypeSyntaxToTypeSymbolOrGetUnbound(TypeRef type)
	{
		return type.Identifier.Value switch
		{
			// TODO: possibly make a SpecialTypeKind enum 
			"Core::Void" => new VoidTypeReferenceSymbol(),
			"_::never" => new NeverTypeReferenceSymbol(),
			_ => MakeTypeSymbol(type, this),
		};

		static TypeReferenceSymbol MakeTypeSymbol(TypeRef type, SyntaxTreeBinder binder)
		{
			TypeReferenceSymbol baseSymbol;

			var ident = binder.IdentHelper.ForTypeRef(type);

			baseSymbol = binder.SymbolManager.GlobalStructTable.TryGet(ident, out var structSymbol)
				? new StructTypeReferenceSymbol(structSymbol)
				: new UnboundTypeReferenceSymbol(ident);

			if (type.Generics.Count > 0)
			{
				var generics = type.Generics.Select(t => MakeTypeSymbol(t, binder)).ToList();
				baseSymbol = new GenericTypeReferenceSymbol(baseSymbol, generics);
			}

			for (var i = 0; i < type.PointerCount; i++)
			{
				baseSymbol = new PointerTypeReferenceSymbol(baseSymbol);
			}

			return baseSymbol;
		}
	}

	[SuppressMessage("Design", "CA1032:Implement standard exception constructors",
		Justification = "I dont want to")]
	public abstract class AnalyzerException(string message) : Exception(message);

	[SuppressMessage("Design", "CA1032:Implement standard exception constructors",
		Justification = "I dont want to")]
	public sealed class CouldNotBindTypeException(string typeName) : AnalyzerException($"Could not bind type {typeName}")
	{
		public string TypeName { get; } = typeName;

		public CouldNotBindTypeException(GlobalSymbolIdent ident) : this(ident.ToString()) { }
	}
}
