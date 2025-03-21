using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Iriha.Compiler.Lexing;

public sealed class Lexer
{
	private const string PosLog = "{Line}:{Column}:";

	private readonly ILogger<Lexer> _logger;

	private string _source = null!;
	private int _index;
	private int _line = 1;
	private int _column = 1;

	private readonly FrozenDictionary<string, Func<Keyword>> _keywords;

	public Lexer(ILogger<Lexer> logger)
	{
		_logger = logger;
		_keywords = typeof(Lexer).Assembly.GetTypes()
			.Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Keyword)))
			.Select(Activator.CreateInstance)
			.Cast<Keyword>()
			.ToDictionary(
				kw => kw.Value,
				kw => typeof(Lexer)
					.GetMethod("Token", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
					.MakeGenericMethod(kw.GetType()).CreateDelegate<Func<Keyword>>(this))
			.ToFrozenDictionary();
	}

	private ReadOnlySpan<char> Remaining => _source.AsSpan()[_index..];

	private char Peek()
	{
		AssertValidPeek(0);
		return _source[_index];
	}

	private char Eat()
	{
		var c = Peek();
		_index++;
		_column++;
		return c;
	}

	private ReadOnlySpan<char> Peek(int count)
	{
		AssertValidPeek(count);
		return _source.AsSpan()[_index..(_index + count)];
	}

	private ReadOnlySpan<char> Eat(int count)
	{
		var s = Peek(count);
		_index += count;
		_column += count;
		return s;
	}

	private void AssertValidPeek(int count)
	{
		var i = _index + count;
		if (i >= _source.Length)
			ThrowLexerException("Attempt to read past the end of source");
	}

	private void AssertExpected(char actual, params ReadOnlySpan<char> expected)
	{
		Debug.Assert(expected.Length > 0);

		if (expected.Length == 1)
		{
			if (expected[0] != actual)
			{
				ThrowLexerException($"Expected '{expected}', got '{actual}'");
			}
		}
		else
		{
			if (!expected.Contains(actual))
			{
				var sb = (List<string>)[];
				foreach (var c in expected)
				{
					sb.Add($"'{c}'");
				}

				ThrowLexerException($"Expected any of {string.Join(", ", sb)}, got '{actual}'");
			}
		}
	}

	private void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string s = "")
	{
		if (!condition)
			ThrowLexerException(s);
	}

	private void ThrowLexerException(string message) => throw new LexerException(_line, _column, message);

	private Keyword? GetKeyword(string keyword) =>
		_keywords.TryGetValue(keyword, out var f) ? f() : null;

	public List<LexerToken> Parse(string source)
	{
		ArgumentNullException.ThrowIfNull(source);
		_source = source;

		List<LexerToken> tokens = [];

		while (true)
		{
			if (_index == _source.Length)
				break;

			if (Peek() == '\n')
			{
				_ = Eat();
				_line++;
				_column = 0;
				continue;
			}

			if (char.IsWhiteSpace(Peek()))
			{
				_ = Eat();
				_column++;
				continue;
			}

			if (Peek() == '#')
			{
				do
				{
					_ = Eat();
				} while (Peek() != '\n');

				_ = Eat();
				_line++;
				_column = 0;
				continue;
			}

			LexerToken? token = Peek() switch
			{
				'(' => Token<OpenParen>(),
				')' => Token<CloseParen>(),
				'[' => Token<OpenBracket>(),
				']' => Token<CloseBracket>(),
				'{' => Token<OpenBrace>(),
				'}' => Token<CloseBrace>(),
				'<' => Token<OpenAngleBracket>(),
				'>' => Token<CloseAngleBracket>(),
				',' => Token<Comma>(),
				'.' => Token<Dot>(),
				'+' => Token<Plus>(),
				'-' => Token<Minus>(),
				'*' => Token<Star>(),
				'/' => Token<Slash>(),
				'%' => Token<Percent>(),
				';' => Token<SemiColon>(),
				'_' => Token<UnderscoreKeyword>(),
				'&' => Token<Ampersand>(),
				'|' => Token<Pipe>(),
				':' when Peek(2) is not "::" => Token<Colon>(),
				'=' when Peek(2) is not "==" => Token<Equal>(),
				'!' when Peek(2) is not "!=" => Token<Bang>(),
				_ => null
			};

			if (token is not null)
			{
				_ = Eat();
				tokens.Add(token);
				continue;
			}

			token = Peek() switch
			{
				'"' => ReadStringLiteral(),
				>= '0' and <= '9' => ReadNumericLiteral(),
				_ => null
			};

			if (token is not null)
			{
				tokens.Add(token);
				continue;
			}

			token = Peek(2) switch
			{
				"::" => Token<DoubleColon>(),
				"==" => Token<DoubleEqual>(),
				"!=" => Token<NotEqual>(),
				_ => null
			};

			if (token is not null)
			{
				_ = Eat(2);
				tokens.Add(token);
				continue;
			}

			var str = ReadIdentifierOrKeyword();

			if (GetKeyword(str) is { } keyword)
			{
				tokens.Add(keyword);
			}
			else
			{
				tokens.Add(new IdentifierLiteral(str) { Line = _line, Column = _column });
			}
		}

		tokens.Add(Token<EndOfFile>());

		return tokens;
	}

	private StringLiteral ReadStringLiteral()
	{
		_logger.LogDebug($"{PosLog} Reading string literal", _line, _column);

		var startQuote = Eat();
		AssertExpected(startQuote, '"');

		var untilEndQuote = Remaining.IndexOf('"');
		if (untilEndQuote == -1)
			ThrowLexerException("Unterminated string literal");

		var literal = Eat(untilEndQuote).ToString();
		return new StringLiteral(literal) { Line = _line, Column = _column };
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1870:Use a cached 'SearchValues' instance",
		Justification = "All in due time")]
	private NumericLiteral ReadNumericLiteral()
	{
		_logger.LogDebug($"{PosLog} Reading numeric literal", _line, _column);
		Assert(Peek() is >= '0' and <= '9');

		var readUntil = Remaining.IndexOfAnyExcept(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'/*, '.'*/]);
		if (readUntil == -1)
		{
			ThrowLexerException("Numeric literal cannot end the input");
		}

		var literal = Eat(readUntil);
		var intValue = int.Parse(literal, CultureInfo.InvariantCulture);

		return new NumericLiteral(intValue) { Line = _line, Column = _column };
	}

	private string ReadIdentifierOrKeyword()
	{
		_logger.LogDebug($"{PosLog} Reading identifier or keyword", _line, _column);

		Assert(Constants.ValidIdentifierInitialChars.Contains(Peek()));

		var span = Remaining;
		Assert(span.Length > 0);

		var length = span.IndexOfAnyExcept(Constants.ValidIdentifierChars);
		return Eat(length).ToString();
	}

	private T Token<T>() where T : LexerToken
	{
		var instance = Activator.CreateInstance<T>();
		typeof(T).GetProperty(nameof(LexerToken.Line))!.SetValue(instance, _line);
		typeof(T).GetProperty(nameof(LexerToken.Column))!.SetValue(instance, _column);
		return instance;
	}
}
