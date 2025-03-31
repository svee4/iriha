using Iril.TypeSystem;

namespace Iril.Interpreter;

public static class TypeSystemExtensions
{
	public static Struct FindType(this Assembly ass, string name) =>
		ass.Structs.First(s => s.Name == name);

	public static TypeReference AsTypeRef(this Struct s) =>
		new StructTypeReference(s);

	public static FunctionSignature GetFuncSig(this Assembly ass, string name)
	{
		return ass.Functions.First(f => f.Signature.Name == name).Signature;
	}

	public static Function GetFunc(this Assembly ass, FunctionSignature sig) =>
		ass.Functions.First(f => f.Signature.Name == sig.Name);
}
