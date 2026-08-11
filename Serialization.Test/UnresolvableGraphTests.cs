using System.Linq;
using System.Threading.Tasks;
using Calcusystem.Serialization.Exceptions;
using Calcusystem.Serialization.Mappers;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;
using Xunit;

namespace Calcusystem.Serialization.Test;

/// <summary>
/// The deferred-build loop retries an expression whose children are not loaded yet. A payload where some
/// expression can never become buildable used to spin forever — iteratively, so not even a stack overflow would
/// end it. These tests pin the no-progress guard that replaced that hang.
/// </summary>
/// <remarks>
/// Every test here carries a timeout: a regression would otherwise hang the suite rather than fail it.
/// </remarks>
public class UnresolvableGraphTests
{
    private static Dtos.ExpressionSystem Payload() => new()
    {
        Id = "sys",
        Type = "ExpressionSystem",
        Name = "broken",
        Description = "",
    };

    private static Dtos.SingleVariable Leaf(string id) => new()
    {
        Id = id,
        Type = nameof(Variable),
        Symbol = id,
        Dimensionality = "",
        KmsValue = 1,
        Uncertainty = null,
    };

    private static Dtos.SingleDerivedVariable Negated(string id, string innerId) => new()
    {
        Id = id,
        Type = nameof(NegatedExpression),
        InnerId = innerId,
    };

    /// <remarks>
    /// Run on a worker task so the timeout can actually interrupt it — a guard regression would otherwise spin
    /// on the test thread and hang the run rather than failing it.
    /// </remarks>
    private static Task<ExpressionSystem> Deserialize(Dtos.ExpressionSystem dto) =>
        Task.Run(() => new DeserializingMapper(new DeserializationContext(), new AlwaysEqual()).Map(dto));

    [Fact(Timeout = 10000)]
    public async Task DanglingReferenceIsReportedAsMissing()
    {
        var dto = Payload();
        dto.SingleDerivedVariables.Add(Negated("neg", "nowhere"));

        var thrown = (await FluentActions.Awaiting(() => Deserialize(dto))
            .Should().ThrowAsync<UnresolvableGraphException>()).Which;
        thrown.UnbuiltIds.Should().Equal("neg");
        thrown.MissingIds.Should().Equal("nowhere");
        thrown.CyclicIds.Should().BeEmpty();
        thrown.Message.Should().Contain("absent from the payload");
    }

    [Fact(Timeout = 10000)]
    public async Task ReferenceCycleIsReportedAsCyclic()
    {
        // a -> b -> a. Both ids are present, so neither is missing; neither can ever be built.
        var dto = Payload();
        dto.SingleDerivedVariables.Add(Negated("a", "b"));
        dto.SingleDerivedVariables.Add(Negated("b", "a"));

        var thrown = (await FluentActions.Awaiting(() => Deserialize(dto))
            .Should().ThrowAsync<UnresolvableGraphException>()).Which;
        thrown.UnbuiltIds.Should().BeEquivalentTo("a", "b");
        thrown.CyclicIds.Should().BeEquivalentTo("a", "b");
        thrown.MissingIds.Should().BeEmpty();
        thrown.Message.Should().Contain("cycle");
    }

    [Fact(Timeout = 10000)]
    public async Task SelfReferenceIsReportedAsCyclic()
    {
        var dto = Payload();
        dto.SingleDerivedVariables.Add(Negated("loop", "loop"));

        (await FluentActions.Awaiting(() => Deserialize(dto))
            .Should().ThrowAsync<UnresolvableGraphException>())
            .Which.CyclicIds.Should().Equal("loop");
    }

    [Fact(Timeout = 10000)]
    public async Task BuildableExpressionsStillLoadWhenOrderedWorstCase()
    {
        // Guards against an over-eager counter: a chain written in exactly reverse dependency order defers on
        // almost every attempt, but does make progress each pass and must not be rejected.
        var dto = Payload();
        dto.DirectExpressions.Add(Leaf("x"));
        dto.SingleDerivedVariables.Add(Negated("n4", "n3"));
        dto.SingleDerivedVariables.Add(Negated("n3", "n2"));
        dto.SingleDerivedVariables.Add(Negated("n2", "n1"));
        dto.SingleDerivedVariables.Add(Negated("n1", "x"));

        var system = await Deserialize(dto);

        system.DerivedExpressions.Should().HaveCount(4);
        system.GetAllExpressions().Select(e => e.Id).Should().BeEquivalentTo("x", "n1", "n2", "n3", "n4");
    }

    [Fact(Timeout = 10000)]
    public async Task OneUnbuildableEntryDoesNotHideTheRestBeingFine()
    {
        var dto = Payload();
        dto.DirectExpressions.Add(Leaf("x"));
        dto.SingleDerivedVariables.Add(Negated("good", "x"));
        dto.SingleDerivedVariables.Add(Negated("bad", "nowhere"));

        var thrown = (await FluentActions.Awaiting(() => Deserialize(dto))
            .Should().ThrowAsync<UnresolvableGraphException>()).Which;
        thrown.UnbuiltIds.Should().Equal("bad");
        thrown.MissingIds.Should().Equal("nowhere");
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
