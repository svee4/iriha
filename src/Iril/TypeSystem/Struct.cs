using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class Struct
{
	public string Assembly { get; }
	public string Name { get; }

	public ImmutableArray<AttributeApplication> Attributes { get; }
	public ImmutableArray<TypeParameter> TypeParameters { get; }
	public ImmutableArray<Field> Fields { get; }

	internal Struct(
		string assembly,
		string name,
		ImmutableArray<AttributeApplication> attributes,
		ImmutableArray<TypeParameter> typeParameters,
		ImmutableArray<Field> fields)
	{
		Assembly = assembly;
		Name = name;
		Attributes = attributes;
		TypeParameters = typeParameters;
		Fields = fields;
	}
}
