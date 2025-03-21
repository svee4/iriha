using Iriha.Compiler.Infra;

namespace Iriha.Compiler.Parsing.Nodes;

public interface IStatement;

public sealed record FunctionDeclaration(TypeRef ReturnType, Identifier Name, EquatableArray<TypeParameter> TypeParameters,
	EquatableArray<FunctionParameter> Parameters, EquatableArray<IExpressionStatement> Statements) : IStatement, ITopLevelStatement;

public sealed record FunctionParameter(Identifier Name, TypeRef Type);
public sealed record TypeParameter(Identifier Name, EquatableArray<TypeParameterConstraint> Constraints);

public abstract record TypeParameterConstraint;

public sealed record StructDeclaration(Identifier Name, EquatableArray<TypeParameter> TypeParameters,
	EquatableArray<StructFieldDeclaration> Fields) : IStatement, ITopLevelStatement;

public sealed record StructFieldDeclaration(Identifier Name, TypeRef Type);

public sealed record ImplBlockStatement(StructMethodDeclaration Methods) : ITopLevelStatement;

public sealed record StructMethodDeclaration(TypeRef ReturnType, Identifier Name, EquatableArray<TypeParameter> TypeParameters,
	EquatableArray<FunctionParameter> Parameters, EquatableArray<IStatement> Statemets) : IStatement;

public enum VariableKind
{
	Let = 1,
	Mut
}

public sealed record VariableDeclarationStatement(Identifier Name, TypeRef Type, VariableKind Kind, IExpression Initializer)
	: IExpressionStatement;

public sealed record TraitDeclarationStatement(Identifier Name) : ITopLevelStatement;
