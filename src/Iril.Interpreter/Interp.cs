using Iril.Instructions;
using Iril.TypeSystem;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Diagnostics;

namespace Iril.Interpreter;

public sealed class Interp(IEnumerable<Assembly> assemblies, ILogger logger)
{
	private readonly FrozenDictionary<string, Assembly> _assemblies = assemblies.ToFrozenDictionary(ass => ass.Name);
	private readonly ILogger _logger = logger;

	private FreeStack<TypedObject> _stack = [];
	private FreeStack<List<TypedObject>> _locals = [];
	private List<TypedObject> _args = [];

	public void ExecuteFunction(Function func, List<TypedObject> arguments)
	{
		var prev = (Locals: _locals, Args: _args, Stack: _stack);
		_stack = [];
		_locals = [];
		_args = [];

		foreach (var arg in arguments)
		{
			_args.Add(arg);
		}

		_logger.LogInformation("Function: {Function}", func.Signature.Name);

		if (_args.Count > 0)
		{
			_logger.LogInformation("Args: {Args}", string.Join(", ", _args));
		}

		foreach (var inst in func.Body.Instructions)
		{
			_logger.LogInformation("{Instruction}", inst.AsmRepr);

			var cont = Execute(inst);

			if (_stack.Count > 0)
			{
				_logger.LogInformation("Stack after: {Stack}", _stack);
			}

			if (_locals.Count > 0)
			{
				_logger.LogInformation("Locals after: {Locals}", _locals);
			}

			if (!cont)
			{
				break;
			}
		}

		_logger.LogInformation("Exit {Func}", func.Signature.Name);
		(_stack, _locals, _args) = (prev.Stack, prev.Locals, prev.Args);
	}

	private bool Execute(Instruction inst)
	{
		switch (inst)
		{
			case LoadArg ldarg:
			{
				_stack.Push(_args[ldarg.Index]);
				break;
			}
			case LoadConst ldc:
			{
				_stack.Push(TypedObject.FromTypedPrimitive(ldc.Value));
				break;
			}
			case Call call:
			{
				var f = _assemblies[call.Signature.Assembly].GetFunc(call.Signature);
				ExecuteFunction(f, _stack.ToList());
				break;
			}
			case Return:
			{
				return false;
			}

			case Add add:
			{
				var right = _stack.Pop();
				var left = _stack.Pop();

				var result = left.AsPrimitive().Switch(right.AsPrimitive(),
					static (v, state) => TypedObject.From((byte)(v + state.AsU8())),
					static (v, state) => TypedObject.From((sbyte)(v + state.AsI8())),
					static (v, state) => TypedObject.From((short)(v + state.AsI16())),
					static (v, state) => TypedObject.From((ushort)(v + state.AsU16())),
					static (v, state) => TypedObject.From((int)(v + state.AsI32())),
					static (v, state) => TypedObject.From((uint)(v + state.AsU16())),
					static (v, state) => TypedObject.From((long)(v + state.AsI64())),
					static (v, state) => TypedObject.From((ulong)(v + state.AsU64())),
					static (v, state) => TypedObject.From((Half)(v + state.AsF16())),
					static (v, state) => TypedObject.From((float)(v + state.AsF32())),
					static (v, state) => TypedObject.From((double)(v + state.AsF64()))
				);

				_stack.Push(result);
				break;
			}

			default: throw new NotImplementedException($"instruction {inst} not implemented");
		}

		return true;
	}

	public readonly record struct TypedObject(PrimitiveKind Kind, object Value)
	{
		public static TypedObject From(byte value) => new TypedObject(PrimitiveKind.U8, value);
		public static TypedObject From(sbyte value) => new TypedObject(PrimitiveKind.I8, value);
		public static TypedObject From(short value) => new TypedObject(PrimitiveKind.I16, value);
		public static TypedObject From(ushort value) => new TypedObject(PrimitiveKind.U16, value);
		public static TypedObject From(int value) => new TypedObject(PrimitiveKind.I32, value);
		public static TypedObject From(uint value) => new TypedObject(PrimitiveKind.U32, value);
		public static TypedObject From(long value) => new TypedObject(PrimitiveKind.I64, value);
		public static TypedObject From(ulong value) => new TypedObject(PrimitiveKind.U64, value);
		public static TypedObject From(Half value) => new TypedObject(PrimitiveKind.F16, value);
		public static TypedObject From(float value) => new TypedObject(PrimitiveKind.F32, value);
		public static TypedObject From(double value) => new TypedObject(PrimitiveKind.F64, value);

		public static TypedObject FromTypedPrimitive(TypedPrimitive value) => value.Switch(
			(byte)0,
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v),
			(v, _) => From(v)
		);

		public TypedPrimitive AsPrimitive() => PrimitiveExtensions.Switch(Kind, Value,
			static (obj) => TypedPrimitive.From((byte)obj),
			static (obj) => TypedPrimitive.From((sbyte)obj),
			static (obj) => TypedPrimitive.From((short)obj),
			static (obj) => TypedPrimitive.From((ushort)obj),
			static (obj) => TypedPrimitive.From((int)obj),
			static (obj) => TypedPrimitive.From((uint)obj),
			static (obj) => TypedPrimitive.From((long)obj),
			static (obj) => TypedPrimitive.From((ulong)obj),
			static (obj) => TypedPrimitive.From((Half)obj),
			static (obj) => TypedPrimitive.From((float)obj),
			static (obj) => TypedPrimitive.From((double)obj)
		);
	}
}
