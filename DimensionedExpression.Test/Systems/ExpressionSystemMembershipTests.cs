using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Systems;

/// <summary>
/// What a system <i>contains</i> and what it <i>reaches</i> are the same set, by construction. Adding anything
/// absorbs the whole subgraph beneath it, so membership can never disagree with reachability — the same reason
/// <c>Definitions</c> and <c>Constraints</c> are views rather than lists.
/// </summary>
public class ExpressionSystemMembershipTests
{
    private static Variable Bound(string symbol, double kmsValue) =>
        new(symbol,
            new Quantity(kmsValue, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0)),
            symbol);

    [Fact]
    public void AddingACompositeAbsorbsTheOperandsBeneathIt()
    {
        var a = Bound("a", 1);
        var b = Bound("b", 2);
        var sum = new SumExpression([a, b]) { Id = "s" };

        var system = ExpressionSystem.Create("composite", "");
        system.Add(sum);

        system.Variables.Select(v => v.Id).Should().BeEquivalentTo("a", "b");
        system.DerivedExpressions.Select(e => e.Id).Should().Equal("s");
    }

    [Fact]
    public void AddingACompositeAbsorbsNodesNestedInsideOtherNodes()
    {
        var a = Bound("a", 1);
        var b = Bound("b", 2);
        var c = Bound("c", 3);
        var inner = new SumExpression([a, b]) { Id = "inner" };
        var outer = new ProductExpression([inner, c]) { Id = "outer" };

        var system = ExpressionSystem.Create("nested", "");
        system.Add(outer);

        system.Variables.Select(v => v.Id).Should().BeEquivalentTo("a", "b", "c");
        system.DerivedExpressions.Select(e => e.Id).Should().BeEquivalentTo("inner", "outer");
    }

    [Fact]
    public void AddingARelationshipAbsorbsBothSides()
    {
        var measured = Bound("measured", 1);
        var limit = Bound("limit", 3);

        var system = ExpressionSystem.Create("check", "");
        system.Add(new DefinitelyLessThanOperator { Id = "lt", Lhs = measured, Rhs = limit });

        system.Variables.Select(v => v.Id).Should().BeEquivalentTo("measured", "limit");
        system.Relationships.Select(r => r.Id).Should().Equal("lt");
    }

    [Fact]
    public void AbsorbingIsIdempotentAndSharedNodesAppearOnce()
    {
        var shared = Bound("shared", 1);
        var other = Bound("other", 2);
        var left = new SumExpression([shared, other]) { Id = "left" };
        var right = new ProductExpression([shared, other]) { Id = "right" };

        var system = ExpressionSystem.Create("shared subgraph", "");
        system.Add(shared);          // declared explicitly as well as reached twice
        system.Add(left);
        system.Add(right);
        system.Add(left);            // adding the same node again changes nothing

        system.Variables.Select(v => v.Id).Should().BeEquivalentTo("shared", "other");
        system.DerivedExpressions.Select(e => e.Id).Should().BeEquivalentTo("left", "right");
    }

    [Fact]
    public void AVariableWithNoOtherReferenceIsStillAMember()
    {
        // Declaring a variable before using it in anything must keep working: absorbing widens membership, it
        // does not make declaration the only path or the reachable set the only source.
        var declared = new Variable("declared", Dimensionality.Mass, "declared");

        var system = ExpressionSystem.Create("declared only", "");
        system.Add(declared);

        system.Variables.Should().Equal(declared);
        system.DerivedExpressions.Should().BeEmpty();
    }
}
