using Iriha.Compiler.Infra;
using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing.Nodes;
using Iriha.Compiler.Parsing.Parselets;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Iriha.Compiler.Parsing;

/*
 * Primary parser is a recursive descent parser.
 * Expression are parsed using operator precedence parsing based on this article
 * https://journal.stuffwithstuff.com/2011/03/19/pratt-parsers-expression-parsing-made-easy/
 * and this c# implementation
 * https://github.com/jfcardinal/BantamCs
 * which is encapsulated in ExpressionParser.
 * 
 * https://en.wikipedia.org/wiki/Recursive_descent_parser
 * https://en.wikipedia.org/wiki/Operator-precedence_parser
*/

public sealed class Parser
{
	private List<LexerToken> _tokens = null!;
	private int _position;

	internal ExpressionParser ExpressionParser { get; }
	internal ILogger<Parser> Logger { get; }
	internal Compilation Compilation { get; }

	public Parser(Compilation compilation, ILogger<Parser> logger)
	{
		Compilation = compilation;
		Logger = logger;
		ExpressionParser = new ExpressionParser(this);
	}

	public SyntaxTree Parse(List<LexerToken> tokens)
	{
		_tokens = tokens;

		var tree = new SyntaxTree();
		while (Peek() is not EndOfFile)
		{
			tree.TopLevelStatements.Add(ParseTopLevelStatement());
		}

		return tree;
	}

	public LexerToken Peek() => _tokens[_position];
	public LexerToken Peek(int skip) => _tokens[_position + skip];
	public LexerToken Eat() => _tokens[_position++];

	public T Peek<T>() where T : LexerToken =>
		Peek() is T v ? v : ThrowAt<T>(Peek(), "Expected {Expected}, got {Actual}", typeof(T), Peek().GetType());

	public T Eat<T>() where T : LexerToken =>
		Peek() is T ? (T)Eat() : ThrowAt<T>(Peek(), "Expected {Expected}, got {Actual}", typeof(T), Eat().GetType());

	public LexerToken Eat<T1, T2>() where T1 : LexerToken where T2 : LexerToken => 
		Eat() switch
		{
			T1 v => v,
			T2 v => v,
			var v => ThrowAt<LexerToken>(v, "Expected {Expected1} or {Expected2}, got {Actual}", typeof(T1), typeof(T2), v.GetType())
		};

	public T? EatIf<T>() where T : LexerToken =>
		Peek() is T ? Eat<T>() : null;

	public IDisposable? Scope([CallerMemberName] string scope = "(unknown caller)") =>
		Logger.BeginScope(scope);

	public ITopLevelStatement ParseTopLevelStatement()
	{
		using var scope = Scope();
		return Peek() switch
		{
			FuncKeyword => ParseFunctionDeclaration(),
			StructKeyword => ParseStructDeclaration(),
			ImplKeyword => ParseImplBlock(),
			var token => ThrowAt<ITopLevelStatement>(token, "Expected func or struct, got {Actual}", token.GetType())
		};
	}

	public Identifier ParseIdentifier()
	{
		var first = Eat<IdentifierLiteral>().Value;
		if (EatIf<DoubleColon>() is not null)
		{
			var second = Eat<IdentifierLiteral>().Value;
			return new Identifier(second, first);
		}
		else
		{
			return new Identifier(first, null);
		}
	}

	private ImplBlockStatement ParseImplBlock()
	{
		using var scope = Scope();

		_ = Eat<ImplKeyword>();

		var typeParams = Peek() is OpenAngleBracket
			? ParseTypeParameters()
			: [];

		var forStruct = Eat<IdentifierLiteral>().Value;
		throw new NotImplementedException();
		//var structGenerics = Peek() is OpenAngleBracket
		//	? ParseTypeArguments()
		//	: [];

	}

	private EquatableArray<TypeParameter> ParseTypeParameters()
	{
		using var scope = Scope();

		List<TypeParameter> typeParameters = [];

		_ = Eat<OpenAngleBracket>();

		while (Peek() is not CloseAngleBracket)
		{
			var tpName = Eat<IdentifierLiteral>().Value;
			// TODO: type parameter constraints
			typeParameters.Add(new TypeParameter(new Identifier(tpName, null), []));
		}

		_ = Eat<CloseAngleBracket>();
		return typeParameters;
	}

	private sealed record FunctionComponents(
		Identifier? Name,
		TypeRef ReturnType,
		EquatableArray<TypeParameter> TypeParameters,
		EquatableArray<FunctionParameter> Parameters,
		EquatableArray<IExpressionStatement> Statements);

	/// <summary>
	/// Shared logic for function declarations and function expressions
	/// </summary>
	private FunctionComponents ParseFunctionComponents(bool named)
	{
		_ = Eat<FuncKeyword>();

		var name = named ? new Identifier(Eat<IdentifierLiteral>().Value, Compilation.ModuleName) : null;

		EquatableArray<TypeParameter> typeParams = Peek() is OpenAngleBracket
			? ParseTypeParameters()
			: [];

		List<FunctionParameter> parameters = [];
		using (Scope("[Parameters]"))
		{
			_ = Eat<OpenParen>();

			while (Peek() is not CloseParen)
			{
				parameters.Add(ParseFunctionParameter());
				_ = EatIf<Comma>();
			}

			_ = Eat<CloseParen>();
		}

		_ = Eat<Colon>();
		var returnType = ParseTypeRef();

		List<IExpressionStatement> statements = [];
		using (Scope("[Statements]"))
		{
			_ = Eat<OpenBrace>();

			while (Peek() is not CloseBrace)
			{
				statements.Add(ParseExpressionStatement());
			}

			_ = Eat<CloseBrace>();
		}

		return new FunctionComponents(
			name,
			returnType,
			typeParams,
			parameters,
			statements);

		FunctionParameter ParseFunctionParameter()
		{
			using var scope = Scope(nameof(ParseFunctionParameter));

			var identifier = Eat<IdentifierLiteral>().Value;
			_ = Eat<Colon>();
			var type = ParseTypeRef();

			return new FunctionParameter(new Identifier(identifier, null), type);
		}
	}

	public FunctionDeclaration ParseFunctionDeclaration()
	{
		using var scope = Scope();
		var components = ParseFunctionComponents(true);

		if (components.Name is null)
		{
			throw new UnreachableException("Function name should not be null for function statement");
		}

		return new FunctionDeclaration(
			components.ReturnType,
			components.Name,
			components.TypeParameters,
			components.Parameters,
			components.Statements);
	}


	public FunctionExpression ParseFunctionExpression()
	{
		using var scope = Scope();
		var comps = ParseFunctionComponents(false);

		if (comps.Name is not null)
		{
			throw new UnreachableException("Function name should be null for function expression");
		}

		return new FunctionExpression(
			comps.ReturnType,
			comps.TypeParameters,
			comps.Parameters,
			comps.Statements);
	}

	public IExpressionStatement ParseExpressionStatement()
	{
		using var scope = Scope();

		var token = Peek();
		return ParseExpression() switch
		{
			IExpressionStatement st => st,
			var res => ThrowAt<IExpressionStatement>(token, "Expected expression statement, got {Actual}", res)
		};
	}

	public IExpression ParseExpression() =>
		ExpressionParser.ParseExpression(Precedence.None);

	public IExpression ParseSubExpression(Precedence precedence) =>
		ExpressionParser.ParseExpression(precedence);

	public StructDeclaration ParseStructDeclaration()
	{
		using var scope = Scope();

		_ = Eat<StructKeyword>();
		var structName = Eat<IdentifierLiteral>().Value;

		EquatableArray<TypeParameter> typeParams = Peek() is OpenAngleBracket
			? ParseTypeParameters()
			: [];

		_ = Eat<OpenBrace>();

		List<StructFieldDeclaration> fields = [];
		while (Peek() is not CloseBrace)
		{
			var fieldName = Eat<IdentifierLiteral>().Value;
			_ = Eat<Colon>();

			var type = ParseTypeRef();
			_ = Eat<SemiColon>();

			fields.Add(new StructFieldDeclaration(new Identifier(fieldName, null), type));
		}

		_ = Eat<CloseBrace>();

		return new StructDeclaration(new Identifier(structName, null), typeParams, fields);
	}

	public VariableDeclarationStatement ParseVariableDeclaration()
	{
		using var scope = Scope();

		var varType = Eat<LetKeyword, MutKeyword>() switch
		{
			LetKeyword => VariableKind.Let,
			MutKeyword => VariableKind.Mut,
			_ => throw new UnreachableException()
		};

		var name = Eat<IdentifierLiteral>().Value;

		_ = Eat<Colon>();
		var type = ParseTypeRef();

		_ = Eat<Equal>();
		var expr = ParseExpression();

		return new VariableDeclarationStatement(new Identifier(name, null), type, varType, expr);
	}

	public TypeRef ParseTypeRef()
	{
		using var scope = Scope();

		var pcount = 0;
		while (EatIf<Ampersand>() is not null)
		{
			pcount++;
		}


		Identifier ident;
		string? original = null;

		var next = Peek();
		if (next is IdentifierLiteral)
		{
			ident = ParseIdentifier();
		}
		else if (next is TypeKeyword kw)
		{
			_ = Eat<TypeKeyword>();
			ident = Identifier.CoreLib(kw.CoreLibType);
			original = kw.Keyword;
		}
		else
		{
			ThrowAt(next, "Expected {Expected1} or {Expected2}, got {Actual}",
				typeof(IdentifierLiteral), typeof(TypeKeyword), next.GetType());
			throw new UnreachableException();
		}

		List<TypeRef> generics = [];
		if (EatIf<OpenAngleBracket>() is not null)
		{
			generics.Add(ParseTypeRef());
			_ = Eat<CloseAngleBracket>();
		}

		return new TypeRef(ident, pcount, generics, original);
	}

	[SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Close enough")]
	[DoesNotReturn]
	internal T Throw<T>(int line, int column, string format, params object?[] args)
	{
		format = $"(at {{Line}}:{{Column}}): {format}";
		args = [line, column, .. args];

		Logger.LogError(format, args);
		throw new ParserException(new LogValuesFormatter(format).Format(args));
	}

	internal void Throw(int line, int column, string format, params object?[] args) => Throw<object>(line, column, format, args);

	internal T ThrowAt<T>(LexerToken token, string format, params object?[] args) =>
		Throw<T>(token.Line, token.Column, format, args);

	internal void ThrowAt(LexerToken token, string format, params object?[] args) =>
		Throw<object>(token.Line, token.Column, format, args);

}
