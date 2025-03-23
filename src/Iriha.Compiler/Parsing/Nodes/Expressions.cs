using Iriha.Compiler.Infra;

namespace Iriha.Compiler.Parsing.Nodes;

public interface IExpression;

public interface IExpressionStatement : IExpression, IStatement;

public sealed record FunctionExpression(TypeRef ReturnType, EquatableArray<TypeParameter> TypeParameters,
	EquatableArray<FunctionParameter> Parameters, EquatableArray<IExpressionStatement> Statements) : IExpression;

public sealed record PipeExpression(IExpression From, IExpression To) : IExpression;
public sealed record TupleCreationExpression(EquatableArray<IExpression> Expressions) : IExpression;

public sealed record FunctionCallExpression(IExpression FunctionExpression, EquatableArray<FunctionArgument> Arguments) : IExpressionStatement;
public sealed record IndexerCallExpression(IExpression Source, IExpression Indexer) : IExpression;
public sealed record FunctionArgument(IExpression Value);

public sealed record AssignmentExpression(IExpression Target, IExpression Value) : IExpressionStatement;
public sealed record DiscardExpression(IExpression Expression) : IExpressionStatement;

public sealed record IdentifierExpression(Identifier Identifier) : IExpression;
public sealed record BlockExpression(List<IExpressionStatement> Statements) : IExpression;

public sealed record ReturnExpression(IExpression? Value) : IExpressionStatement;
public sealed record YieldExpression(IExpression? Value) : IExpressionStatement;

public sealed record MemberAccessExpression(IExpression Source, IdentifierExpression Member) : IExpression;
public sealed record EmptyStatement : IExpressionStatement;

public sealed record StringLiteralExpression(string Value) : IExpression;
public sealed record IntLiteralExpression(int Value) : IExpression;

public abstract record BinaryExpression(IExpression Left, IExpression Right) : IExpression;

public sealed record AdditionExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record SubtractionExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record MultiplicationExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record DivisionExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);

public sealed record LogicalAndExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record LogicalOrExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record LogicalLessThanExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record LogicalGreaterThanExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record LogicalEqualExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);
public sealed record LogicalNotEqualExpression(IExpression Left, IExpression Right) : BinaryExpression(Left, Right);

public abstract record UnaryExpression : IExpression;
public abstract record PrefixUnaryExpression : UnaryExpression;

public sealed record LogicalNegationExpression(IExpression Source) : PrefixUnaryExpression;
public sealed record MathematicalNegationExpression(IExpression Source) : PrefixUnaryExpression;
public sealed record BitwiseNotExpression(IExpression Source) : PrefixUnaryExpression;
public sealed record DereferenceExpression(IExpression Source) : PrefixUnaryExpression;
