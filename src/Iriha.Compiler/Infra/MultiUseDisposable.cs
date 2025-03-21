namespace Iriha.Compiler.Infra;

public readonly struct MultiUseDisposable(Action dispose) : IDisposable
{
	private readonly Action _dispose = dispose;
	public void Dispose() => _dispose();
}
