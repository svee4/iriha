using Iril.Builder;

namespace Iril.TypeSystem;

public abstract class TypeReference
{
	public override abstract string ToString();
}

public sealed class BuilderTypeReference(StructBuilder type) : TypeReference
{
	public StructBuilder Type { get; } = type;

	public override string ToString() => Type.Name;
}

public sealed class StructTypeReference(Struct @struct) : TypeReference
{
	public Struct Struct { get; } = @struct;

	public override string ToString() => Struct.Name;
}

public sealed class TypeParameterTypeReference(string typeParameter) : TypeReference
{
	public string TypeParameter { get; } = typeParameter;

	public override string ToString() => "!" + TypeParameter;
}
