using Iril.Instructions;
using Iril.TypeSystem;
using System.Collections.Immutable;

namespace Iril.Builder;

public sealed class InstructionWriter
{
	private List<Instruction> _instructions = [];
	public IReadOnlyList<Instruction> Instructions => _instructions;

	public InstructionWriter Write(Instruction instr)
	{
		_instructions.Add(instr);
		return this;
	}

	public Body Build() => new Body(Instructions.ToImmutableArray());
}
