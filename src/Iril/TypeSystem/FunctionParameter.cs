using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class FunctionParameter
{
	public ImmutableArray<AttributeApplication> Attributes { get; }
	public string Name { get; }
	public TypeReference Type { get; }

	internal FunctionParameter(ImmutableArray<AttributeApplication> attributes, string name, TypeReference type)
	{
		Attributes = attributes;
		Name = name;
		Type = type;
	}
}
