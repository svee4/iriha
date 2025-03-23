using Iriha.Compiler.Infra;
using Iriha.Compiler.Semantic.Binding;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Iriha.Compiler.Semantic;

public sealed class SymbolTableManager
{
	public GlobalVariableTable GlobalVariableTable { get; }
	public StructTable GlobalStructTable { get; }
	public TraitTable GlobalTraitTable { get; }
	public FunctionTable GlobalFunctionTable { get; }
	public LocalVariablesManager LocalVariablesManager { get; }
	internal SyntaxTreeBinder Binder { get; }

	public SymbolTableManager(SyntaxTreeBinder binder)
	{
		GlobalVariableTable = new(this);
		GlobalStructTable = new(this);
		GlobalTraitTable = new(this);
		GlobalFunctionTable = new(this);
		LocalVariablesManager = new(this);
		Binder = binder;
	}

	public bool SymbolExists(ISymbolIdent ident) => ident switch
	{
		GlobalSymbolIdent g =>
			GlobalVariableTable.Contains(g)
			|| GlobalStructTable.Contains(g)
			|| GlobalTraitTable.Contains(g)
			|| GlobalFunctionTable.Contains(g),
		LocalSymbolIdent l => LocalVariablesManager.IsInScope(l),
		null => throw new ArgumentNullException(nameof(ident)),
		_ => throw new UnreachableException($"Unhandled {nameof(ISymbolIdent)} {ident.GetType()}")
	};

	public void EnsureSymbolNameIsUnique(ISymbolIdent ident)
	{
		if (SymbolExists(ident))
		{
			throw new InvalidOperationException($"Symbol {ident} already exists");
		}
	}
}

public sealed class LocalVariablesManager(SymbolTableManager manager)
{
	private readonly SymbolTableManager _manager = manager;

	private Stack<LocalVariableTable> Scopes { get; } = [];
	private LocalVariableTable CurrentScope => Scopes.Peek();

	public IDisposable PushScope()
	{
		var scope = new LocalVariableTable(_manager);
		Scopes.Push(scope);

		return new MultiUseDisposable(() =>
		{
			if (!ReferenceEquals(scope, CurrentScope))
			{
				throw new InvalidOperationException("Attempt to dispose the wrong scope - a scope has gone rogue");
			}

			_ = PopScope();
		});
	}

	public LocalVariableTable PopScope() => Scopes.Pop();

	public void EnsureNoScope()
	{
		if (Scopes.Count > 0)
		{
			throw new InvalidOperationException($"Scopes was not empty ({Scopes.Count})");
		}
	}

	public void AddLocal(LocalVariableSymbol variable)
	{
		// shadowing is allowed - local scope is always check first and top down
		if (CurrentScope.Symbols.Any(sym => sym.Ident == variable.Ident))
		{
			throw new InvalidOperationException($"Variable ident {variable.Ident} already used in current scope");
		}

		CurrentScope.Add(variable);
	}

	public bool IsInScope(LocalSymbolIdent ident)
	{
		// foreach over a stack enumerates from top to bottom
		foreach (var table in Scopes)
		{
			if (table.Contains(ident))
			{
				return true;
			}
		}

		return false;
	}

	public bool TryGet(LocalSymbolIdent ident, [NotNullWhen(true)] out LocalVariableSymbol? symbol)
	{
		foreach (var table in Scopes)
		{
			if (table.TryGet(ident, out symbol))
			{
				return true;
			}
		}

		symbol = null;
		return false;
	}
}
