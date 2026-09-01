using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Dimensions;
using Calcusystem.Measurement.Quantities;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Systems;

/// <summary>
/// What a system <i>contains</i> and what it <i>reaches</i> are the same set, by construction. Adding anything
/// absorbs the whole subgraph beneath it, so membership can never disagree with reachability — the same reason
/// the role-based views are views rather than lists.
/// </summary>
public class ExpressionSystemMembershipTests
{
    private static Variable Bound(string symbol, double kmsValue) =>
        new(symbol,
            new Quantity(kmsValue, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0)),
            symbol);

    /// <remarks>
    /// The authoring gate. Comparing a mass against a length is a modelling mistake, not a verdict — the
    /// relationship is meaningless rather than false — and nothing said so until now, though <c>Quantity</c> has
    /// always refused to add the two. Caught at <c>Add</c> because that is where the model is being written and
    /// where the mistake can still be pointed at.
    /// </remarks>
    [Fact]
    public void ARelationshipAcrossDimensionsIsRefusedWhenItIsAdded()
    {
        var mass = Bound("m", 10);
        var length = new Variable("l", Dimensionality.Length, "l");
        var system = ExpressionSystem.Create("mismatched", "");

        var act = () => system.Add(new DefinitelyLessThanOperator { Id = "bad", Lhs = mass, Rhs = length });

        act.Should().Throw<Measurement.Exceptions.IncompatibleDimensionsException>()
            .WithMessage("*bad*");
        system.Relationships.Should().BeEmpty("a refused relationship must not leave its operands behind");
        system.Variables.Should().BeEmpty();
    }

    /// <remarks>
    /// Dimensionality is known for every expression whether or not it has a value, so the gate works on a model
    /// that has not been given any numbers yet — which is when a modeller most wants to hear about it.
    /// </remarks>
    [Fact]
    public void TheGateHoldsForRelationshipsOverUnknowns()
    {
        var system = ExpressionSystem.Create("unknowns", "");

        var act = () => system.Add(new DefinitelyLessThanOperator
        {
            Id = "bad",
            Lhs = new Variable("m", Dimensionality.Mass, "m"),
            Rhs = new Variable("l", Dimensionality.Length, "l"),
        });

        act.Should().Throw<Measurement.Exceptions.IncompatibleDimensionsException>();
    }

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
