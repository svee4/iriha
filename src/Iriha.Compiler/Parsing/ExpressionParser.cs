using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;
using Iriha.Compiler.Parsing.Parselets;
using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;

namespace Iriha.Compiler.Parsing;

public sealed class ExpressionParser(Parser parser)
{
	private readonly Parser _parser = parser;

	private IDisposable? Scope(Precedence precedence) =>
		_parser.Scope($"{nameof(ParseExpression)}@{precedence}");

	public IExpression ParseExpression(Precedence precedence)
	{
		using var scope = Scope(precedence);

		var token = _parser.Peek();
		if (token is SemiColon)
		{
			_parser.ThrowAt(token, "Expected expression, got semicolon");
			throw new UnreachableException();
		}

		if (GetPrefixParseletForToken(token) is not { } prefixParselet)
		{
			_parser.ThrowAt(token, "No prefix parselet for token {Token}", token);
			throw new UnreachableException();
		}

		var expr = prefixParselet.Parse(_parser);

		while (GetInfixParseletForToken(_parser.Peek()) is { } infixParselet && precedence < infixParselet.GetPrecedence())
		{
			expr = infixParselet.Parse(_parser, expr);
		}

		return expr;
	}

#pragma warning disable format

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
		Justification = "One day might not be")]
	private IPrefixParselet? GetPrefixParseletForToken(LexerToken token) => token switch
	{
		Bang =>			new PrefixOperatorParselet<Bang>(),
		Minus =>		new PrefixOperatorParselet<Minus>(),
		Star =>			new PrefixOperatorParselet<Star>(),
		OpenParen =>	new GroupingParselet(),
		OpenBrace =>	new BlockExpressionParselet(),
		FuncKeyword =>	new FunctionExpressionParselet(),
		YieldKeyword => new YieldExpressionParselet(),
		ReturnKeyword =>	new ReturnExpressionParselet(),
		StringLiteral =>	new LiteralParselet<StringLiteral>(),
		NumericLiteral =>	new LiteralParselet<NumericLiteral>(),
		IdentifierLiteral =>	new IdentifierParselet(),
		UnderscoreKeyword =>	new DiscardExpressionParselet(),
		DollarSign =>	new PipeGroupingParselet(),
		LetKeyword or MutKeyword =>		new VariableDeclarationParselet(),
		_ => null
	};

	private IInfixParselet? GetInfixParseletForToken(LexerToken token) => token switch
	{
		Dot =>		new MemberAccessParselet(),
		Equal =>	new AssignmentParselet(),
		Plus =>		new BinaryOperatorParselet<Plus>(_parser),
		Minus =>	new BinaryOperatorParselet<Minus>(_parser),
		Star =>		new BinaryOperatorParselet<Star>(_parser),
		Slash =>	new BinaryOperatorParselet<Slash>(_parser),
		Ampersand =>	new BinaryOperatorParselet<Ampersand>(_parser),
		OpenParen =>	new FunctionCallParselet(),
		OpenBracket =>	new IndexerCallParselet(),
		DoubleEqual =>	new BinaryOperatorParselet<DoubleEqual>(_parser),
		NotEqual =>		new BinaryOperatorParselet<NotEqual>(_parser),
		OpenAngleBracket =>		new BinaryOperatorParselet<OpenAngleBracket>(_parser),
		CloseAngleBracket =>	new BinaryOperatorParselet<CloseAngleBracket>(_parser),
		DoubleCloseAngleBracket =>	new PipeParselet(),
		_ => null
	};

#pragma warning restore format

	//private static readonly FrozenDictionary<Type, IPrefixParselet> PrefixParselets = new Dictionary<Type, IPrefixParselet>
	//{
	//	{ typeof(OpenParen), new GroupingParselet() },
	//	{ typeof(Bang), new PrefixOperatorParselet<Bang>() },
	//	{ typeof(Minus), new PrefixOperatorParselet<Minus>() },
	//	{ typeof(Star), new PrefixOperatorParselet<Star>() },
	//	{ typeof(IdentifierLiteral), new IdentifierParselet() },
	//	{ typeof(StringLiteral), new LiteralParselet<StringLiteral>() },
	//	{ typeof(NumericLiteral), new LiteralParselet<NumericLiteral>() },
	//}.ToFrozenDictionary();

	//private static readonly FrozenDictionary<Type, IInfixParselet> InfixParselets = new Dictionary<Type, IInfixParselet>
	//{
	//	{ typeof(Equal), new AssignmentParselet() },
	//	{ typeof(OpenParen), new FunctionCallParselet() },
	//	{ typeof(Plus), new BinaryOperatorParselet<Plus>(Precedence.AdditionOrSubtraction) },
	//	{ typeof(Minus), new BinaryOperatorParselet<Minus>(Precedence.AdditionOrSubtraction) },
	//	{ typeof(Star), new BinaryOperatorParselet<Star>(Precedence.MultiplicationOrDivision) },
	//	{ typeof(Slash), new BinaryOperatorParselet<Slash>(Precedence.MultiplicationOrDivision) },
	//}.ToFrozenDictionary();
}
