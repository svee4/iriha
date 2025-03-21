using Iriha.Compiler.Parsing.Nodes;
using System.Globalization;
using System.Text;

namespace Iriha.Compiler.Parsing;

public sealed class SyntaxTree
{
	public List<ITopLevelStatement> TopLevelStatements { get; } = [];

	// everything below this line is TEMPORARY and subject to great refactoring

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Noise")]
	public string Reconstruct()
	{
		var w = new IndentedTextWriter();
		foreach (var statement in TopLevelStatements)
		{
			w.AppendLine();
			w.AppendLine();

			switch (statement)
			{
				case FunctionDeclaration func:
				{
					w.Append("func ");
					w.Append(func.Name.Value);

					w.Append('(');
					foreach (var p in func.Parameters)
					{
						w.Append(p.Name.Value);
						w.Append(": ");
						w.Append(TypeRefToString(p.Type));
						w.Append(", ");
					}
					w.StringBuilder.Remove(w.StringBuilder.Length - 2, 2);

					w.Append(')');
					w.Append(": ");
					w.Append(TypeRefToString(func.ReturnType));
					w.Append(" {");
					w.AppendLine();
					w.AddIndent();

					foreach (var st in func.Statements)
					{
						w.AppendIndentation();
						switch (st)
						{
							case VariableDeclarationStatement varDecl:
							{
								w.Append("var ");
								w.Append(varDecl.Name.Value);
								w.Append(": ");
								w.Append(TypeRefToString(varDecl.Type));
								w.Append(" = ");
								w.Append(ExpressionToString(varDecl.Initializer));
								w.Append(';');
								w.AppendLine();
								break;
							}
							case DiscardExpression discard:
							{
								w.Append("_ = ");
								w.Append(ExpressionToString(discard.Expression));
								w.Append(';');
								break;
							}
							case EmptyStatement:
							{
								w.Append(";");
								break;
							}
							case ReturnExpression ret:
							{
								w.Append("return");
								if (ret.Value is { } v)
								{
									w.Append(" " + ExpressionToString(v));
								}
								w.Append(";");
								break;
							}
							case YieldExpression yld:
							{
								w.Append("return");
								if (yld.Value is { } v)
								{
									w.Append(" " + ExpressionToString(v));
								}
								w.Append(";");
								break;
							}
							case FunctionCallExpression call:
							{
								w.Append(ExpressionToString(call.FunctionExpression));
								w.Append($"({string.Join(", ", call.Arguments.Select(arg => ExpressionToString(arg.Value)))})");
								break;
							}
							case var idk: throw new NotSupportedException(idk.ToString());
						}
					}

					w.RemoveIndent();
					w.AppendLine();
					w.Append('}');

					break;
				}
				case StructDeclaration s:
				{
					w.Append("struct ");
					w.Append(s.Name.Value);
					w.Append(" {");
					w.AppendLine();
					w.AddIndent();

					foreach (var field in s.Fields)
					{
						w.AppendIndentation();
						w.Append(field.Name.Value);
						w.Append(": ");
						w.Append(TypeRefToString(field.Type));
						w.Append(';');
						w.AppendLine();
					}

					w.RemoveIndent();
					w.Append('}');
					break;
				}
				default: throw new NotSupportedException();
			}
		}

		return w.StringBuilder.ToString();

		static string TypeRefToString(TypeRef type)
		{
			var b = $"{new string('&', type.PointerCount)}{type.Identifier.Value}";

			if (type.Generics.Count > 0)
			{
				b += $"<{string.Join(", ", type.Generics.Select(TypeRefToString))}>";
			}

			return b;
		}

		static string ExpressionToString(IExpression expr) => expr switch
		{
			StringLiteralExpression s => $"\"{s.Value}\"",
			IntLiteralExpression i => i.Value.ToString(CultureInfo.InvariantCulture),
			IdentifierExpression ident => ident.Identifier.Value,
			AssignmentExpression ass => $"{ExpressionToString(ass.Target)} = {ExpressionToString(ass.Value)}",
			FunctionCallExpression f => $"{ExpressionToString(f.FunctionExpression)}({string.Join(", ", f.Arguments.Select(arg => ExpressionToString(arg.Value)))})",
			IndexerCallExpression f => $"{ExpressionToString(f.Source)}({ExpressionToString(f.Indexer)})",
			MemberAccessExpression f => $"{ExpressionToString(f.Source)}.{ExpressionToString(f.Member)}",

			VariableDeclarationStatement v => v.Initializer is null ? $"var {v.Name};" : $"var {v.Name} = {ExpressionToString(v.Initializer)}",

			BlockExpression b => $$"""
{
	{{string.Join(";", b.Statements.Select(ExpressionToString))}}
}
""",

			YieldExpression e => e.Value is null ? "yield;" : $"yield {ExpressionToString(e.Value)};",
			ReturnExpression r => r.Value is null ? "return;" : $"return {ExpressionToString(r.Value)};",

			BinaryExpression bin => $"{ExpressionToString(bin.Left)} {GetBinaryExpressionOperator(bin)} {ExpressionToString(bin.Right)}",

			PrefixUnaryExpression => expr switch
			{
				LogicalNegationExpression op => $"!{ExpressionToString(op.Source)}",
				MathematicalNegationExpression op => $"-{ExpressionToString(op.Source)}",
				BitwiseNotExpression op => $"~{ExpressionToString(op.Source)}",
				DereferenceExpression op => $"*{ExpressionToString(op.Source)}",
				_ => throw new NotSupportedException($"Unsupported prefix unary expression {expr}")
			},

			_ => throw new NotSupportedException($"Unsupported expression {expr}")
		};

		static string GetBinaryExpressionOperator(BinaryExpression expr) => expr switch
		{
			AdditionExpression => "+",
			SubtractionExpression => "-",
			MultiplicationExpression => "*",
			DivisionExpression => "/",

			BitwiseAndExpression => "&",
			BitwiseOrExpression => "|",
			BitwiseXorExpression => "^",

			BitwiseLeftShiftExpression => "<<",
			BitwiseRightShiftExpression => ">>",

			LogicalAndExpression => "&&",
			LogicalOrExpression => "||",
			LogicalLessThanExpression => "<",
			LogicalGreaterThanExpression => ">",

			LogicalEqualExpression => "==",
			LogicalNotEqualExpression => "!=",

			_ => throw new NotSupportedException($"Unsupported binary expression {expr}")
		};
	}

	private sealed class IndentedTextWriter
	{
		public StringBuilder StringBuilder { get; } = new();

		public int Indent { get; private set; }

		public void AppendIndentation() => StringBuilder.Append(new string('\t', Indent));

		public IndentedTextWriter Append(char value)
		{
			_ = StringBuilder.Append(value);
			return this;
		}

		public IndentedTextWriter Append(string value)
		{
			_ = StringBuilder.Append(value);
			return this;
		}

		public IndentedTextWriter AppendLine()
		{
			_ = StringBuilder.AppendLine();
			return this;
		}

		public IndentedTextWriter AppendLine(string value)
		{
			_ = StringBuilder.AppendLine(value);
			return this;
		}

		public IndentedTextWriter AddIndent()
		{
			Indent++;
			return this;
		}

		public IndentedTextWriter RemoveIndent()
		{
			Indent--;
			return this;
		}

		public Unindenter WithIndent()
		{
			_ = AddIndent();
			return new Unindenter(this);
		}

		public readonly struct Unindenter(IndentedTextWriter writer) : IDisposable
		{
			public void Dispose() => writer.RemoveIndent();
		}
	}
}
