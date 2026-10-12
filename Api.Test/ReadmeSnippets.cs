using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Calcusystem.Api.Test;

/* The payloads and responses printed in this assembly's README, posted and compared. A JSON block cannot be
   compiled the way a C# snippet can, and prose alone cannot show that a field is spelled the way it is served —
   both of which have gone stale here before anywhere else. */
public class ReadmeSnippets(WebApplicationFactory<Program> host) : IClassFixture<WebApplicationFactory<Program>>
{
    /* Byte for byte what the README's curl posts, shell quoting undone. */
    private const string PostedDraft =
        """
        {
          "Name": "Newton's second law",
          "Expressions": [
            { "Id": "m", "Type": "Variable", "Symbol": "m", "Dimensionality": "M1", "KmsValue": 2 },
            { "Id": "a", "Type": "Variable", "Symbol": "a", "Dimensionality": "L1,T-2", "KmsValue": 3 },
            { "Id": "f", "Type": "Product", "OperandIds": ["m", "a"] }
          ]
        }
        """;

    private const string ServedCalculation =
        """
        {
          "IsComplete": true,
          "AllRelationshipsHold": true,
          "Values": [
            { "Id": "m", "Value": { "KmsValue": 2, "Dimensionality": "M1", "KmsUpperAbsoluteUncertainty": 0, "KmsLowerAbsoluteUncertainty": 0 } },
            { "Id": "a", "Value": { "KmsValue": 3, "Dimensionality": "L1,T-2", "KmsUpperAbsoluteUncertainty": 0, "KmsLowerAbsoluteUncertainty": 0 } },
            { "Id": "f", "Value": { "KmsValue": 6, "Dimensionality": "M1,L1,T-2", "KmsUpperAbsoluteUncertainty": 0, "KmsLowerAbsoluteUncertainty": 0 } }
          ],
          "UnresolvedIds": [],
          "MissingValueIds": [],
          "Outcomes": []
        }
        """;

    private static StringContent AsJson(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task TheReadmesCalculationIsWhatTheServerServes()
    {
        var response = await host.CreateClient()
            .PostAsync("/api/drafts/calculation", AsJson(PostedDraft));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var served = await response.Content.ReadFromJsonAsync<JsonElement>();
        var documented = JsonDocument.Parse(ServedCalculation).RootElement;

        Canonical(served).Should().Be(Canonical(documented));
    }

    [Fact]
    public async Task TheReadmesFailedBuildIsWhatTheServerServes()
    {
        var draft = JsonNode(PostedDraft, """{ "Id": "nothing-yet" }""");

        var response = await host.CreateClient().PostAsync("/api/drafts/build", AsJson(draft));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var served = await response.Content.ReadFromJsonAsync<JsonElement>();
        served.GetProperty("title").GetString().Should().Be("The request was read but could not be acted on.");
        served.GetProperty("status").GetInt32().Should().Be(422);
        served.GetProperty("detail").GetString().Should().Be("One problem stops this draft from building.");
        served.GetProperty("instance").GetString().Should().Be("/api/drafts/build");
        served.GetProperty("reason").GetString().Should().Be("Unprocessable");

        var problem = served.GetProperty("problems").EnumerateArray().Should().ContainSingle().Subject;
        problem.GetProperty("Kind").GetString().Should().Be("TypeNotChosen");
        problem.GetProperty("NodeId").GetString().Should().Be("nothing-yet");
        problem.GetProperty("Message").GetString().Should().Be("No kind of expression has been chosen.");
    }

    /* The README's own example with one more expression node appended. */
    private static string JsonNode(string draft, string node)
    {
        const string product = """{ "Id": "f", "Type": "Product", "OperandIds": ["m", "a"] }""";

        return draft.Replace(product, product + ", " + node);
    }

    /* Compared as text in one canonical form, so neither indentation nor the order of the Values list makes two
       equal documents look different. */
    private static string Canonical(JsonElement element) => JsonSerializer.Serialize(Sorted(element));

    private static object? Sorted(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToDictionary(p => p.Name, p => Sorted(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(Sorted)
            .OrderBy(entry => JsonSerializer.Serialize(entry), StringComparer.Ordinal).ToList(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };
}
