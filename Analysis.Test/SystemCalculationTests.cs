using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Traversal;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Analysis.Test;

public class SystemCalculationTests
{
    private static readonly Dimensionality Acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Variable Bound(string symbol, double kmsValue, Dimensionality dim) =>
        new(symbol, new Quantity(kmsValue, dim).Measurand(SymmetricUncertainty.FromRelErr(0)), symbol);

    private static Measurand Value(double kmsValue, Dimensionality dim) =>
        new Quantity(kmsValue, dim).Measurand(SymmetricUncertainty.FromRelErr(0));

    /// <summary>F = m·a, with both leaves supplied.</summary>
    private static ExpressionSystem NewtonsSecondLaw(out Variable m, out Variable a, out ProductExpression f,
        bool massIsBound = true)
    {
        m = massIsBound
            ? Bound("m", 2, Dimensionality.Mass)
            : new Variable("m", Dimensionality.Mass, "m");
        a = Bound("a", 3, Acceleration);

        f = new ProductExpression { Id = "f" };
        f.AddFactor(m);
        f.AddFactor(a);

        var system = ExpressionSystem.Create("F = m·a", "");
        system.DirectExpressions.Add(m);
        system.DirectExpressions.Add(a);
        system.DerivedExpressions.Add(f);
        return system;
    }

    [Fact]
    public void ResolvesDerivedValuesAndReportsCompleteness()
    {
        var system = NewtonsSecondLaw(out _, out _, out var f);

        var result = system.Calculate();

        result.IsComplete.Should().BeTrue();
        result.Unresolved.Should().BeEmpty();
        result.MissingValues.Should().BeEmpty();
        result.ValueOf(f)!.KmsValue.Should().BeApproximately(6, 1e-9);
        result.ValueOf(f)!.Dimensionality.Should().Be(Dimensionality.Mass * Acceleration);
    }

    [Fact]
    public void AgreesWithASingleNodesOwnWalk()
    {
        // The evaluator factors out the walk; it must not change the arithmetic.
        var system = NewtonsSecondLaw(out _, out _, out var f);

        var result = system.Calculate();

        result.ValueOf(f)!.KmsValue.Should().Be(f.CalculateValueIfDetermined()!.KmsValue);
        result.ValueOf(f)!.RelativeError.Should().Be(f.CalculateValueIfDetermined()!.RelativeError);
    }

    [Fact]
    public void ReportsWhatCouldNotResolveAndWhyRatherThanThrowing()
    {
        var system = NewtonsSecondLaw(out var m, out _, out var f, massIsBound: false);

        var result = system.Calculate();

        result.IsComplete.Should().BeFalse();
        result.Unresolved.Select(e => e.Id).Should().BeEquivalentTo("m", "f");
        result.MissingValues.Should().Equal(m);
        result.ValueOf(f).Should().BeNull();

        // The half that could be computed still was.
        result.ValueOf(system.DirectExpressions.Single(v => v.Id == "a"))!.KmsValue
            .Should().BeApproximately(3, 1e-9);
    }

    [Fact]
    public void OverridesSupplyAValueWithoutTouchingTheModel()
    {
        var system = NewtonsSecondLaw(out var m, out _, out var f, massIsBound: false);

        var result = system.Calculate(
            new Dictionary<Variable, Measurand> { [m] = Value(5, Dimensionality.Mass) });

        result.IsComplete.Should().BeTrue();
        result.ValueOf(f)!.KmsValue.Should().BeApproximately(15, 1e-9);
        result.MissingValues.Should().BeEmpty();

        // The model is unchanged, so the next caller sees no trace of the trial value.
        m.Value.Should().BeNull();
        system.Calculate().IsComplete.Should().BeFalse();
    }

    [Fact]
    public void AnOverrideTakesPrecedenceOverAVariablesStoredValue()
    {
        var system = NewtonsSecondLaw(out var m, out _, out var f);

        var result = system.Calculate(
            new Dictionary<Variable, Measurand> { [m] = Value(10, Dimensionality.Mass) });

        result.ValueOf(f)!.KmsValue.Should().BeApproximately(30, 1e-9);
        m.Value!.KmsValue.Should().BeApproximately(2, 1e-9);
    }

    [Fact]
    public void RepeatedTrialValuesLeaveTheModelUntouched()
    {
        // The shape a solver needs: probe the same system at many points, in any order, with no restore step.
        var system = NewtonsSecondLaw(out var m, out _, out var f, massIsBound: false);

        var computed = new[] { 1.0, 2.0, 4.0 }
            .Select(trial => system.Calculate(
                new Dictionary<Variable, Measurand> { [m] = Value(trial, Dimensionality.Mass) }))
            .Select(r => r.ValueOf(f)!.KmsValue)
            .ToList();

        computed.Should().Equal(3, 6, 12);
        m.Value.Should().BeNull();
    }

    [Fact]
    public void TheCalculationCarriesTheOverridesThatProducedIt()
    {
        // A set of values is not reviewable without the assumptions behind it, so the inputs travel with the
        // outputs — and two calculations of one system can then be compared on equal terms.
        var system = NewtonsSecondLaw(out var m, out _, out var f, massIsBound: false);
        var trial = Value(5, Dimensionality.Mass);

        var result = system.Calculate(new Dictionary<Variable, Measurand> { [m] = trial });

        result.Overrides.Should().ContainKey(m).WhoseValue.Should().Be(trial);
        system.Calculate().Overrides.Should().BeEmpty();
    }

    [Fact]
    public void IndependentCalculationsAreSafeToRunInParallel()
    {
        // Why `Calculate` needs no async or internal parallelism: it is a pure function of (system, overrides)
        // and mutates nothing, so a caller wanting many trial points already has the obvious way to get them.
        var system = NewtonsSecondLaw(out var m, out _, out var f, massIsBound: false);
        var trials = Enumerable.Range(1, 500).Select(i => (double)i).ToList();

        var computed = trials
            .AsParallel()
            .AsOrdered()
            .Select(trial => system
                .Calculate(new Dictionary<Variable, Measurand> { [m] = Value(trial, Dimensionality.Mass) })
                .ValueOf(f)!.KmsValue)
            .ToList();

        computed.Should().Equal(trials.Select(t => t * 3));
        m.Value.Should().BeNull();
    }

    [Fact]
    public void ASharedSubexpressionResolvesOnceAndIsReportedOnce()
    {
        // s = a + b, used as both factors of a product. The DAG has 4 distinct nodes, not 5.
        var a = Bound("a", 2, Dimensionality.Mass);
        var b = Bound("b", 3, Dimensionality.Mass);
        var sum = new SumExpression(Dimensionality.Mass) { Id = "s" };
        sum.AddAddend(a);
        sum.AddAddend(b);

        var product = new ProductExpression { Id = "p" };
        product.AddFactor(sum);
        product.AddFactor(sum);

        var system = ExpressionSystem.Create("shared", "");
        system.DirectExpressions.Add(a);
        system.DirectExpressions.Add(b);
        system.DerivedExpressions.Add(product);

        var result = system.Calculate();

        result.ValueOf(sum)!.KmsValue.Should().BeApproximately(5, 1e-9);
        result.ValueOf(product)!.KmsValue.Should().BeApproximately(25, 1e-9);
        // Four distinct nodes, though `sum` is referenced twice — keys are nodes, deduplicated by identity.
        result.Values.Keys.Select(k => k.Id).Should().BeEquivalentTo("a", "b", "s", "p");
    }

    [Fact]
    public void DeeplyNestedExpressionsDoNotExhaustTheStack()
    {
        // Traversal is iterative precisely so depth is a data question, not a crash.
        var leaf = Bound("x", 1, Dimensionality.Mass);

        IExpression nested = leaf;
        for (var i = 0; i < 20_000; i++) nested = new NegatedExpression(nested);

        var system = ExpressionSystem.Create("deep", "");
        system.DirectExpressions.Add(leaf);
        system.DerivedExpressions.Add(nested);

        var act = () => system.Calculate();

        act.Should().NotThrow();
        // An even number of negations returns the original magnitude.
        system.Calculate().ValueOf(nested)!.KmsValue.Should().BeApproximately(1, 1e-9);
    }
}
