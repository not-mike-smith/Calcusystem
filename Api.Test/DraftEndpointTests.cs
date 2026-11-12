using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Calcusystem.Api.Reports;
using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;
using Calcusystem.DimensionedExpression.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Dtos = Calcusystem.Serialization.Dtos;

namespace Calcusystem.Api.Test;

/* Over real HTTP, through the real pipeline. Model binding, the JSON options, the status mapping and the
   problem document are the whole of what this assembly adds, and none of them run when a controller is called
   as an object. */
public class DraftEndpointTests(WebApplicationFactory<Program> host) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient Client => host.CreateClient();

    private static Dtos.SystemDraft NewtonsSecondLaw(double? limit = null, double? acceleration = 3)
    {
        var draft = new Dtos.SystemDraft
        {
            Id = "sys-1",
            Name = "Newton's second law",
            Expressions =
            [
                new Dtos.ExpressionDraft
                {
                    Id = "var-m",
                    Type = ExpressionDraftType.Variable,
                    Name = "m",
                    Dimensionality = "M1",
                    KmsValue = 2,
                },
                new Dtos.ExpressionDraft
                {
                    Id = "var-a",
                    Type = ExpressionDraftType.Variable,
                    Name = "a",
                    Dimensionality = "L1,T-2",
                    KmsValue = acceleration,
                },
                new Dtos.ExpressionDraft
                {
                    Id = "expr-f",
                    Type = ExpressionDraftType.Product,
                    OperandIds = ["var-m", "var-a"],
                },
            ],
        };

        if (limit is null) return draft;

        draft.Expressions.Add(new Dtos.ExpressionDraft
        {
            Id = "var-limit",
            Type = ExpressionDraftType.Variable,
            Name = "F-max",
            Dimensionality = "M1,L1,T-2",
            KmsValue = limit,
        });

        draft.Relationships.Add(new Dtos.RelationshipDraft
        {
            Id = "rel-1",
            Type = BinaryOperatorType.DefinitelyLessThan,
            LhsId = "expr-f",
            RhsId = "var-limit",
        });

        return draft;
    }

    private async Task<HttpResponseMessage> Post(string route, Dtos.SystemDraft draft) =>
        await Client.PostAsJsonAsync($"/api/drafts/{route}", draft, Wire.Options);

    private static async Task<TReport> Read<TReport>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<TReport>(Wire.Options))!;

    [Fact]
    public async Task AnEmptyDraftIsAModelNobodyHasStartedRatherThanAnError()
    {
        var response = await Post("validation", new Dtos.SystemDraft());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Read<List<DraftProblem>>(response)).Should().BeEmpty();
    }

    /* Problems are the answer to a validation request, not a refusal of it. A 4xx here would have an editor
       showing a modelling mistake as a transport error. */
    [Fact]
    public async Task ValidationAnswersWithProblemsAndAnOkStatus()
    {
        var draft = NewtonsSecondLaw();
        draft.Expressions.Add(new Dtos.ExpressionDraft { Id = "nothing-yet" });

        var response = await Post("validation", draft);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var problems = await Read<List<DraftProblem>>(response);
        problems.Should().ContainSingle()
            .Which.Should().Match<DraftProblem>(p =>
                p.NodeId == "nothing-yet" && p.Kind == DraftProblemKind.TypeNotChosen);
    }

    [Fact]
    public async Task ABuiltDraftComesBackInTheFormAClientCanSave()
    {
        var response = await Post("build", NewtonsSecondLaw());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await Read<Dtos.ExpressionSystem>(response);
        saved.Id.Should().Be("sys-1");
        saved.Variables.Select(v => v.Id).Should().BeEquivalentTo(["var-m", "var-a"]);
        saved.ListDerivedVariables.Should().ContainSingle().Which.Id.Should().Be("expr-f");
    }

    /* The reason `Failure<TDetail>` exists. An editor has to mark up every node at fault, so one message
       naming the first would send it back to guessing. */
    [Fact]
    public async Task ADraftThatCannotBuildIsUnprocessableAndCarriesEveryProblem()
    {
        var draft = NewtonsSecondLaw();
        draft.Expressions.Add(new Dtos.ExpressionDraft { Id = "untyped-1" });
        draft.Expressions.Add(new Dtos.ExpressionDraft { Id = "untyped-2" });

        var response = await Post("build", draft);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("reason").GetString().Should().Be("Unprocessable");
        root.GetProperty("problems").EnumerateArray().Select(p => p.GetProperty("NodeId").GetString())
            .Should().BeEquivalentTo(["untyped-1", "untyped-2"]);
    }

    /* A malformed dimensionality is a different kind of wrong from an unfinished model: the payload cannot be
       held as a draft at all, so there is no node to blame and nothing for an editor to mark. */
    [Fact]
    public async Task AnUnreadablePayloadIsABadRequestRatherThanAnUnprocessableDraft()
    {
        var draft = NewtonsSecondLaw();
        draft.Expressions[0] = new Dtos.ExpressionDraft
        {
            Id = "var-m",
            Type = ExpressionDraftType.Variable,
            Dimensionality = "Z9",
        };

        var response = await Post("build", draft);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Z9");
    }

    [Fact]
    public async Task ACalculationCarriesTheComputedValueAndItsUncertainty()
    {
        var response = await Post("calculation", NewtonsSecondLaw());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await Read<CalculationReport>(response);
        report.IsComplete.Should().BeTrue();
        report.Values.Should().Contain(v => v.Id == "expr-f" && v.Value.KmsValue == 6);
        report.Values.Single(v => v.Id == "expr-f").Value.Dimensionality.Should().Be("M1,L1,T-2");
    }

    [Fact]
    public async Task ACalculationNamesTheVariableThatStopsItResolving()
    {
        var report = await Read<CalculationReport>(
            await Post("calculation", NewtonsSecondLaw(acceleration: null)));

        report.IsComplete.Should().BeFalse();
        report.MissingValueIds.Should().Equal(["var-a"]);
        report.UnresolvedIds.Should().Contain("expr-f");
    }

    [Fact]
    public async Task ACalculationReportsAViolatedRequirementAsOneRatherThanAnError()
    {
        var report = await Read<CalculationReport>(await Post("calculation", NewtonsSecondLaw(limit: 5)));

        report.IsComplete.Should().BeTrue();
        report.AllRelationshipsHold.Should().BeFalse();
        var outcome = report.Outcomes.Should().ContainSingle().Subject;
        outcome.RelationshipId.Should().Be("rel-1");
        outcome.IsSatisfied.Should().BeFalse();
        outcome.IsViolation.Should().BeTrue();
        outcome.Lhs!.KmsValue.Should().Be(6);
        outcome.Rhs!.KmsValue.Should().Be(5);
    }

    [Fact]
    public async Task ADecompositionReportsTheCountAndWhichUnknownsAreDeterminable()
    {
        var report = await Read<DecompositionReport>(
            await Post("decomposition", NewtonsSecondLaw(acceleration: null)));

        report.DegreesOfFreedom.Should().Be(1);
        report.IsWellPosed.Should().BeFalse();
        report.Underdetermined.Unknowns.Should().ContainSingle()
            .Which.Should().Match<UnknownReport>(u => u.Id == "var-a" && u.Name == "a");
        report.UnknownsWithNoEquationIds.Should().Equal(["var-a"]);
    }

    /* The front-end populates its menus from here rather than holding its own copy of the enums. */
    [Fact]
    public async Task TheVocabularyNamesEveryKindAKindCouldBe()
    {
        var response = await Client.GetAsync("/api/vocabulary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var vocabulary = await Read<VocabularyReport>(response);
        vocabulary.ExpressionKinds.Should().BeEquivalentTo(Enum.GetNames<ExpressionDraftType>());
        vocabulary.RelationshipKinds.Should().BeEquivalentTo(Enum.GetNames<BinaryOperatorType>());
        vocabulary.Dimensions.Should().Contain(d => d.Symbol == "M" && d.Name == "Mass");
    }

    /* A misspelled kind is a client defect, not an authoring state — the editor offers a fixed list. Model
       binding refuses it before anything here runs, which is the behavior wanted. */
    [Fact]
    public async Task AKindThatNamesNoKnownTypeIsRefused()
    {
        var payload = new StringContent(
            """{"Expressions":[{"Id":"var-1","Type":"Prodcut"}]}""", Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/drafts/validation", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /* Field names go over the wire as they are written, so a client can save what it was served and the file
       matches what the serializer writes. A camelCase policy would give one DTO two spellings. */
    [Fact]
    public async Task FieldNamesAreServedAsTheyAreWritten()
    {
        var response = await Post("build", NewtonsSecondLaw());

        (await response.Content.ReadAsStringAsync()).Should().Contain("\"Variables\"").And.NotContain("\"variables\"");
    }

    /* Spec A slice 1, over the wire: a name problem reaches an editor as a kind it can branch on, by name. */
    [Fact]
    public async Task NameProblemsAreReportedByKind()
    {
        var draft = NewtonsSecondLaw();
        draft.Expressions.Add(new Dtos.ExpressionDraft
        {
            Id = "var-m2", Type = ExpressionDraftType.Variable, Name = "m", Dimensionality = "M1",
        });
        draft.Expressions.Add(new Dtos.ExpressionDraft
        {
            Id = "var-unnamed", Type = ExpressionDraftType.Variable, Dimensionality = "M1",
        });
        draft.Expressions.Add(new Dtos.ExpressionDraft
        {
            Id = "var-padded", Type = ExpressionDraftType.Variable, Name = " padded", Dimensionality = "M1",
        });

        var response = await Post("validation", draft);
        var body = await response.Content.ReadAsStringAsync();
        var problems = await Read<List<DraftProblem>>(response);

        problems.Select(p => (p.NodeId, p.Kind)).Should().BeEquivalentTo(
        [
            ("var-m", DraftProblemKind.DuplicateName),
            ("var-m2", DraftProblemKind.DuplicateName),
            ("var-unnamed", DraftProblemKind.NameMissing),
            ("var-padded", DraftProblemKind.NameInvalid),
        ]);
        body.Should().Contain("\"DuplicateName\"");
    }

    [Fact]
    public async Task AFormulasNameIsAcceptedAndTheDraftBuilds()
    {
        var draft = NewtonsSecondLaw();
        draft.Expressions.Single(e => e.Id == "expr-f").OperandIds.Should().NotBeEmpty();
        draft.Expressions.Add(new Dtos.ExpressionDraft
        {
            Id = "expr-named", Type = ExpressionDraftType.Negated, Name = "eq-1", OperandIds = ["var-m"],
        });

        (await Post("build", draft)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Spec A slice 2, over the wire: fixed values ──────────────────────────

    /* F = 2·m·g, with g a constant and 2 a scalar: the draft document carries them as kinds of expression, in
       the fields a variable uses. */
    private static Dtos.SystemDraft WeightTwice(double? gravity = 9.80665) => new()
    {
        Id = "sys-2",
        Name = "Twice the weight",
        Expressions =
        [
            new Dtos.ExpressionDraft
            {
                Id = "var-m", Type = ExpressionDraftType.Variable, Name = "m", Dimensionality = "M1", KmsValue = 2,
            },
            new Dtos.ExpressionDraft
            {
                Id = "const-g", Type = ExpressionDraftType.Constant, Name = "g", Dimensionality = "L1,T-2",
                KmsValue = gravity,
            },
            new Dtos.ExpressionDraft { Id = "scalar-2", Type = ExpressionDraftType.Scalar, KmsValue = 2 },
            new Dtos.ExpressionDraft
            {
                Id = "expr-f", Type = ExpressionDraftType.Product, OperandIds = ["scalar-2", "var-m", "const-g"],
            },
        ],
    };

    [Fact]
    public async Task ABuiltDraftListsItsGivensAndScalarsApartFromItsVariables()
    {
        var response = await Post("build", WeightTwice());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await Read<Dtos.ExpressionSystem>(response);
        saved.Variables.Select(v => v.Id).Should().Equal("var-m");
        saved.Givens.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = "const-g", Type = "Constant", Name = "g", KmsValue = 9.80665 });
        saved.Scalars.Should().ContainSingle().Which.Id.Should().Be("scalar-2");
    }

    [Fact]
    public async Task ACalculationUsesFixedValuesAndNeverCountsThemMissing()
    {
        var response = await Post("calculation", WeightTwice());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await Read<CalculationReport>(response);
        report.IsComplete.Should().BeTrue();
        report.MissingValueIds.Should().BeEmpty();
        report.Values.Single(v => v.Id == "expr-f").Value.KmsValue.Should().BeApproximately(39.2266, 1e-9);
        report.Values.Select(v => v.Id).Should().Contain(["const-g", "scalar-2"]);
    }

    [Fact]
    public async Task AFixedValueIsNeverAnUnknown()
    {
        var response = await Post("decomposition", WeightTwice());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await Read<DecompositionReport>(response);
        report.DegreesOfFreedom.Should().Be(0);
        report.UnknownsWithNoEquationIds.Should().BeEmpty();
    }

    /* A fixed value's problems reach an editor as kinds it can branch on, by name. */
    [Fact]
    public async Task FixedValueProblemsAreReportedByKind()
    {
        var draft = WeightTwice(gravity: null);
        draft.Expressions.Add(new Dtos.ExpressionDraft
        {
            Id = "scalar-kg", Type = ExpressionDraftType.Scalar, Dimensionality = "M1", KmsValue = 3,
        });

        var response = await Post("validation", draft);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"ValueMissing\"").And.Contain("\"FixedValueInvalid\"");
        var problems = JsonSerializer.Deserialize<List<DraftProblem>>(body, Wire.Options)!;
        problems.Select(p => (p.NodeId, p.Kind)).Should().BeEquivalentTo(new[]
        {
            ((string?)"const-g", DraftProblemKind.ValueMissing),
            ((string?)"scalar-kg", DraftProblemKind.FixedValueInvalid),
        });
    }

    [Fact]
    public async Task TheVocabularyOffersTheFixedKinds()
    {
        var vocabulary = await Read<VocabularyReport>(await Client.GetAsync("/api/vocabulary"));

        vocabulary.ExpressionKinds.Should().Contain(["Constant", "ReferenceValue", "Scalar"]);
    }
}
