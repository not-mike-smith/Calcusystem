using System.Text.Json;
using System.Text.Json.Serialization;

namespace Calcusystem.Api.Http;

/// <summary>How this service spells JSON, in one place.</summary>
/// <remarks>
/// Applied to both the MVC options and the HTTP options, which are separate: controllers read the first, and
/// the OpenAPI document is described from the second. Configuring only one would have the document describe a
/// wire format the server does not serve — and the front-end's types are generated from that document.
/// </remarks>
public static class WireJson
{
    /// <summary>Applies the wire format to <paramref name="options"/>.</summary>
    public static void Configure(JsonSerializerOptions options)
    {
        // Property names as written, matching what a saved document looks like on disk. A camelCase policy
        // would give the same DTO two spellings, and a client could not save what it was served.
        options.PropertyNamingPolicy = null;

        // Enum members as names. Numbers would renumber an existing payload's meaning the moment a member is
        // inserted, and a name is what a front-end can put in a menu.
        options.Converters.Add(new JsonStringEnumConverter());

        // A draft is mostly unset, and absent already means "not supplied yet".
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }
}
