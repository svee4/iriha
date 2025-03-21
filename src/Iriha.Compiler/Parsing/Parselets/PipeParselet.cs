using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

public sealed class PipeParselet : ParseletBase, IInfixParselet
{
	public Precedence GetPrecedence() => Precedence.Pipe;

	public IExpression Parse(Parser parser, IExpression left)
	{
		using var scope = Scope(parser);

		_ = parser.Eat<DoubleCloseAngleBracket>();
		var to = parser.ParseSubExpression(Precedence.Pipe);

		return new PipeExpression(left, to);
	}
}
