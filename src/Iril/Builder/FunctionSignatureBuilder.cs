using System.Collections.Immutable;

namespace Iril.Builder;

public sealed class FunctionSignatureBuilder(
	string name,
	IEnumerable<TypeSystem.TypeParameter> typeParameters,
	IEnumerable<TypeSystem.FunctionParameter> parameters,
	TypeSystem.TypeReference returnType)
{
	public string Name { get; } = name;
	public IReadOnlyList<TypeSystem.TypeParameter> TypeParameters { get; } = typeParameters.ToArray();
	public IReadOnlyList<TypeSystem.FunctionParameter> Parameters { get; } = parameters.ToArray();
	public TypeSystem.TypeReference ReturnType { get; } = returnType;

	public TypeSystem.FunctionSignature Build(string assembly) =>
		new TypeSystem.FunctionSignature(
			assembly: assembly,
			name: Name,
			typeParameters: TypeParameters.ToImmutableArray(),
			parameters: Parameters.ToImmutableArray(),
			returnType: ReturnType);
}
