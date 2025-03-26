namespace Iril.TypeSystem;

public sealed class TypeParameter
{
	public IReadOnlyList<Attribute> Attributes { get; }
	public string Name { get; }
}
