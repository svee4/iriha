using Iriha.Compiler.Infra;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public interface IPrefixParselet
{
	IExpression Parse(Parser parser);
}

public interface IInfixParselet
{
	IExpression Parse(Parser parser, IExpression left);
	Precedence GetPrecedence();
}

// precedence rules are heavily inspired by C# rules
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/#operator-precedence
public enum Precedence
{
	None = 0,
	Assignment = 1,
	LogicalOr, // ||
	LogicalAnd, // &&
	BitwiseOr, // |
	BitwiseXor, // ^
	BitwiseAnd, // &
	LogicalEquality, // ==, !=
	LogicalComparison, // <, >
	Additive, // +, -
	Multiplicative, // *, /
	Prefix, // -, !, ~
	Postfix, // none yet
	Primary // function call, indexer call, member access
}

public abstract class ParseletBase
{
	protected IDisposable? Scope(Parser parser) => parser.Scope(Helpers.GetPrettyTypeName(GetType()));
}

