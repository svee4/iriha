using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class Assembly
{
	public ImmutableArray<Attribute> Attributes { get; }
	public ImmutableArray<Struct> Structs { get; }
	public ImmutableArray<Function> Functions { get; }

	internal Assembly(
		ImmutableArray<Attribute> attributes, 
		ImmutableArray<Struct> structs,
		ImmutableArray<Function> functions)
	{
		Attributes = attributes;
		Structs = structs;
		Functions = functions;
	}
}
