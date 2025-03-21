using Microsoft.Extensions.Logging;

namespace Iriha.Compiler.Infra;

public sealed class ConsoleLogger<T>(LogLevel logLevel) : ILogger<T>
{
	private readonly LogLevel _logLevel = logLevel;
	private readonly string _categoryName = typeof(T).FullName ?? throw new InvalidOperationException($"Invalid type name for {typeof(T)}");
	private readonly Queue<object> _scopes = [];

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		this.LogTrace("State push: {State}", state);
		_scopes.Enqueue(state);
		return new ScopeDisposer(_scopes.Count, _scopes);
	}

	public bool IsEnabled(LogLevel logLevel) => logLevel >= _logLevel;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}

		var scopes = _scopes.Count > 0
			? $" - {string.Join(" -> ", _scopes)}"
			: null;

		Console.WriteLine($"[{logLevel}] ({_categoryName}{scopes}) {formatter(state, exception)}");
	}

	private sealed class ScopeDisposer(int count, Queue<object> scopes) : IDisposable
	{
		private readonly int _count = count;
		private readonly Queue<object> _scopes = scopes;

		public void Dispose()
		{
			if (_count != _scopes.Count)
			{
				throw new InvalidOperationException($"Attempt to dispose scope at the wrong level (expected: {_count}, actual: {_scopes.Count})");
			}

			_ = _scopes.Dequeue();
		}
	}
}
