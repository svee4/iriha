using Iriha.Compiler.Infra;
using Iriha.Compiler.Lexing;
using Iriha.Compiler.Parsing;
using Iriha.Compiler.Semantic;
using Iriha.Compiler.Semantic.Binding;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Iriha.Compiler;

public sealed class Compilation(string moduleName)
{
	public string ModuleName { get; } = moduleName;
	public LogLevel LogLevel { get; } = LogLevel.Debug;

	private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
	{
		TypeInfoResolver = new DumpEverythingPolymorphicJsonTypeInfoResolver(),
		WriteIndented = true,
	};

	public void Compile(string source)
	{
		var logger = new ConsoleLogger<Compilation>(LogLevel);

		var lexed = new Lexer(new ConsoleLogger<Lexer>(LogLevel)).Parse(source);
		foreach (var token in lexed)
		{
			logger.LogDebug("{Token}", token);
		}

		var result = new Parser(this, new ConsoleLogger<Parser>(LogLevel)).Parse(lexed);
		var json = JsonSerializer.Serialize(result, _serializerOptions);

		logger.LogDebug("{Json}", json);
		File.WriteAllText("./ast.txt", json);

		var reconstructed = result.Reconstruct();
		logger.LogDebug("{Reconstructed}", reconstructed);

		var binder = new SyntaxTreeBinder(this, new ConsoleLogger<SyntaxTreeBinder>(LogLevel));
		_ = new ModuleSymbol(Constants.CoreLibModuleName);

		foreach (var (_, coreLibTypeName) in Constants.TypeKeywords)
		{
			binder.SymbolManager.GlobalStructTable.Add(
				new StructSymbol(
					GlobalSymbolIdent.From(coreLibTypeName, Constants.CoreLibModuleName),
					[],
					[],
					[],
					null
				));
		}

		var listSymbol = new StructSymbol(
			GlobalSymbolIdent.CoreLib("List`1"),
			[new TypeParameterSymbol(new LocalSymbolIdent("T"), [])],
			[],
			[],
			null);

		listSymbol = listSymbol with
		{
			Fields =
			[
				new StructFieldSymbol(new MemberSymbolIdent("Count", listSymbol.Ident),
					new StructTypeReferenceSymbol(
						binder.SymbolManager.GlobalStructTable.Get(
							GlobalSymbolIdent.From("Int32", Constants.CoreLibModuleName))))
			],
			Methods =
			[
				new StructMethodSymbol(
					new MemberSymbolIdent("At", listSymbol.Ident),
					new FunctionSymbolBase(
						new TypeParameterTypeReferenceSymbol(new TypeParameterSymbol(new LocalSymbolIdent("T"), [])),
						[new TypeParameterSymbol(new LocalSymbolIdent("T"), [])],
						[
							new FunctionParameterSymbol(
								new LocalSymbolIdent("index"),
								new StructTypeReferenceSymbol(
									binder.SymbolManager.GlobalStructTable.Get(GlobalSymbolIdent.CoreLib("Int32"))),
							VariableModifiers.None)
						],
						[]),
					null),
			]
		};

		binder.SymbolManager.GlobalStructTable.Add(listSymbol);

		binder.Bind(result);

		_ = 1;
	}
}
