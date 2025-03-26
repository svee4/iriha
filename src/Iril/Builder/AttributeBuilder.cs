using Iril.TypeSystem;
using System.Collections.Immutable;

namespace Iril.Builder;

public sealed class AttributeBuilder
{
	public AttributeDeclarationBuilder Attribute { get; }
	public IReadOnlyList<string> Arguments { get; }

	internal AttributeBuilder(AttributeDeclarationBuilder attribute, IEnumerable<string> arguments)
	{
		Attribute = attribute;
		Arguments = arguments.ToArray();
	}

	public AttributeApplication Apply() =>
		new AttributeApplication(Attribute.Build(), Arguments.ToImmutableArray());
}
