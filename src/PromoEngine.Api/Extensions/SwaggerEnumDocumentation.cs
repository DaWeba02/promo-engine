using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PromoEngine.Api.Extensions;

public sealed class EnumSchemaDescriptionFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
        {
            return;
        }

        var mappings = Enum.GetValues(context.Type)
            .Cast<object>()
            .Select(value => $"{Convert.ToInt32(value)} = {value}");
        var note = $"Request payloads may use the numeric enum values listed here: {string.Join(", ", mappings)}. Responses serialize enum values as strings.";
        schema.Description = string.IsNullOrWhiteSpace(schema.Description)
            ? note
            : $"{schema.Description} {note}";
    }
}

public sealed class RequestEnumOperationFilter : IOperationFilter
{
    private const string Note = "Request enum fields accept integer values; response enum fields are serialized as strings.";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content is null)
        {
            return;
        }

        foreach (var mediaType in operation.RequestBody.Content.Values)
        {
            if (mediaType.Schema is null)
            {
                continue;
            }

            mediaType.Schema.Description = string.IsNullOrWhiteSpace(mediaType.Schema.Description)
                ? Note
                : $"{mediaType.Schema.Description} {Note}";
        }
    }
}
