using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Parsing.Parselets;

// a function does not need to be called with its name:
// var x: int = (func _(): int { return 3; })(); should be valid (in the future when function expression are implemented),
// as well as GetHof()(); where GetHof is func GetHof(): TODO: how to express function types?
public sealed class FunctionCallParselet : ParseletBase, IInfixParselet
{
	public Precedence GetPrecedence() => Precedence.Primary;

	public IExpression Parse(Parser parser, IExpression left)
	{
		using var scope = Scope(parser);

		_ = parser.Eat<OpenParen>();
		List<FunctionArgument> arguments = [];

		while (parser.Peek() is not CloseParen)
		{
			arguments.Add(new FunctionArgument(parser.ParseSubExpression(Precedence.None)));
			_ = parser.EatIf<Comma>();
		}

		// single trailing commas in parameter list are allowed

		_ = parser.Eat<CloseParen>();
		_ = parser.EatIf<SemiColon>();

		return new FunctionCallExpression(left, arguments);
	}
}
