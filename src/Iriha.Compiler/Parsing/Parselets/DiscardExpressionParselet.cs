using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class DiscardExpressionParselet : ParseletBase, IPrefixParselet
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);

		_ = parser.Eat<UnderscoreKeyword>();
		_ = parser.Eat<Equal>();

		var expr = parser.ParseExpression();

		_ = parser.EatIf<SemiColon>();

		return expr;
	}
}
