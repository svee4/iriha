using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class FunctionExpressionParselet : ParseletBase, IPrefixParselet
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);
		return parser.ParseFunctionExpression();
	}
}
