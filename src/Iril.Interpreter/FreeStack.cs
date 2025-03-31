using System.Collections;

namespace Iril.Interpreter;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming",
	"CA1711:Identifiers should not have incorrect suffix", 
	Justification = "????")]
public sealed class FreeStack<T> : ICollection<T>, IReadOnlyCollection<T>
{
	private readonly List<T> _list = [];

	public int Count => _list.Count;

	public int Capacity
	{
		get => _list.Capacity;
		set => _list.Capacity = value;
	}

	public void Push(T value) => _list.Insert(0, value);

	public T Pop()
	{
		var v = _list[0];
		_list.RemoveAt(0);
		return v;
	}

	public T Peek() => _list[0];
	public T Peek(int skip) => _list[skip];

	bool ICollection<T>.IsReadOnly => false;
	void ICollection<T>.Add(T item) => _list.Add(item);
	void ICollection<T>.Clear() => _list.Clear();
	bool ICollection<T>.Contains(T item) => _list.Contains(item);
	void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
	bool ICollection<T>.Remove(T item) => _list.Remove(item);

	public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
}
