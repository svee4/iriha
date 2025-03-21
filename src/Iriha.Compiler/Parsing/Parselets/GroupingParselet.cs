using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class GroupingParselet : ParseletBase, IPrefixParselet
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);

		_ = parser.Eat<OpenBrace>();
		var expr = parser.ParseSubExpression(Precedence.None);
		_ = parser.Eat<CloseBrace>();

		return expr;
	}
}
