using Iril.TypeSystem;
using System.Text;

namespace Iril;

public sealed class AssemblyWriter
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style",
		"IDE0058:Expression value is never used", Justification = "<Pending>")]
	public static string WriteAssembly(Assembly ass)
	{
		ArgumentNullException.ThrowIfNull(ass);

		var sb = new StringBuilder();

		foreach (var attribute in ass.Attributes)
		{
			sb.AppendLine()
				.Append("attribute ")
				.Append(attribute.Name)
				.Append('(');

			foreach (var parameter in attribute.Parameters)
			{
				sb.Append(parameter).Append(", ");
			}

			sb.Append(')');
		}

		sb.AppendLine();

		foreach (var s in ass.Structs)
		{
			sb.AppendLine()
				.Append("struct ")
				.Append(s.Name)
				.Append(" {");

			foreach (var attr in s.Attributes)
			{
				sb.AppendLine().Indent()
					.Append(".attribute ")
					.Append(attr.Attribute.Name)
					.Append('(');

				foreach (var arg in attr.Arguments)
				{
					sb.Append(arg).Append(", ");
				}

				sb.Append(')');
			}

			foreach (var field in s.Fields)
			{
				sb.AppendLine().Indent()
					.Append(".field ")
					.Append(field.Type)
					.Append(' ')
					.Append(field.Name)
					.Append('{');

				foreach (var attr in field.Attributes)
				{
					sb.AppendLine().Indent().Indent()
						.Append(ApplyAttribute(attr));
				}

				sb.Append('}');
			}
		}

		sb.Append('}').AppendLine();

		foreach (var func in ass.Functions)
		{
			sb.AppendLine()
				.Append("function ")
				.Append(func.Signature.Name)
				.Append('(');

			foreach (var parameter in func.Signature.Parameters)
			{
				sb.Append(parameter.Type)
					.Append(' ')
					.Append(parameter.Name)
					.Append(", ");
			}

			sb.Append(") {");

			foreach (var attr in func.Attributes)
			{
				sb.AppendLine().AppendLine().Indent()
					.Append(".attribute ")
					.Append(ApplyAttribute(attr))
					.AppendLine();
			}

			foreach (var parameter in func.Signature.Parameters)
			{
				sb.AppendLine().AppendLine().Indent()
					.Append(".param ")
					.Append(parameter.Name)
					.Append(" {");

				foreach (var attr in parameter.Attributes)
				{
					sb.AppendLine().Indent().Indent()
						.Append(".attribute ")
						.Append(ApplyAttribute(attr));
				}

				sb.AppendLine().Indent().Append('}');
			}

			sb.AppendLine()
				.AppendLine()
				.Indent().Append(".body {").AppendLine();

			foreach (var inst in func.Body.Instructions)
			{
				sb.Indent().Indent()
					.Append(inst.AsmRepr)
					.AppendLine();
			}

			sb.Indent()
				.Append('}')
				.AppendLine()
				.Append('}');
		}

		return sb.ToString();
	}

	private static string ApplyAttribute(AttributeApplication attr) =>
		$"{attr}({string.Join(", ", attr.Arguments)})";
}

internal static class StringBuilderExtensions
{
	public static StringBuilder Indent(this StringBuilder sb) =>
		sb.Append("  ");

	public static StringBuilder RemoveLast(this StringBuilder sb, int count) =>
		sb.Remove(sb.Length - count, count);
}
