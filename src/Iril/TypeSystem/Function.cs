using Iril.Instructions;
using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class Function
{
	public ImmutableArray<AttributeApplication> Attributes { get; }
	public FunctionSignature Signature { get; }
	public Body Body { get; }

	internal Function(
		ImmutableArray<AttributeApplication> attributes, 
		FunctionSignature signature,
		Body body)
	{
		Attributes = attributes;
		Signature = signature;
		Body = body;
	}
}
