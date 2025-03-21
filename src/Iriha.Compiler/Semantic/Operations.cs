using System.Diagnostics;

namespace Iriha.Compiler.Semantic;

public interface IOperation
{
	TypeReferenceSymbol Type { get; }
}

public sealed record VariableDeclarationOperation(IVariableSymbol Variable, IOperation? Initializer) : IOperation
{
	public TypeReferenceSymbol Type => Variable switch
	{
		GlobalVariableSymbol g => g.Type,
		LocalVariableSymbol l => l.Type,
		var v => throw new UnreachableException($"IVariableSymbol {v?.GetType()} not implemented")
	};
}

public sealed record InvocationOperation(IFunctionSymbol Target, List<IOperation> Arguments) : IOperation
{
	public TypeReferenceSymbol Type => Target.ReturnType;
}

public sealed record AssignmentOperation(IOperation Target, IOperation Value) : IOperation
{
	public TypeReferenceSymbol Type => Value.Type;
}

public sealed record MemberAccessOperation(IOperation Source, IMemberSymbol Member) : IOperation
{
	public TypeReferenceSymbol Type => Member.Type;
}

public sealed record ReturnOperation(IOperation? Value) : IOperation
{
	public TypeReferenceSymbol Type => Value?.Type ?? new VoidTypeReferenceSymbol();
}

public sealed record YieldOperation(IOperation? Value) : IOperation
{
	public TypeReferenceSymbol Type => Value?.Type ?? new VoidTypeReferenceSymbol();
}

public sealed record BlockExpressionOperation(List<IOperation> Operations, TypeReferenceSymbol Type) : IOperation;
public sealed record DiscardOperation(IOperation Value) : IOperation
{
	public TypeReferenceSymbol Type => Value.Type;
}

public abstract record LiteralOperation(TypeReferenceSymbol Type) : IOperation;
public sealed record StringLiteralOperation(string Value, TypeReferenceSymbol StringType) : LiteralOperation(StringType);
public sealed record IntegerLiteralOperation(int Value, TypeReferenceSymbol IntegerType) : LiteralOperation(IntegerType);

public abstract record BinaryOperation : IOperation
{
	public IOperation Left { get; }
	public IOperation Right { get; }

	protected BinaryOperation(IOperation left, IOperation right)
	{
		if (left.Type != right.Type)
		{
			throw new ArgumentException(
				$"Binary operator arguments {left} and {right} types {left.Type} and {right.Type} are not compatible");
		}

		Left = left;
		Right = right;
	}

	public TypeReferenceSymbol Type => Left.Type;
}

public sealed record AdditionOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record SubtractionOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record MultiplicationOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record DivisionOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record BitwiseAndOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record BitwiseOrOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record BitwiseXorOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record BitwiseLeftShiftOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record BitwiseRightShiftOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record LogicalAndOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record LogicalOrOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record LogicalLessThanOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record LogicalGreaterThanOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record LogicalEqualOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);
public sealed record LogicalNotEqualOperation(IOperation Left, IOperation Right) : BinaryOperation(Left, Right);

public abstract record UnaryOperation(IOperation Source, TypeReferenceSymbol ResultType) : IOperation
{
	public TypeReferenceSymbol Type => Source.Type;
}

public sealed record LogicalNegationOperation(IOperation Source, TypeReferenceSymbol ResultType) : UnaryOperation(Source, ResultType);
public sealed record MathematicalNegationOperation(IOperation Source, TypeReferenceSymbol ResultType) : UnaryOperation(Source, ResultType);
public sealed record BitwiseNotOperation(IOperation Source, TypeReferenceSymbol ResultType) : UnaryOperation(Source, ResultType);
public sealed record DereferenceOperation(IOperation Source, TypeReferenceSymbol ResultType) : UnaryOperation(Source, ResultType);

////////////////////////////

public sealed record LocalVariableReferenceOperation(LocalVariableSymbol Symbol) : IOperation
{
	public TypeReferenceSymbol Type => Symbol.Type;
}

public sealed record GlobalVariableReferenceOperation(GlobalVariableSymbol Symbol) : IOperation
{
	public TypeReferenceSymbol Type => Symbol.Type;
}

public sealed record FunctionReferenceOperation(GlobalFunctionSymbol Symbol) : IOperation
{
	public TypeReferenceSymbol Type => throw new NotImplementedException();
}

public sealed record StructReferenceOperation(StructSymbol Symbol) : IOperation
{
	public StructTypeReferenceSymbol Type => throw new NotImplementedException();
	TypeReferenceSymbol IOperation.Type => Type;
}

public sealed record TraitReferenceOperation(TraitSymbol Symbol) : IOperation
{
	public TraitReferenceTypeSymbol Type => throw new NotImplementedException();
	TypeReferenceSymbol IOperation.Type => Type;
}
