using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Iriha.Compiler.Infra;

// this is copied and cleaned up
// from https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LogValuesFormatter.cs
// used for formatting a string (string.Format) with the same format string as a logger message

public sealed class LogValuesFormatter
{
	private const string NullValue = "(null)";
	private readonly CompositeFormat _format;

	public LogValuesFormatter(string format)
	{
		ArgumentNullException.ThrowIfNull(format);

		OriginalFormat = format;

		var sb = new StringBuilder();
		var scanIndex = 0;
		var endIndex = format.Length;

		while (scanIndex < endIndex)
		{
			var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
			if (scanIndex == 0 && openBraceIndex == endIndex)
			{
				// No holes found.
				_format = CompositeFormat.Parse(format);
				return;
			}

			var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

			if (closeBraceIndex == endIndex)
			{
				_ = sb.Append(format.AsSpan(scanIndex, endIndex - scanIndex));
				scanIndex = endIndex;
			}
			else
			{
				// Format item syntax : { index[,alignment][ :formatString] }.
				var formatDelimiterIndex = format.AsSpan(openBraceIndex, closeBraceIndex - openBraceIndex).IndexOfAny(',', ':');
				formatDelimiterIndex = formatDelimiterIndex < 0 ? closeBraceIndex : formatDelimiterIndex + openBraceIndex;

				_ = sb.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
				_ = sb.Append(ValueNames.Count);
				ValueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
				_ = sb.Append(format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));

				scanIndex = closeBraceIndex + 1;
			}
		}

		_format = CompositeFormat.Parse(sb.ToString());
	}

	public string OriginalFormat { get; }
	public List<string> ValueNames { get; } = [];

	private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
	{
		// Example: {{prefix{{{Argument}}}suffix}}.
		var braceIndex = endIndex;
		var scanIndex = startIndex;
		var braceOccurrenceCount = 0;

		while (scanIndex < endIndex)
		{
			if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
			{
				if (braceOccurrenceCount % 2 == 0)
				{
					// Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
					braceOccurrenceCount = 0;
					braceIndex = endIndex;
				}
				else
				{
					// An unescaped '{' or '}' found.
					break;
				}
			}
			else if (format[scanIndex] == brace)
			{
				if (brace == '}')
				{
					if (braceOccurrenceCount == 0)
						// For '}' pick the first occurrence.
						braceIndex = scanIndex;
				}
				else
				{
					// For '{' pick the last occurrence.
					braceIndex = scanIndex;
				}

				braceOccurrenceCount++;
			}

			scanIndex++;
		}

		return braceIndex;
	}

	public string Format(object?[]? values)
	{
		var formattedValues = values;

		if (values != null)
		{
			for (var i = 0; i < values.Length; i++)
			{
				var formattedValue = FormatArgument(values[i]);
				// If the formatted value is changed, we allocate and copy items to a new array to avoid mutating the array passed in to this method
				if (!ReferenceEquals(formattedValue, values[i]))
				{
					formattedValues = new object[values.Length];
					Array.Copy(values, formattedValues, i);
					formattedValues[i++] = formattedValue;
					for (; i < values.Length; i++)
					{
						formattedValues[i] = FormatArgument(values[i]);
					}
					break;
				}
			}
		}

		return string.Format(CultureInfo.InvariantCulture, _format, formattedValues ?? Array.Empty<object>());
	}

	// NOTE: This method mutates the items in the array if needed to avoid extra allocations, and should only be used when caller expects this to happen
	public string FormatWithOverwrite(object?[]? values)
	{
		if (values != null)
		{
			for (var i = 0; i < values.Length; i++)
			{
				values[i] = FormatArgument(values[i]);
			}
		}

		return string.Format(CultureInfo.InvariantCulture, _format, values ?? Array.Empty<object>());
	}

	public string Format() => _format.Format;

	public string Format<TArg0>(TArg0 arg0) =>
		!TryFormatArgumentIfNullOrEnumerable(arg0, out var arg0String)
			? string.Format(CultureInfo.InvariantCulture, _format, arg0)
			: string.Format(CultureInfo.InvariantCulture, _format, arg0String);

	public string Format<TArg0, TArg1>(TArg0 arg0, TArg1 arg1) =>
		TryFormatArgumentIfNullOrEnumerable(arg0, out var arg0String) | TryFormatArgumentIfNullOrEnumerable(arg1, out var arg1String)
			? string.Format(CultureInfo.InvariantCulture, _format, arg0String ?? arg0, arg1String ?? arg1)
			: string.Format(CultureInfo.InvariantCulture, _format, arg0, arg1);

	public string Format<TArg0, TArg1, TArg2>(TArg0 arg0, TArg1 arg1, TArg2 arg2) =>
		TryFormatArgumentIfNullOrEnumerable(arg0, out var arg0String) | TryFormatArgumentIfNullOrEnumerable(arg1, out var arg1String) | TryFormatArgumentIfNullOrEnumerable(arg2, out var arg2String)
		? string.Format(CultureInfo.InvariantCulture, _format, arg0String ?? arg0, arg1String ?? arg1, arg2String ?? arg2)
		: string.Format(CultureInfo.InvariantCulture, _format, arg0, arg1, arg2);

	public KeyValuePair<string, object?> GetValue(object?[] values, int index)
	{
		if (index < 0 || index > ValueNames.Count)
#pragma warning disable CA2201 // Do not raise reserved exception types
			throw new IndexOutOfRangeException(nameof(index));

		return ValueNames.Count > index
			? new KeyValuePair<string, object?>(ValueNames[index], values[index])
			: new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
	}

	public IEnumerable<KeyValuePair<string, object?>> GetValues(object[] values)
	{
		var valueArray = new KeyValuePair<string, object?>[values.Length + 1];
		for (var index = 0; index != ValueNames.Count; ++index)
		{
			valueArray[index] = new KeyValuePair<string, object?>(ValueNames[index], values[index]);
		}

		valueArray[^1] = new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
		return valueArray;
	}

	private static object FormatArgument(object? value) => TryFormatArgumentIfNullOrEnumerable(value, out var stringValue) ? stringValue : value!;

	private static bool TryFormatArgumentIfNullOrEnumerable<T>(T? value, [NotNullWhen(true)] out object? stringValue)
	{
		if (value == null)
		{
			stringValue = NullValue;
			return true;
		}

		// if the value implements IEnumerable but isn't itself a string, build a comma separated string.
		if (value is not string and IEnumerable enumerable)
		{
			var sb = new StringBuilder();
			var first = true;
			foreach (var e in enumerable)
			{
				if (!first)
					_ = sb.Append(", ");

				_ = sb.Append(e != null ? e.ToString() : NullValue);
				first = false;
			}
			stringValue = sb.ToString();
			return true;
		}

		stringValue = null;
		return false;
	}
}
