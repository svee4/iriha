namespace Iril.Builder;

public sealed class FieldBuilder
{
	public string Name { get; }
	public TypeSystem.TypeReference Type { get; }

	public TypeSystem.Field Build() => new TypeSystem.Field([], Name, Type);

	internal FieldBuilder(string name, TypeSystem.TypeReference type)
	{
		Name = name;
		Type = type;
	}
}
