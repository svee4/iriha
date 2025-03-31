namespace Iril.Builder;

public sealed class FunctionBuilder
{
	public FunctionSignatureBuilder Signature { get; }
	public InstructionWriter Writer { get; } = new();

	internal FunctionBuilder(FunctionSignatureBuilder signature)
	{
		Signature = signature;
	}

	public TypeSystem.Function Build(string assembly) =>
		new TypeSystem.Function(
			attributes: [],
			signature: Signature.Build(assembly),
			body: Writer.Build());
}
