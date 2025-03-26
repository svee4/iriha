using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class AttributeApplication(Attribute attribute, ImmutableArray<string> arguments)
{
	public Attribute Attribute { get; } = attribute;
	public ImmutableArray<string> Arguments { get; } = arguments;
}
