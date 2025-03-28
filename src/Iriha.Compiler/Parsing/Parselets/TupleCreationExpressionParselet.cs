using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class TupleCreationExpressionParselet : ParseletBase, IPrefixParselet
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);

		_ = parser.Eat<DollarSign>();
		_ = parser.Eat<OpenParen>();

		List<IExpression> expressions = [parser.ParseExpression()];
		while (parser.EatIf<Comma>() is not null)
		{
			expressions.Add(parser.ParseExpression());
		}

		_ = parser.Eat<CloseParen>();

		return new TupleCreationExpression(expressions);
	}
}
