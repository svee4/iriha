using Iril.Builder;
using Iril.Instructions;
using Iril.TypeSystem;

namespace Iril;

public static class CoreLib
{
	public static Assembly Assembly { get; }

	static CoreLib()
	{
		var assb = new AssemblyBuilder("Core");

		var memberFunctionAttribute = assb.AddAttributeDeclaration("memberFunction", ["member"]);
		var operatorFunctionAttribute = assb.AddAttributeDeclaration("operatorFunction", ["operatorIdent"]);
		var thisParameterAttribute = assb.AddAttributeDeclaration("thisParameter", []);

		var int32b = assb.AddStruct("Int32");
		{
			var opAdd = assb.AddFunction(new FunctionSignatureBuilder(
				name: "Int32.Add",
				typeParameters: [],
				parameters: [
					new FunctionParameter(
						attributes: [new AttributeBuilder(thisParameterAttribute, []).Apply()],
						"this",
						int32b.ToTypeReference()),

					new FunctionParameter(
						attributes: [],
						name: "other",
						int32b.ToTypeReference())
				],
				returnType: int32b.ToTypeReference()
			));

			_ = opAdd.Writer
				.Write(new LoadArg(0))
				.Write(new LoadArg(1))
				.Write(new Add(PrimitiveKind.I32))
				.Write(new Return());
		}

		Assembly = assb.Build();
	}
}
