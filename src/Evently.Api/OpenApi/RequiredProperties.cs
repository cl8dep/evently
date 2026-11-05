using Microsoft.OpenApi;

namespace Evently.Api;

// Every non-nullable property is always present in a response; say so, so generated clients don't type it as optional.
public static class RequiredProperties
{
    public static void Apply(OpenApiSchema schema)
    {
        if (schema.Properties is null) return;
        schema.Required ??= new HashSet<string>();
        foreach (var (name, prop) in schema.Properties)
            if (!IsNullable(prop)) schema.Required.Add(name);
    }

    static bool IsNullable(IOpenApiSchema s) =>
        s.Type is { } t ? t.HasFlag(JsonSchemaType.Null) : s.OneOf?.Any(o => o.Type is { } ot && ot.HasFlag(JsonSchemaType.Null)) == true;
}
