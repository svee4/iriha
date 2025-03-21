namespace Iriha.Compiler.Lexing;

public abstract record LexerToken
{
	public required int Line { get; init; }
	public required int Column { get; init; }
}

public sealed record EndOfFile : LexerToken;

public abstract record Literal : LexerToken;
public sealed record StringLiteral(string Value) : Literal;
public sealed record NumericLiteral(int Value) : Literal;
public sealed record IdentifierLiteral(string Value) : Literal;

public abstract record Keyword(string Value) : LexerToken;
public sealed record ReturnKeyword() : Keyword("return");
public sealed record YieldKeyword() : Keyword("yield");
public sealed record StructKeyword() : Keyword("struct");
public sealed record ImplKeyword() : Keyword("impl");
public sealed record FuncKeyword() : Keyword("func");
public sealed record LetKeyword() : Keyword("let");
public sealed record MutKeyword() : Keyword("mut");
public sealed record NeverKeyword() : Keyword("never");
public sealed record UnderscoreKeyword() : Keyword("_");
public sealed record ArrowKeyword() : Keyword("->");

public abstract record TypeKeyword(string Keyword, string CoreLibType) : Keyword(Keyword);
public sealed record BoolKeyword() : TypeKeyword("bool", "Boolean");
public sealed record ByteKeyword() : TypeKeyword("byte", "Int8");
public sealed record SByteKeyword() : TypeKeyword("sbyte", "Uint8");
public sealed record ShortKeyword() : TypeKeyword("short", "Int16");
public sealed record UShortKeyword() : TypeKeyword("ushort", "UInt16");
public sealed record IntKeyword() : TypeKeyword("int", "Int32");
public sealed record UIntKeyword() : TypeKeyword("uint", "UInt32");
public sealed record LongKeyword() : TypeKeyword("long", "Int64");
public sealed record ULongKeyword() : TypeKeyword("ulong", "UInt64");
public sealed record VoidKeyword() : TypeKeyword("void", "Void");
public sealed record StringKeyword() : TypeKeyword("string", "String");

public sealed record OpenParen : LexerToken;
public sealed record CloseParen : LexerToken;
public sealed record OpenBracket : LexerToken;
public sealed record CloseBracket : LexerToken;
public sealed record OpenBrace : LexerToken;
public sealed record CloseBrace : LexerToken;
public sealed record OpenAngleBracket : LexerToken;
public sealed record CloseAngleBracket : LexerToken;
public sealed record DoubleCloseAngleBracket : LexerToken; // >>

public sealed record Comma : LexerToken;
public sealed record Dot : LexerToken;
public sealed record DollarSign : LexerToken;

public sealed record Plus : LexerToken;
public sealed record Minus : LexerToken;
public sealed record Star : LexerToken;
public sealed record Slash : LexerToken;
public sealed record Percent : LexerToken;
public sealed record SemiColon : LexerToken;
public sealed record Colon : LexerToken;
public sealed record DoubleColon : LexerToken;
public sealed record Ampersand : LexerToken;
public sealed record Pipe : LexerToken;

public sealed record Bang : LexerToken;
public sealed record Equal : LexerToken;
public sealed record DoubleEqual : LexerToken;
public sealed record NotEqual : LexerToken;
