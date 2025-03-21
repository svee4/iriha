using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class VariableDeclarationParselet : ParseletBase, IPrefixParselet
{
	public IExpression Parse(Parser parser)
	{
		using var scope = Scope(parser);
		var expr = parser.ParseVariableDeclaration();
		_ = parser.EatIf<SemiColon>();
		return expr;
	}
}
