using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Calcusystem.Api.Test;

/* The front-end's types are generated from this document, so anything it gets wrong about the wire is wrong in
   the client too — and wrong in a form the compiler there will defend rather than question. Each of these was
   false until the thing that fixes it was added. */
public class OpenApiDocumentTests(WebApplicationFactory<Program> host)
    : IClassFixture<WebApplicationFactory<Program>>
{
    /* Served only in development, which is also the only place it is read. */
    private HttpClient Client => host
        .WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development))
        .CreateClient();

    private async Task<JsonElement> Document() =>
        await Client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

    private async Task<JsonElement> Schema(string name) =>
        (await Document()).GetProperty("components").GetProperty("schemas").GetProperty(name);

    /* The OpenAPI document is described from the HTTP JSON options, where a controller answers from the MVC
       ones. Configuring only the MVC set leaves the document describing camelCase while the server serves and
       accepts PascalCase. */
    [Fact]
    public async Task TheDocumentSpellsPropertiesTheWayTheServerServesThem()
    {
        var properties = (await Schema("ExpressionDraft")).GetProperty("properties");

        properties.TryGetProperty("OperandIds", out _).Should().BeTrue();
        properties.TryGetProperty("operandIds", out _).Should().BeFalse();
    }

    [Fact]
    public async Task TheDocumentSaysEnumsTravelAsNames()
    {
        var kind = await Schema("ExpressionDraftType");

        kind.GetProperty("enum").EnumerateArray().Select(e => e.GetString())
            .Should().Contain("Product");
        kind.TryGetProperty("type", out var type).Should().BeFalse(
            "an integer type would have the client send a number for a kind");
    }

    /* Element nullability does not survive the trip from `List<string?>`, and a null entry is the whole
       representation of a slot an author has opened and not filled. */
    [Fact]
    public async Task TheDocumentSaysAnOperandSlotCanBeEmpty()
    {
        var operands = (await Schema("ExpressionDraft")).GetProperty("properties").GetProperty("OperandIds");

        operands.GetProperty("items").GetProperty("type").EnumerateArray().Select(t => t.GetString())
            .Should().Contain("null");
    }

    [Fact]
    public async Task EveryRouteTheClientCallsIsDescribed()
    {
        var paths = (await Document()).GetProperty("paths");

        paths.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo([
            "/api/drafts/validation",
            "/api/drafts/build",
            "/api/drafts/decomposition",
            "/api/drafts/calculation",
            "/api/vocabulary",
        ]);
    }
}
