using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Iriha.Compiler.Infra;

public sealed class DumpEverythingPolymorphicJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var jsonTypeInfo = base.GetTypeInfo(type, options);

		var derivedTypes = typeof(DumpEverythingPolymorphicJsonTypeInfoResolver).Assembly.GetTypes()
			.Where(derivedType => derivedType.BaseType == type || derivedType.GetInterface(type.FullName!) is not null)
			.Where(derivedType => !derivedType.IsInterface && !derivedType.IsAbstract)
			.ToList();

		if (derivedTypes.Count > 0)
		{
			jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
			{
				TypeDiscriminatorPropertyName = "$type",
				UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
			};

			foreach (var derivedType in derivedTypes)
			{
				jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(derivedType, derivedType.Name));
			}
		}

		return jsonTypeInfo;
	}
}
