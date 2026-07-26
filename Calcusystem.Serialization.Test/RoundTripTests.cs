using System.Linq;
using Calcusystem.Serialization;
using Calcusystem.Serialization.Mappers;
using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.Systems;
using FluentAssertions;
using Measurement;
using Measurement.Models;
using Measurement.Uncertainty;

namespace Calcusystem.Serialization.Test;

public class RoundTripTests
{
    private static readonly Dimensionality Acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Variable Bound(string symbol, Dimensionality dim, double kmsValue, double relativeError) =>
        new(symbol, new Quantity(kmsValue, dim).Measurand(GaussianUncertainty.FromRelErr(relativeError)), symbol);

    /// <summary>Round-trips a whole system through both mappers and returns the rebuilt system.</summary>
    private static ExpressionSystem RoundTrip(ExpressionSystem system)
    {
        var dto = new SerializingMapper().Map(system);
        var mapper = new DeserializingMapper(new DeserializationContext(), new AlwaysEqual());
        return mapper.Map(dto);
    }

    private static IExpression ById(ExpressionSystem system, string id) =>
        system.GetAllExpressions().Single(e => e.Id == id);

    [Fact]
    public void LeafVariables_RoundTrip()
    {
        var system = ExpressionSystem.Create("leaves", "just direct variables");
        system.DirectExpressions.Add(Bound("m", Dimensionality.Mass, 2, 0.01));
        system.DirectExpressions.Add(new Variable("u", Dimensionality.Time, "u")); // intentionally unbound

        var restored = RoundTrip(system);

        restored.Id.Should().Be(system.Id);
        restored.Name.Should().Be("leaves");
        restored.Description.Should().Be("just direct variables");

        var m = (Variable)ById(restored, "m");
        m.Symbol.Should().Be("m");
        m.Dimensionality.Should().Be(Dimensionality.Mass);
        m.Value!.KmsValue.Should().BeApproximately(2, 1E-9);
        m.Value!.RelativeError.Should().BeApproximately(0.01, 1E-9);

        ((Variable)ById(restored, "u")).IsFullyDescribed.Should().BeFalse();
    }

    [Fact]
    public void DerivedExpressions_RoundTrip()
    {
        var system = ExpressionSystem.Create("derived", "one of each derived shape");

        var m = Bound("m", Dimensionality.Mass, 2, 0.01);
        var m2 = Bound("m2", Dimensionality.Mass, 5, 0.0);
        var a = Bound("a", Acceleration, 3, 0.02);
        system.DirectExpressions.Add(m);
        system.DirectExpressions.Add(m2);
        system.DirectExpressions.Add(a);

        // ListDerivedVariable: Product and Sum
        var force = new ProductExpression { Id = "force" };
        force.AddFactor(m);
        force.AddFactor(a);
        var totalMass = new SumExpression(new IExpression[] { m, m2 }) { Id = "totalMass" };

        // PairDerivedVariable: Quotient
        var quotient = new QuotientExpression { Id = "q", Numerator = force, Denominator = m };

        // SingleDerivedVariable: Reciprocal and Negated
        var reciprocal = new ReciprocalExpression(m, "recip");
        var negated = new NegatedExpression(a, "neg");

        system.DerivedExpressions.Add(force);
        system.DerivedExpressions.Add(totalMass);
        system.DerivedExpressions.Add(quotient);
        system.DerivedExpressions.Add(reciprocal);
        system.DerivedExpressions.Add(negated);

        var restored = RoundTrip(system);

        ById(restored, "force").Value!.KmsValue.Should().BeApproximately(6, 1E-9);      // 2 * 3
        ById(restored, "totalMass").Value!.KmsValue.Should().BeApproximately(7, 1E-9);  // 2 + 5
        ById(restored, "q").Value!.KmsValue.Should().BeApproximately(3, 1E-9);          // 6 / 2
        ById(restored, "recip").Value!.KmsValue.Should().BeApproximately(0.5, 1E-9);    // 1 / 2
        ById(restored, "neg").Value!.KmsValue.Should().BeApproximately(-3, 1E-9);       // -3

        // Shared-reference integrity: the quotient's denominator is the same restored 'm' leaf.
        var restoredM = ById(restored, "m");
        ((QuotientExpression)ById(restored, "q")).Denominator.Should().BeSameAs(restoredM);
        ((ReciprocalExpression)ById(restored, "recip")).Reciprocand.Should().BeSameAs(restoredM);
    }

    [Fact]
    public void Operators_RoundTrip()
    {
        var system = ExpressionSystem.Create("operators", "definitions and constraints");

        var lhs = Bound("x", Dimensionality.Length, 10, 0.01);
        var rhs = Bound("y", Dimensionality.Length, 10, 0.02);
        system.DirectExpressions.Add(lhs);
        system.DirectExpressions.Add(rhs);

        var equality = new EqualityOperator(new AlwaysEqual())
        {
            Id = "eq", Name = "x equals y", Description = "defn", Lhs = lhs, Rhs = rhs
        };
        var tolerance = new WithinBindingToleranceOperator
        {
            Id = "tol", Name = "x within y", Description = "constraint", Lhs = lhs, Rhs = rhs
        };
        system.Definitions.Add(equality);
        system.Constraints.Add(tolerance);

        var restored = RoundTrip(system);

        var eq = restored.Definitions.Single();
        eq.Should().BeOfType<EqualityOperator>();
        eq.Id.Should().Be("eq");
        eq.Name.Should().Be("x equals y");
        eq.Lhs.Id.Should().Be("x");
        eq.Rhs.Id.Should().Be("y");
        eq.IsSatisfied().Should().BeTrue(); // AlwaysEqual estimator was injected on deserialize

        var tol = restored.Constraints.Single();
        tol.Should().BeOfType<WithinBindingToleranceOperator>();
        tol.Id.Should().Be("tol");
        tol.Lhs.Id.Should().Be("x");
        tol.Rhs.Id.Should().Be("y");
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
