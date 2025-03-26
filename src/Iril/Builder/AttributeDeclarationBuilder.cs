using System.Collections.Immutable;

namespace Iril.Builder;

public sealed class AttributeDeclarationBuilder
{
	public string Name { get; }
	public IReadOnlyList<string> Parameters { get; }

	internal AttributeDeclarationBuilder(string name, IEnumerable<string> parameters)
	{
		Name = name;
		Parameters = parameters.ToArray();
	}

	public TypeSystem.Attribute Build() => 
		new TypeSystem.Attribute(Name, Parameters.ToImmutableArray());
}
