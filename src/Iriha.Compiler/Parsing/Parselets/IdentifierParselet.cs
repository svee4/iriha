using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class IdentifierParselet : ParseletBase, IPrefixParselet
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);
		var value = parser.ParseIdentifier();
		return new IdentifierExpression(value);
	}
}
