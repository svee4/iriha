using Iril.TypeSystem;
using System.Collections.Immutable;

namespace Iril.Builder;

public sealed class StructBuilder
{
	public string Name { get; }

	private List<FieldBuilder> _fields = [];
	public IReadOnlyList<FieldBuilder> Fields => _fields;

	public TypeSystem.Struct Build(string assembly) => 
		new TypeSystem.Struct(
			assembly,
			Name,
			attributes: [], 
			typeParameters: [], 
			fields: Fields.Select(f => f.Build()).ToImmutableArray());

	internal StructBuilder(string name) 
	{
		Name = name;
	}

	public FieldBuilder AddField(string name, TypeSystem.TypeReference type)
	{
		var b = new FieldBuilder(name, type);
		_fields.Add(b);
		return b;
	}

	public TypeSystem.TypeReference ToTypeReference() =>
		new BuilderTypeReference(this);
}
