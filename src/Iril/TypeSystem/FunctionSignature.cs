using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class FunctionSignature
{
	public string Name { get; }
	public ImmutableArray<TypeParameter> TypeParameters { get; }
	public ImmutableArray<FunctionParameter> Parameters { get; }
	public TypeReference ReturnType { get; }

	internal FunctionSignature(
		string name, 
		ImmutableArray<TypeParameter> typeParameters,
		ImmutableArray<FunctionParameter> parameters, 
		TypeReference returnType)
	{
		Name = name;
		TypeParameters = typeParameters;
		Parameters = parameters;
		ReturnType = returnType;
	}

	public override string ToString()
	{
		var t = string.Join(", ", TypeParameters);
		var p = string.Join(", ", Parameters);
		return $"{Name}<{t}>({p}):{ReturnType}";
	}
}
