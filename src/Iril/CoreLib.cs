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

		var memberFunctionAttribute = assb.AddAttributeDeclaration("MemberFunction", ["member"]);
		var operatorFunctionAttribute = assb.AddAttributeDeclaration("OperatorFunction", ["operatorIdent"]);
		var thisParameterAttribute = assb.AddAttributeDeclaration("ThisParameter", []);

		var int32b = assb.AddStruct("Int32");
		{
			var opAdd = assb.AddFunction(new FunctionSignatureBuilder(
				name: "Add",
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
				.Write(new Add())
				.Write(new Return());
		}

		Assembly = assb.Build();
	}
}
