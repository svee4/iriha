using System.Diagnostics.CodeAnalysis;

namespace Iriha.Compiler.Semantic;

public abstract class SymbolTable<T, TIdent>(SymbolTableManager manager)
	where T : INamedSymbol
	where TIdent : ISymbolIdent
{
	protected SymbolTableManager Manager { get; } = manager;
	protected Dictionary<TIdent, T> SymbolMap { get; } = [];

	public IReadOnlyList<T> Symbols => SymbolMap.Values.ToArray();

	public bool Contains(TIdent name) => TryGet(name, out _);

	public bool TryGet(TIdent ident, [NotNullWhen(true)] out T? symbol)
	{
		if (SymbolMap.TryGetValue(ident, out var sym))
		{
			symbol = sym;
			return true;
		}

		symbol = default;
		return false;
	}

	public void Add(T symbol)
	{
		Manager.EnsureSymbolNameIsUnique(symbol.Ident);
		SymbolMap.Add((TIdent)symbol.Ident, symbol);
	}

	public T Get(TIdent ident) =>
		SymbolMap.TryGetValue(ident, out var value)
			? value
			: throw new InvalidOperationException($"Could not find symbol {ident}");

	public void Update(TIdent ident, Func<T, T> updater) =>
		SymbolMap[ident] = updater(SymbolMap[ident]);
}

public sealed class LocalVariableTable(SymbolTableManager manager) : SymbolTable<LocalVariableSymbol, LocalSymbolIdent>(manager);
public sealed class GlobalVariableTable(SymbolTableManager manager) : SymbolTable<GlobalVariableSymbol, GlobalSymbolIdent>(manager);
public sealed class StructTable(SymbolTableManager manager) : SymbolTable<StructSymbol, GlobalSymbolIdent>(manager);
public sealed class TraitTable(SymbolTableManager manager) : SymbolTable<TraitSymbol, GlobalSymbolIdent>(manager);
public sealed class FunctionTable(SymbolTableManager manager) : SymbolTable<GlobalFunctionSymbol, GlobalSymbolIdent>(manager);
