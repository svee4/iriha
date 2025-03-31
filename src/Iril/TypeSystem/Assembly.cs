using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class Assembly
{
	public string Name { get; }

	public ImmutableArray<Attribute> Attributes { get; }
	public ImmutableArray<Struct> Structs { get; }
	public ImmutableArray<Function> Functions { get; }

	internal Assembly(
		string name,
		ImmutableArray<Attribute> attributes, 
		ImmutableArray<Struct> structs,
		ImmutableArray<Function> functions)
	{
		Name = name;
		Attributes = attributes;
		Structs = structs;
		Functions = functions;
	}
}
