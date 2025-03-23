using Iriha.Compiler.Parsing.Nodes;

namespace Iriha.Compiler.Semantic.Binding;


public sealed class IdentHelper(SyntaxTreeBinder analyzer)
{
	private readonly SyntaxTreeBinder _analyzer = analyzer;

	private string CurComp => _analyzer.Compilation.ModuleName;

	public static string FormatArity(string name, int arity) =>
		arity > 0 ? $"{name}`{arity}" : name;

	public static LocalSymbolIdent ForLocalVariable(string localName) =>
		new LocalSymbolIdent(localName);

	public GlobalSymbolIdent ForCurComp(string symbol) =>
		GlobalSymbolIdent.From(symbol, CurComp);

	public static GlobalSymbolIdent ForStruct(StructDeclaration st, string module) =>
		GlobalSymbolIdent.From(FormatArity(st.Name.Value, st.TypeParameters.Count), module);

	public GlobalSymbolIdent ForStructCurComp(StructDeclaration st) =>
		ForStruct(st, CurComp);

	public static GlobalSymbolIdent ForNamedFunc(FunctionDeclaration st, string module) =>
		GlobalSymbolIdent.From(FormatArity(st.Name.Value, st.TypeParameters.Count), module);

	public GlobalSymbolIdent ForNamedFuncCurComp(FunctionDeclaration st) =>
		ForNamedFunc(st, CurComp);

	public GlobalSymbolIdent ForAnonFunc() => throw new NotImplementedException();

	public GlobalSymbolIdent ForTypeRef(TypeRef type) =>
		GlobalSymbolIdent.From(FormatArity(type.Identifier.Value, type.Generics.Count), type.Identifier.Module ?? CurComp);

	public GlobalSymbolIdent FromIdentifier(Identifier ident) =>
		ident.Module is { } mod ? GlobalSymbolIdent.From(ident.Value, mod) : ForCurComp(ident.Value);
}
