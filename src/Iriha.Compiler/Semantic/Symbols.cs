using Iriha.Compiler.Infra;
using Iriha.Compiler.Parsing.Nodes;
using Iriha.Compiler.Semantic.Binding;
using System.Text;

namespace Iriha.Compiler.Semantic;

public interface ISymbol;

public interface INamedSymbol
{
	ISymbolIdent Ident { get; }
}

public interface IGlobalNamedSymbol : INamedSymbol
{
	new GlobalSymbolIdent Ident { get; }
	ISymbolIdent INamedSymbol.Ident => Ident;
}

public interface ILocalNamedSymbol : INamedSymbol
{
	new LocalSymbolIdent Ident { get; }
	ISymbolIdent INamedSymbol.Ident => Ident;
}

public interface IMemberSymbol : INamedSymbol
{
	new MemberSymbolIdent Ident { get; }
	TypeReferenceSymbol Type { get; }
	ISymbolIdent INamedSymbol.Ident => Ident;
}

[Flags]
public enum VariableModifiers
{
	None,
	Mutable = 1 << 1,
	MatchMutability = 1 << 2
}

public interface IVariableSymbol : INamedSymbol
{
	VariableModifiers Modifiers { get; }
	TypeReferenceSymbol Type { get; }
}

public interface ISymbolIdent;

public sealed record ModuleSymbol
{
	public string Name { get; }

	public ModuleSymbol(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if (Constants.CompilerReservedModuleNames.Contains(name))
		{
			throw new ArgumentException($"Module name {name} is reserved for compiler use");
		}

		Name = name;
	}
}

/// <summary>
/// A symbol identifier that contains a module. e.g. a function, struct or trait from this or any other compilation
/// </summary>
public sealed record GlobalSymbolIdent : ISymbolIdent
{
	public string Name { get; }
	public ModuleSymbol Module { get; }

	public GlobalSymbolIdent(string name, ModuleSymbol module)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentNullException.ThrowIfNull(module);

		Name = name;
		Module = module;
	}

	public override string ToString() =>
		$"{Module.Name}::{Name}";

	public static GlobalSymbolIdent From(string symbolName, string moduleName) =>
		new GlobalSymbolIdent(symbolName, new ModuleSymbol(moduleName));

	public static GlobalSymbolIdent CompilerInternal(string symbolName) =>
		new GlobalSymbolIdent(symbolName, new ModuleSymbol(Constants.CompilerInternalModuleName));

	public static GlobalSymbolIdent CoreLib(string symbol) =>
		From(symbol, Constants.CoreLibModuleName);
}

/// <summary>
/// A symbol identifier without a module. e.g. type parameter, parameter, local variable
/// </summary>
public sealed record LocalSymbolIdent(string Name) : ISymbolIdent
{
	public override string ToString() => $"{Constants.LocalSymbolIdentModuleName}::{Name}";
}

public sealed record MemberSymbolIdent(string Name, GlobalSymbolIdent Parent) : ISymbolIdent
{
	public override string ToString() => $"{Parent}.{Name}";
}

// top level symbols

public abstract record GlobalSymbol(GlobalSymbolIdent Ident);

public sealed record GlobalVariableSymbol(GlobalSymbolIdent Ident, TypeReferenceSymbol Type, VariableModifiers Modifiers)
	: GlobalSymbol(Ident), INamedSymbol, IVariableSymbol
{
	ISymbolIdent INamedSymbol.Ident => Ident;
}

public sealed record StructSymbol(
	GlobalSymbolIdent Ident,
	EquatableArray<TypeParameterSymbol> GenericParameters,
	EquatableArray<StructFieldSymbol> Fields,
	EquatableArray<StructMethodSymbol> Methods,
	StructDeclaration? Syntax) : GlobalSymbol(Ident), INamedSymbol
{
	ISymbolIdent INamedSymbol.Ident => Ident;
}

public sealed record TraitSymbol(
	GlobalSymbolIdent Ident,
	EquatableArray<GlobalSymbolIdent> TypeParameters,
	StructDeclaration? Syntax) : GlobalSymbol(Ident), INamedSymbol
{
	ISymbolIdent INamedSymbol.Ident => Ident;
}

// function related symbols

public interface IFunctionSymbol : ISymbol
{
	TypeReferenceSymbol ReturnType { get; }
	EquatableArray<TypeParameterSymbol> TypeParameters { get; }
	EquatableArray<FunctionParameterSymbol> Parameters { get; }
	EquatableArray<IOperation> Operations { get; }
}

public interface INamedFunctionSymbol : IFunctionSymbol, INamedSymbol;

public sealed record FunctionSymbolBase(
	TypeReferenceSymbol ReturnType,
	EquatableArray<TypeParameterSymbol> TypeParameters,
	EquatableArray<FunctionParameterSymbol> Parameters,
	EquatableArray<IOperation> Operations);

/// <summary>
/// A function that does not have an identifier. e.g. a function expression
/// </summary>
public sealed record UnnamedFunctionSymbol(
	GlobalSymbolIdent Ident,
	FunctionSymbolBase Function,
	FunctionDeclaration? Syntax) : IFunctionSymbol
{
	public TypeReferenceSymbol ReturnType => Function.ReturnType;
	public EquatableArray<TypeParameterSymbol> TypeParameters => Function.TypeParameters;
	public EquatableArray<FunctionParameterSymbol> Parameters => Function.Parameters;
	public EquatableArray<IOperation> Operations => Function.Operations;
}

public sealed record GlobalFunctionSymbol(
	GlobalSymbolIdent Ident,
	FunctionSymbolBase Function,
	FunctionDeclaration? Syntax) : GlobalSymbol(Ident), INamedFunctionSymbol
{
	public TypeReferenceSymbol ReturnType => Function.ReturnType;
	public EquatableArray<TypeParameterSymbol> TypeParameters => Function.TypeParameters;
	public EquatableArray<FunctionParameterSymbol> Parameters => Function.Parameters;
	public EquatableArray<IOperation> Operations => Function.Operations;

	ISymbolIdent INamedSymbol.Ident => Ident;
}

public sealed record StructMethodSymbol(
	MemberSymbolIdent Ident,
	FunctionSymbolBase Function,
	StructMethodDeclaration? Syntax) : INamedFunctionSymbol, IMemberSymbol
{
	public TypeReferenceSymbol ReturnType => Function.ReturnType;
	public EquatableArray<TypeParameterSymbol> TypeParameters => Function.TypeParameters;
	public EquatableArray<FunctionParameterSymbol> Parameters => Function.Parameters;
	public EquatableArray<IOperation> Operations => Function.Operations;

	public TypeReferenceSymbol Type => new FunctionTypeReferenceSymbol(
		TypeParameters,
		Parameters.Select(p => p.Type).ToEquatableArray(),
		ReturnType);

	ISymbolIdent INamedSymbol.Ident => Ident;
}

// type references

public interface INamedTypeReferenceSymbol
{
	ISymbolIdent Ident { get; }
}

public abstract record TypeReferenceSymbol;

public abstract record GlobalTypeReferenceSymbol(GlobalSymbolIdent Ident) : TypeReferenceSymbol, INamedTypeReferenceSymbol
{
	ISymbolIdent INamedTypeReferenceSymbol.Ident => Ident;
	public override string ToString() => Ident.ToString();
}

/// <summary>
/// Signifies that a function never returns
/// </summary>
public sealed record NeverTypeReferenceSymbol() : GlobalTypeReferenceSymbol(GlobalSymbolIdent.CompilerInternal("never"))
{
	public override string ToString() => "never";
}

/// <summary>
/// The empty unit type
/// </summary>
public sealed record VoidTypeReferenceSymbol() : GlobalTypeReferenceSymbol(GlobalSymbolIdent.CoreLib("Void"));

public sealed record TupleTypeReferenceSymbol(EquatableArray<TypeReferenceSymbol> Values)
	: GlobalTypeReferenceSymbol(GlobalSymbolIdent.CoreLib(IdentHelper.FormatArity("Tuple", Values.Count)))
{
	public int Length => Values.Count;
}

/// <summary>
/// <code>
/// struct S {}
/// </code>
/// </summary>
public sealed record StructTypeReferenceSymbol(StructSymbol Source) : GlobalTypeReferenceSymbol(Source.Ident)
{
	public override string ToString() => base.ToString();
}

/// <summary>
/// Trait T {}
/// </summary>
public sealed record TraitReferenceTypeSymbol(TraitSymbol Source) : GlobalTypeReferenceSymbol(Source.Ident)
{
	public override string ToString() => base.ToString();
}

/// <summary>
/// Reference to a function declaration
/// </summary>
public sealed record NamedFunctionTypeReferenceSymbol(GlobalFunctionSymbol Source)
	: GlobalTypeReferenceSymbol(Source.Ident)
{
	public override string ToString() => base.ToString();
}

/*

func CallFun<
	TArg,
	TRet,
	TFunc: (TArg) -> TRet
>(arg: TArg, fun: TFunc): TRet {
	return fun(arg);
}

func AppendToList<T>(list: List<T>, value: T): void {
	list.Append(value);
}

let variable: (int) -> int = func (arg: int) { return arg; }

*/

/// <summary>
/// Inline function type:
/// <code>
/// (string, int) -> void
/// </code>
/// </summary>
public sealed record FunctionTypeReferenceSymbol(
	EquatableArray<TypeParameterSymbol> TypeParameters,
	EquatableArray<TypeReferenceSymbol> Parameters,
	TypeReferenceSymbol ReturnType) : TypeReferenceSymbol
{
	public override string ToString()
	{
		var sb = new StringBuilder();

		if (TypeParameters.Count > 0)
		{
			sb.Append('<');
			foreach (var p in TypeParameters)
			{
				sb.Append(p);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			sb.Append('>');
		}

		sb.Append('(');
		foreach (var p in Parameters)
		{
			sb.Append(p);
			sb.Append(", ");
		}
		sb.Remove(sb.Length - 2, 2);
		sb.Append(')');

		sb.Append(" -> ");
		sb.Append(ReturnType);

		return sb.ToString();
	}
}

/// <summary>
/// Generic type:
/// <code>
/// List&lt;int&gt;
/// </code>
/// </summary>
public sealed record GenericTypeReferenceSymbol(
	TypeReferenceSymbol UnderlyingType,
	EquatableArray<TypeReferenceSymbol> GenericArguments) : TypeReferenceSymbol
{
	public override string ToString() => $"{UnderlyingType}<{string.Join(", ", GenericArguments)}>";
}

/// <summary>
/// Pointer type:
/// <code>
/// &amp;int
/// </code>
/// </summary>
public sealed record PointerTypeReferenceSymbol(TypeReferenceSymbol UnderlyingType) : TypeReferenceSymbol
{
	public override string ToString() => $"&{UnderlyingType}";
}

/// <summary>
/// Type parameter:
/// <code>
/// func M&lt;T&gt;() {
///		let var: T;
///		#        ^ here
/// }
/// </code>
/// </summary>
public sealed record TypeParameterTypeReferenceSymbol(TypeParameterSymbol Source) : TypeReferenceSymbol;

/// <summary>
/// Reference to a type that has not been bound yet.<br />
/// Should never be encountered after binding has been completed.
/// </summary>
public sealed record UnboundTypeReferenceSymbol(GlobalSymbolIdent Ident) : TypeReferenceSymbol;

// stuff

public sealed record LocalVariableSymbol(LocalSymbolIdent Ident, TypeReferenceSymbol Type, VariableModifiers Modifiers)
	: INamedSymbol, IVariableSymbol
{
	ISymbolIdent INamedSymbol.Ident => Ident;
}

public sealed record FunctionParameterSymbol(LocalSymbolIdent Ident, TypeReferenceSymbol Type, VariableModifiers Modifiers)
	: IVariableSymbol
{
	ISymbolIdent INamedSymbol.Ident => Ident;
}

public sealed record TypeParameterSymbol(LocalSymbolIdent Ident, EquatableArray<TypeParameterSymbolConstraint> Constraints)
{
	public override string ToString() => $"{Ident.Name}";
}

public sealed record TypeParameterSymbolConstraint;

public sealed record StructFieldSymbol(MemberSymbolIdent Ident, TypeReferenceSymbol Type) : IMemberSymbol
{
	ISymbolIdent INamedSymbol.Ident => Ident;
	public override string ToString() => $"{Ident}: {Type}";
}
