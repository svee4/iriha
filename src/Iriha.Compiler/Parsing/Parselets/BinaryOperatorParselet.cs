using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

// EVERYTHING IS LEFT ASSOCIATIVE
// this class is used for all binary operators as listed in the switch
public sealed class BinaryOperatorParselet<TToken>(Parser parser) : ParseletBase, IInfixParselet where TToken : LexerToken
{
	private readonly Parser _parser = parser;

	public Precedence GetPrecedence() => GetNextOperatorPrecedence(_parser);

	private static Precedence GetNextOperatorPrecedence(Parser parser) => (parser.Peek<TToken>(), parser.Peek(1)) switch
	{
		(Ampersand, Ampersand) => Precedence.LogicalAnd,
		(Pipe, Pipe) => Precedence.LogicalOr,
		var (token1, token2) => token1 switch
		{
			Plus or Minus => Precedence.Additive,
			Star or Slash => Precedence.Multiplicative,
			Ampersand => Precedence.BitwiseAnd,
			Pipe => Precedence.BitwiseOr,
			OpenAngleBracket or CloseAngleBracket => Precedence.LogicalComparison,
			DoubleEqual or NotEqual => Precedence.LogicalEquality,
			_ => parser.ThrowAt<Precedence>(token1, "Undefined precedence for tokens {Token1} and {Token2}", token1, token2),
		}
	};

	public IExpression Parse(Parser parser, IExpression left)
	{
		using var scope = Scope(parser);

		var precedence = GetPrecedence();
		var token = parser.Eat<TToken>();

		LexerToken? token2 = token switch
		{
			Ampersand => parser.EatIf<Ampersand>(),
			Pipe => parser.EatIf<Pipe>(),
			_ => null
		};

		var expr = parser.ParseSubExpression(precedence);

		return (token, token2) switch
		{
			(Ampersand, Ampersand) => new LogicalAndExpression(left, expr),
			(Pipe, Pipe) => new LogicalOrExpression(left, expr),
			_ => token switch
			{
				Plus => new AdditionExpression(left, expr),
				Minus => new SubtractionExpression(left, expr),
				Star => new MultiplicationExpression(left, expr),
				Slash => new DivisionExpression(left, expr),
				Ampersand => new BitwiseAndExpression(left, expr),
				Pipe => new BitwiseOrExpression(left, expr),
				OpenAngleBracket => new LogicalLessThanExpression(left, expr),
				CloseAngleBracket => new LogicalGreaterThanExpression(left, expr),
				DoubleEqual => new LogicalEqualExpression(left, expr),
				NotEqual => new LogicalNotEqualExpression(left, expr),
				_ => parser.ThrowAt<IExpression>(token, "Unsupported binary operator combination {Token}, {Token2}", token, token2)
			}
		};
	}
}
