using Iril;
using Iril.Builder;
using Iril.Instructions;
using Iril.Interpreter;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
{
	options.SingleLine = true;
}));

var logger = loggerFactory.CreateLogger<Program>();

var core = CoreLib.Assembly;
logger.LogInformation("{CoreAssembly}", AssemblyWriter.WriteAssembly(core));

var assb = new AssemblyBuilder("Test");
var testFunc = assb.AddFunction(new FunctionSignatureBuilder("TestFunc", [], [], core.FindType("Int32").AsTypeRef()));
var addf = core.GetFuncSig("Int32.Add");

testFunc.Writer
	.Write(new LoadConst(TypedPrimitive.From(2)))
	.Write(new LoadConst(TypedPrimitive.From(3)))
	.Write(new Call(addf))
	.Write(new Return());

var testf2 = assb.AddFunction(new FunctionSignatureBuilder("testf2", [], [], core.FindType("Void").AsTypeRef()));
testf2.Writer
	.Write(new LoadConst(TypedPrimitive.From(0.1f)))
	.Write(new LoadConst(TypedPrimitive.From(0.2f)))
	.Write(new Add(PrimitiveKind.F32));

var testAss = assb.Build();
logger.LogInformation("{TestAssembly}", AssemblyWriter.WriteAssembly(testAss));

var interp = new Interp([core, testAss], logger);
var f = testAss.GetFunc(testAss.GetFuncSig("TestFunc"));
interp.ExecuteFunction(f, []);
