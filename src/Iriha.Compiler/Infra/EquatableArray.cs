using System.Collections;
using System.Runtime.CompilerServices;

namespace Iriha.Compiler.Infra;

/// <summary>
/// Equatable read-only list
/// </summary>
/// <typeparam name="T"></typeparam>
[CollectionBuilder(typeof(EquatableListExtensions), "Create")]
public sealed class EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
	private readonly IReadOnlyList<T> _list;

	public EquatableArray(IEnumerable<T> items) => _list = [.. items];
	public EquatableArray(ReadOnlySpan<T> items) => _list = [.. items];

	public bool Equals(EquatableArray<T>? other) => other is not null && _list.SequenceEqual(other);
	public override bool Equals(object? obj) => Equals(obj as EquatableArray<T>);

	public override int GetHashCode()
	{
		var code = new HashCode();
		foreach (var value in _list)
		{
			code.Add(value);
		}

		return code.ToHashCode();
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
		Justification = "THERE IS, AS AN EXTENSION METHOD")]
	public static implicit operator EquatableArray<T>(List<T> source) => [.. source];

	public override string ToString() => string.Join(", ", _list.Select(v => v?.ToString()));

	// irolist

	public int Count => _list.Count;
	public T this[int index] => _list[index];

	public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();
}

public static class EquatableListExtensions
{
	public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source) => [.. source];

	public static EquatableArray<T> Create<T>(ReadOnlySpan<T> items) => new(items);
}
