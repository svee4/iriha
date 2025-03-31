using System.Collections.Immutable;

namespace Iril.TypeSystem;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming",
	"CA1711:Identifiers should not have incorrect suffix", Justification = "<Pending>")]
public sealed class Attribute
{
	public string Assembly { get; }
	public string Name { get; }
	public ImmutableArray<string> Parameters { get; }

	internal Attribute(string assembly, string name, ImmutableArray<string> parameters)
	{
		Assembly = assembly;
		Name = name;
		Parameters = parameters;
	}
}
