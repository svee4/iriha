using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;

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

public enum PrimitiveKind
{
	None = 0,
	I8,
	U8,
	I16,
	U16,
	I32,
	U32,
	I64,
	U64,
	F16,
	F32,
	F64
}

public readonly struct TypedPrimitive
{
	private readonly ulong _value;
	public PrimitiveKind Kind { get; }

	[Obsolete("Do not use default constructor")]
	public TypedPrimitive() { }

	private TypedPrimitive(ulong value, PrimitiveKind kind)
	{
		_value = value;
		Kind = kind;
	}

#pragma warning disable format
	public byte		AsU8() =>	Kind == PrimitiveKind.U8  ? Deserialize<byte>(_value) :		ThrowTypeMismatch<byte>(Kind);
	public sbyte	AsI8() =>	Kind == PrimitiveKind.I8  ? Deserialize<sbyte>(_value) :	ThrowTypeMismatch<sbyte>(Kind);
	public short	AsI16() =>	Kind == PrimitiveKind.I16 ? Deserialize<short>(_value) :	ThrowTypeMismatch<short>(Kind);
	public ushort	AsU16() =>	Kind == PrimitiveKind.U16 ? Deserialize<ushort>(_value) :	ThrowTypeMismatch<ushort>(Kind);
	public int		AsI32() =>	Kind == PrimitiveKind.I32 ? Deserialize<int>(_value) :		ThrowTypeMismatch<int>(Kind);
	public uint		AsU32() =>	Kind == PrimitiveKind.U32 ? Deserialize<uint>(_value) :		ThrowTypeMismatch<uint>(Kind);
	public long		AsI64() =>	Kind == PrimitiveKind.I64 ? Deserialize<long>(_value) :		ThrowTypeMismatch<long>(Kind);
	public ulong	AsU64() =>	Kind == PrimitiveKind.U64 ? Deserialize<ulong>(_value) :	ThrowTypeMismatch<ulong>(Kind);
	public Half		AsF16() =>	Kind == PrimitiveKind.F16 ? Deserialize<Half>(_value) :		ThrowTypeMismatch<Half>(Kind);
	public float	AsF32() =>	Kind == PrimitiveKind.F32 ? Deserialize<float>(_value) :	ThrowTypeMismatch<float>(Kind);
	public double   AsF64() =>	Kind == PrimitiveKind.F64 ? Deserialize<double>(_value) :	ThrowTypeMismatch<double>(Kind);
#pragma warning restore format

	public static TypedPrimitive From(byte value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.U8);
	public static TypedPrimitive From(sbyte value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.I8);
	public static TypedPrimitive From(short value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.I16);
	public static TypedPrimitive From(ushort value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.U16);
	public static TypedPrimitive From(int value) =>		new TypedPrimitive(Serialize(value), PrimitiveKind.I32);
	public static TypedPrimitive From(uint value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.U32);
	public static TypedPrimitive From(long value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.I64);
	public static TypedPrimitive From(ulong value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.U64);
	public static TypedPrimitive From(Half value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.F16);
	public static TypedPrimitive From(float value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.F32);
	public static TypedPrimitive From(double value) =>	new TypedPrimitive(Serialize(value), PrimitiveKind.F64);

	public override string ToString() => Switch<string, byte>(
		default,
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture),
		(v, _) => v.ToString(CultureInfo.InvariantCulture)
	);

	public T Switch<T, TState>(
			TState state,
			Func<byte, TState, T> fU8,
			Func<sbyte, TState, T> fI8,
			Func<short, TState, T> fI16,
			Func<ushort, TState, T> fU16,
			Func<int, TState, T> fI32,
			Func<uint, TState, T> fU32,
			Func<long, TState, T> fI64,
			Func<ulong, TState, T> fU64,
			Func<Half, TState, T> fF16,
			Func<float, TState, T> fF32,
			Func<double, TState, T> fF64) =>
		PrimitiveExtensions.Switch(Kind, this,
			self => fI8(self.AsI8(), state),
			self => fU8(self.AsU8(), state),
			self => fI16(self.AsI16(), state),
			self => fU16(self.AsU16(), state),
			self => fI32(self.AsI32(), state),
			self => fU32(self.AsU32(), state),
			self => fI64(self.AsI64(), state),
			self => fU64(self.AsU64(), state),
			self => fF16(self.AsF16(), state),
			self => fF32(self.AsF32(), state),
			self => fF64(self.AsF64(), state)
		);

	public void Call<TState>(
			TState state,
			Action<byte, TState> fU8,
			Action<sbyte, TState> fI8,
			Action<short, TState> fI16,
			Action<ushort, TState> fU16,
			Action<int, TState> fI32,
			Action<uint, TState> fU32,
			Action<long, TState> fI64,
			Action<ulong, TState> fU64,
			Action<Half, TState> fF16,
			Action<float, TState> fF32,
			Action<double, TState> fF64) =>
		PrimitiveExtensions.Switch<ValueTuple, TypedPrimitive>(Kind, this,
			self => { fI8(self.AsI8(), state); return default; },
			self => { fU8(self.AsU8(), state); return default; },
			self => { fI16(self.AsI16(), state); return default; },
			self => { fU16(self.AsU16(), state); return default; },
			self => { fI32(self.AsI32(), state); return default; },
			self => { fU32(self.AsU32(), state); return default; },
			self => { fI64(self.AsI64(), state); return default; },
			self => { fU64(self.AsU64(), state); return default; },
			self => { fF16(self.AsF16(), state); return default; },
			self => { fF32(self.AsF32(), state); return default; },
			self => { fF64(self.AsF64(), state); return default; }
		);

	private static T ThrowTypeMismatch<T>(PrimitiveKind actualKind) =>
		throw new InvalidOperationException($"Cannot convert primitive {actualKind} to type {typeof(T).Name}");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe ulong Serialize<T>(T value) where T : unmanaged
	{
		ulong ret = 0;

		// switch assembly is worse than if
		if (sizeof(T) == 1)
		{
			Unsafe.As<ulong, byte>(ref ret) = Unsafe.As<T, byte>(ref value);
		}
		else if (sizeof(T) == 2)
		{
			Unsafe.As<ulong, ushort>(ref ret) = Unsafe.As<T, ushort>(ref value);
		}
		else if (sizeof(T) == 4)
		{
			Unsafe.As<ulong, uint>(ref ret) = Unsafe.As<T, uint>(ref value);
		}
		else if (sizeof(T) == 8)
		{
			ret = Unsafe.As<T, ulong>(ref value);
		}
		else
		{
			_ = Throw();
		}

		return ret;

		static ulong Throw() => throw new ArgumentException("size of value must be 1, 2, 4, 8");
	}

	private static unsafe T Deserialize<T>(ulong value) where T : unmanaged, INumber<T>
	{
		Unsafe.SkipInit(out T ret);

		if (sizeof(T) == 1)
		{
			Unsafe.As<T, byte>(ref ret) = Unsafe.As<ulong, byte>(ref value);
		}
		else if (sizeof(T) == 2)
		{
			Unsafe.As<T, ushort>(ref ret) = Unsafe.As<ulong, ushort>(ref value);
		}
		else if (sizeof(T) == 4)
		{
			Unsafe.As<T, uint>(ref ret) = Unsafe.As<ulong, uint>(ref value);
		}
		else if (sizeof(T) == 8)
		{
			Unsafe.As<T, ulong>(ref ret) = value;
		}
		else
		{
			_ = Throw();
		}

		return ret;

		static ulong Throw() => throw new ArgumentException("size of value must be 1, 2, 4, 8");
	}
}

public sealed record LoadArg(int Index) : Instruction("ldarg", $"ldarg {Index}");
public sealed record LoadLocal(int Index) : Instruction("ldloc", $"ldloc {Index}");
public sealed record StoreLocal(int Index) : Instruction("stloc", $"stloc {Index}");

public sealed record LoadConst(TypedPrimitive Value) : Instruction("ldc", $"ldc.{Value.Kind.AsmRepr()} {Value}");

public sealed record Call(TypeSystem.FunctionSignature Signature) : Instruction("call", 
	$"call {Signature.Name}" +
	$"<{string.Join(", ", Signature.TypeParameters.Select(p => p.Name))}>" +
	$"({string.Join(", ", Signature.Parameters.Select(p => p.Type))})");

public sealed record Return() : Instruction("ret");

public sealed record Add(PrimitiveKind Kind) : Instruction("add", $"add.{Kind.AsmRepr()}");
public sealed record Subtract(PrimitiveKind Kind) : Instruction("sub", $"sub.{Kind.AsmRepr()}");
public sealed record Multiply(PrimitiveKind Kind) : Instruction("mul", $"mul.{Kind.AsmRepr()}");
public sealed record Divide(PrimitiveKind Kind) : Instruction("div", $"div.{Kind.AsmRepr()}");

public sealed record RightShift(PrimitiveKind Kind) : Instruction("bit.rsh", $"bit.rsh.{Kind.AsmRepr()}");
public sealed record LeftShift(PrimitiveKind Kind) : Instruction("bit.lsh", $"bit.lsh.{Kind.AsmRepr()}");
public sealed record BitAnd(PrimitiveKind Kind) : Instruction("bit.and", $"bit.and.{Kind.AsmRepr()}");
public sealed record BitOr(PrimitiveKind Kind) : Instruction("bit.or", $"bit.or.{Kind.AsmRepr()}");
public sealed record BitNot(PrimitiveKind Kind) : Instruction("bit.not", $"bit.not.{Kind.AsmRepr()}");

public sealed record CompareEqual(PrimitiveKind Kind) : Instruction("cmp.eq", $"cmp.eq.{Kind.AsmRepr()}");
public sealed record CompareLessThan(PrimitiveKind Kind) : Instruction("cmp.lt", $"cmp.lt.{Kind.AsmRepr()}");
public sealed record CompareGreaterThan(PrimitiveKind Kind) : Instruction("cmp.gt", $"cmp.gt.{Kind.AsmRepr()}");

public sealed record Jump(int Offset) : Instruction("jmp", $"jmp {Offset}");
public sealed record JumpTrue(int Offset) : Instruction("jmp.true", $"jmp.true {Offset}");
public sealed record JumpFalse(int Offset) : Instruction("jmp.false", $"jmp.false {Offset}");

public static class PrimitiveExtensions
{
	public static string AsmRepr(this PrimitiveKind primitive) => primitive switch
	{
		PrimitiveKind.I8 => "u8",
		PrimitiveKind.U8 => "u8",
		PrimitiveKind.I16 => "i16",
		PrimitiveKind.U16 => "u16",
		PrimitiveKind.I32 => "i32",
		PrimitiveKind.U32 => "u32",
		PrimitiveKind.I64 => "i64",
		PrimitiveKind.U64 => "u64",
		PrimitiveKind.F16 => "f16",
		PrimitiveKind.F32 => "f32",
		PrimitiveKind.F64 => "f64",
		_ => throw new UnreachableException()
	};

	public static T Switch<T, TState>(PrimitiveKind kind, TState state,
		Func<TState, T> fI8,
		Func<TState, T> fU8,
		Func<TState, T> fI16,
		Func<TState, T> fU16,
		Func<TState, T> fI32,
		Func<TState, T> fU32,
		Func<TState, T> fI64,
		Func<TState, T> fU64,
		Func<TState, T> fF16,
		Func<TState, T> fF32,
		Func<TState, T> fF64)
	{
		return kind switch
		{
			PrimitiveKind.I8 => fI8 is not null ? fI8(state) : ThrowNullArg(nameof(fI8)),
			PrimitiveKind.U8 => fU8 is not null ? fU8(state) : ThrowNullArg(nameof(fU8)),
			PrimitiveKind.I16 => fI16 is not null ? fI16(state) : ThrowNullArg(nameof(fI16)),
			PrimitiveKind.U16 => fU16 is not null ? fU16(state) : ThrowNullArg(nameof(fU16)),
			PrimitiveKind.I32 => fI32 is not null ? fI32(state) : ThrowNullArg(nameof(fI32)),
			PrimitiveKind.U32 => fU32 is not null ? fU32(state) : ThrowNullArg(nameof(fU32)),
			PrimitiveKind.I64 => fI64 is not null ? fI64(state) : ThrowNullArg(nameof(fI64)),
			PrimitiveKind.U64 => fU64 is not null ? fU64(state) : ThrowNullArg(nameof(fU64)),
			PrimitiveKind.F16 => fF16 is not null ? fF16(state) : ThrowNullArg(nameof(fF16)),
			PrimitiveKind.F32 => fF32 is not null ? fF32(state) : ThrowNullArg(nameof(fF32)),
			PrimitiveKind.F64 => fF64 is not null ? fF64(state) : ThrowNullArg(nameof(fF64)),
			PrimitiveKind.None or _ => throw new UnreachableException("Invalid primitive kind")
		};

		static T ThrowNullArg(string paramName) => throw new ArgumentNullException(paramName);

	}
}
