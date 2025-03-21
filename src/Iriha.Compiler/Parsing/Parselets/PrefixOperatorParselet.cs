using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

// same as BinaryOperatorParselet, this parselet is used for all prefixed unary operators
// as listed in the switch
public sealed class PrefixOperatorParselet<TToken> : ParseletBase, IPrefixParselet where TToken : LexerToken
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);

		var token = parser.Eat<TToken>();
		var expr = parser.ParseSubExpression(Precedence.Prefix);

		return token switch
		{
			Bang => new LogicalNegationExpression(expr),
			Minus => new MathematicalNegationExpression(expr),
			Star => new DereferenceExpression(expr),
			_ => parser.ThrowAt<IExpression>(token, "Unimplemented or unsupported unary prefix operator {Token}", token)
		};
	}
}
