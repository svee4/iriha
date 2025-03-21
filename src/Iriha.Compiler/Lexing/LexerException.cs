namespace Iriha.Compiler.Lexing;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "")]
public sealed class LexerException : Exception
{
	public int Line { get; }
	public int Column { get; }

	public LexerException(int line, int column, string message) : base(message)
	{
		Line = line;
		Column = column;
	}
}
