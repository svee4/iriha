namespace Iril.Instructions;

public abstract record Instruction
{
	public string AsmName { get; }
	public string AsmRepr { get; }

	internal Instruction(string asmName) 
	{
		AsmName = asmName;
		AsmRepr = asmName;
	}

	internal Instruction(string asmName, string asmRepr)
	{
		AsmName = asmName;
		AsmRepr = asmRepr;
	}
}

public sealed record LoadArg(int Index) : Instruction("ldarg", $"ldarg {Index}");
public sealed record LoadLocal(int Index) : Instruction("ldloc", $"ldloc {Index}");
public sealed record StoreLocal(int Index) : Instruction("stloc", $"stloc {Index}");

public sealed record Call(TypeSystem.FunctionSignature Signature) : Instruction("call");
public sealed record Return() : Instruction("ret");

public sealed record Add() : Instruction("add");
public sealed record Subtract() : Instruction("sub");
public sealed record Multiply() : Instruction("mul");
public sealed record Divide() : Instruction("div");

public sealed record RightShift() : Instruction("bit.rsh");
public sealed record LeftShift() : Instruction("bit.lsh");
public sealed record BitAnd() : Instruction("bit.and");
public sealed record BitOr() : Instruction("bit.or");
public sealed record BitNot() : Instruction("bit.not");

public sealed record CompareEqual() : Instruction("cmp.eq");
public sealed record CompareLessThan() : Instruction("cmp.lt");
public sealed record CompareGreaterThan() : Instruction("cmp.gt");

public sealed record Jump(int Offset) : Instruction("jmp", $"jmp {Offset}");
public sealed record JumpTrue(int Offset) : Instruction("jmp.true", $"jmp.true {Offset}");
public sealed record JumpFalse(int Offset) : Instruction("jmp.false", $"jmp.false {Offset}");

