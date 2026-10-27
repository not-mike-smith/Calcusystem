using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Dtos = Calcusystem.Serialization.Dtos;

namespace Calcusystem.Api.Http;

/// <summary>Corrects what the OpenAPI generator cannot see about the wire.</summary>
/// <remarks>
/// The front-end's types are generated from that document, so an inaccuracy here becomes an inaccuracy there.
/// </remarks>
public static class WireSchema
{
    /// <summary>
    /// Marks an operand slot nullable. <c>List&lt;string?&gt;</c> reaches the generator as a list of strings —
    /// element nullability does not survive — and a null entry is how an opened but unfilled slot is written.
    /// </summary>
    public static Task Transform(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(Dtos.ExpressionDraft)
            && schema.Properties is { } properties
            && properties.TryGetValue("OperandIds", out var operands)
            && operands is OpenApiSchema { Items: OpenApiSchema slot })
        {
            slot.Type |= JsonSchemaType.Null;
        }

        return Task.CompletedTask;
    }
}
