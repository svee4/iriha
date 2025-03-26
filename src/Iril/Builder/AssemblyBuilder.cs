using Iril.TypeSystem;
using System.Collections.Immutable;

namespace Iril.Builder;

public sealed class AssemblyBuilder(string baseNamespace)
{
	private List<AttributeDeclarationBuilder> _attributes = [];
	public IReadOnlyList<AttributeDeclarationBuilder> Attributes => _attributes;

	private List<StructBuilder> _structs = [];
	public IReadOnlyList<StructBuilder> Structs => _structs;

	private List<FunctionBuilder> _functions = [];
	public IReadOnlyList<FunctionBuilder> Functions => _functions;

	public string BaseNamespace { get; } = baseNamespace;

	public Assembly Build() => 
		new Assembly(
			Attributes.Select(a => a.Build()).ToImmutableArray(),
			Structs.Select(s => s.Build()).ToImmutableArray(),
			Functions.Select(f => f.Build()).ToImmutableArray());

	public AttributeDeclarationBuilder AddAttributeDeclaration(string name, IEnumerable<string> parameters)
	{
		var a = new AttributeDeclarationBuilder(name, parameters);
		_attributes.Add(a);
		return a;
	}

	public StructBuilder AddStruct(string name)
	{
		name = BaseNamespace + "." + name;
		var builder = new StructBuilder(name);
		_structs.Add(builder);
		return builder;
	}

	public FunctionSignatureBuilder Signature(
			string name,
			IEnumerable<TypeParameter> typeParameters,
			IEnumerable<FunctionParameter> parameters,
			TypeReference? returnType) =>
		new FunctionSignatureBuilder(name, typeParameters, parameters, returnType);

	public FunctionBuilder AddFunction(FunctionSignatureBuilder signature)
	{
		var b = new FunctionBuilder(signature);
		_functions.Add(b);
		return b;
	}
}
