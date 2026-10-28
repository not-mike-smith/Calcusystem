using System.Text.Json;
using System.Text.Json.Serialization;

namespace Calcusystem.Api.Test;

/* What the server itself is configured with: names for enums, no naming policy. A test that read a response
   with System.Text.Json's defaults would pass while the browser got something else. */
public static class Wire
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        Converters = { new JsonStringEnumConverter() },
    };
}
