namespace Iriha.Compiler;

public static class Constants
{
	public static ReadOnlySpan<char> ValidIdentifierInitialChars => "$abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
	public static ReadOnlySpan<char> ValidIdentifierChars => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_123456789";

	public const string CoreLibModuleName = "Core";

	public static IReadOnlyList<(string Keyword, string CoreLibTypeName)> TypeKeywords =>
	[
		("bool", "Boolean"),
		("byte", "UInt8"),
		("sbyte", "Int8"),
		("short", "Int16"),
		("ushort", "UInt16"),
		("int", "Int32"),
		("uint", "UInt32"),
		("long", "Int64"),
		("ulong", "UInt64"),
		("void", "Void"),
		("string", "String")
	];

	public static string LocalSymbolIdentModuleName => "local";
	public static string CompilerInternalModuleName => "_";

	public static IReadOnlyList<string> CompilerReservedModuleNames =>
	[
		LocalSymbolIdentModuleName,
		CompilerInternalModuleName
	];
}
