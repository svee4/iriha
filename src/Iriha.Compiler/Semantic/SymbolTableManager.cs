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

	private Stack<LocalVariableTable> Tables { get; } = [];
	private LocalVariableTable CurrentScope => Tables.Peek();

	public void PushScope() => Tables.Push(new(_manager));

	public void PopScope() => Tables.Pop();

	public void EnsureNoScope()
	{
		if (Tables.Count != 0)
			throw new InvalidOperationException($"Scope was {Tables.Count}");
	}

	public void AddLocal(LocalVariableSymbol variable)
	{
		_manager.EnsureSymbolNameIsUnique(variable.Ident);
		CurrentScope.Add(variable);
	}

	public bool IsInScope(LocalSymbolIdent ident)
	{
		// foreach over a stack enumerates from top to bottom
		foreach (var table in Tables)
		{
			if (table.Contains(ident))
				return true;
		}

		return false;
	}

	public bool TryGet(string name, [NotNullWhen(true)] out LocalVariableSymbol? symbol)
	{
		var ident = IdentHelper.ForLocalVariable(name);
		foreach (var table in Tables)
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
