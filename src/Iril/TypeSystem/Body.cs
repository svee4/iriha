using Iril.Instructions;
using System.Collections.Immutable;

namespace Iril.TypeSystem;

public sealed class Body
{
	public ImmutableArray<Instruction> Instructions { get; }

	internal Body(ImmutableArray<Instruction> instructions) => 
		Instructions = instructions;
}
