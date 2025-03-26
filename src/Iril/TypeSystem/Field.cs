using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class Field
{
	public ImmutableArray<AttributeApplication> Attributes { get; }
	public string Name { get; }
	public TypeReference Type { get; }

	internal Field(ImmutableArray<AttributeApplication> attributes, string name, TypeReference type)
	{
		Attributes = attributes;
		Name = name;
		Type = type;
	}
}
