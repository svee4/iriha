using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class BlockExpressionParselet : ParseletBase, IPrefixParselet
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);
		_ = parser.Eat<OpenBrace>();

		List<IExpressionStatement> statements = [];
		while (parser.Peek() is not CloseBrace)
		{
			statements.Add(parser.ParseExpressionStatement());
		}

		_ = parser.Eat<CloseBrace>();
		return new BlockExpression(statements);
	}
}
