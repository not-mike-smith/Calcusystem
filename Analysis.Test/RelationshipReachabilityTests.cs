using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Analysis.Test;

/// <summary>
/// A node can be reachable from a system only through a relationship's operands — a limit compared against but
/// never filed under <c>Variables</c>, or an expression built for the comparison and never added to
/// <c>DerivedExpressions</c>. Nothing stops a modeller writing that, and <c>Flatten</c> already gathers unknowns
/// through <c>Relationships</c>, so <c>Calculate</c> must reach the same nodes or the two analyses disagree about
/// what the same model contains.
/// </summary>
public class RelationshipReachabilityTests
{
    private static Variable Bound(string symbol, double kmsValue) =>
        new(symbol,
            new Quantity(kmsValue, Dimensionality.Length).Measurand(SymmetricUncertainty.FromRelErr(0)),
            symbol);

    [Fact]
    public void ComputesABoundVariableReachableOnlyThroughARelationship()
    {
        var length = Bound("l", 2);
        var limit = Bound("l_max", 3);

        var system = ExpressionSystem.Create("beam", "");
        system.Add(length);
        // `limit` is deliberately not added — it enters the system only as the relationship's right-hand side.
        system.Add(new DefinitelyLessThanOperator { Id = "l<l_max", Lhs = length, Rhs = limit });

        var result = system.Calculate();

        result.ValueOf(limit).Should()
            .NotBeNull("the limit is reachable from the system and every leaf beneath it is bound");
    }

    [Fact]
    public void ComputesADerivedNodeReachableOnlyThroughARelationship()
    {
        var length = Bound("l", 2);

        // A limit the modeller assembled for the comparison and never filed under DerivedExpressions.
        var clearance = Bound("clearance", 1);
        var nominal = Bound("l_nominal", 3);
        var limit = new SumExpression([nominal, clearance]) { Id = "l_max" };

        var system = ExpressionSystem.Create("beam", "");
        system.Add(length);
        system.Add(new DefinitelyLessThanOperator { Id = "l<l_max", Lhs = length, Rhs = limit });

        var result = system.Calculate();

        result.ValueOf(limit).Should().NotBeNull("both addends are bound, so the sum is determined");
        result.ValueOf(limit)!.KmsValue.Should().BeApproximately(4, 1e-9);
    }

    [Fact]
    public void FlattenAndCalculateAgreeOnUnknownsReachableOnlyThroughARelationship()
    {
        var length = Bound("l", 2);
        var limit = new Variable("l_max", Dimensionality.Length, "l_max");   // unbound

        var system = ExpressionSystem.Create("beam", "");
        system.Add(length);
        system.Add(new DefinitelyLessThanOperator { Id = "l<l_max", Lhs = length, Rhs = limit });

        var flat = system.Flatten();
        var result = system.Calculate();

        flat.Unknowns.Should().Contain(limit, "Flatten gathers unknowns through Relationships");
        result.MissingValues.Should()
            .Contain(limit, "Calculate must report the same outstanding value Flatten counts");
    }

    /// <remarks>
    /// <c>Unresolved</c> covers what the system references rather than only what it lists, so that it cannot
    /// disagree with <c>MissingValues</c>. Reporting a value as outstanding while calling the calculation
    /// complete would be a contradiction a reader has no way to resolve.
    /// </remarks>
    [Fact]
    public void AnUnresolvableRelationshipOperandLeavesTheCalculationIncomplete()
    {
        var length = Bound("l", 2);
        var limit = new Variable("l_max", Dimensionality.Length, "l_max");   // unbound

        var system = ExpressionSystem.Create("beam", "");
        system.Add(length);
        system.Add(new DefinitelyLessThanOperator { Id = "l<l_max", Lhs = length, Rhs = limit });

        var result = system.Calculate();

        result.MissingValues.Should().Contain(limit);
        result.Unresolved.Should().Contain(limit);
        result.IsComplete.Should().BeFalse("a check whose bound cannot be evaluated is still outstanding");
    }
}
