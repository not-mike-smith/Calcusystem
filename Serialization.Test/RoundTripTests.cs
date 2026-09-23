using System.Linq;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Serialization.Mappers;
using FluentAssertions;

namespace Calcusystem.Serialization.Test;

public class RoundTripTests
{
    private static readonly Dimensionality Acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Variable Valued(string symbol, Dimensionality dim, double kmsValue, double relativeUncertainty) =>
        new(symbol, new Quantity(kmsValue, dim).Measurand(SymmetricUncertainty.FromRelative(relativeUncertainty)), symbol);

    /// <summary>Round-trips a whole system through both mappers and returns the rebuilt system.</summary>
    private static ExpressionSystem RoundTrip(ExpressionSystem system)
    {
        var dto = new SerializingMapper().Map(system);
        var mapper = new DeserializingMapper(new DeserializationContext());
        return mapper.Map(dto);
    }

    private static IExpression ById(ExpressionSystem system, string id) =>
        system.GetAllExpressions().Single(e => e.Id == id);

    [Fact]
    public void LeafVariables_RoundTrip()
    {
        var system = ExpressionSystem.Create("leaves", "just direct variables");
        system.Add(Valued("m", Dimensionality.Mass, 2, 0.01));
        system.Add(new Variable("u", Dimensionality.Time, "u")); // intentionally unset

        var restored = RoundTrip(system);

        restored.Id.Should().Be(system.Id);
        restored.Name.Should().Be("leaves");
        restored.Description.Should().Be("just direct variables");

        var m = (Variable)ById(restored, "m");
        m.Symbol.Should().Be("m");
        m.Dimensionality.Should().Be(Dimensionality.Mass);
        m.Value!.KmsValue.Should().BeApproximately(2, 1E-9);
        m.Value!.RelativeUncertainty.Should().BeApproximately(0.01, 1E-9);

        ((Variable)ById(restored, "u")).IsFullyDescribed.Should().BeFalse();
    }

    [Fact]
    public void AbsoluteUncertainty_RoundTrips()
    {
        var system = ExpressionSystem.Create("abs-unc", "absolute-error uncertainty on a zero value");
        // value 0 carrying an absolute error — the case relative-only storage could not represent
        system.Add(new Variable(
            "z",
            new Quantity(0, Dimensionality.Length).Measurand(SymmetricUncertainty.FromAbsolute(new Quantity(0.5, Dimensionality.Length))),
            "z"));

        var restored = (Variable)ById(RoundTrip(system), "z");

        restored.Value!.KmsValue.Should().Be(0);
        restored.Value!.KmsAbsoluteUncertainty.Should().BeApproximately(0.5, 1E-9); // survives round-trip as absolute
        double.IsPositiveInfinity(restored.Value!.RelativeUncertainty).Should().BeTrue();
    }

    [Fact]
    public void DerivedExpressions_RoundTrip()
    {
        var system = ExpressionSystem.Create("derived", "one of each derived shape");

        var m = Valued("m", Dimensionality.Mass, 2, 0.01);
        var m2 = Valued("m2", Dimensionality.Mass, 5, 0.0);
        var a = Valued("a", Acceleration, 3, 0.02);
        system.Add(m);
        system.Add(m2);
        system.Add(a);

        // ListDerivedVariable: Product and Sum
        var force = new ProductExpression([m, a]) { Id = "force" };
        var totalMass = new SumExpression(new IExpression[] { m, m2 }) { Id = "totalMass" };

        // PairDerivedVariable: Quotient
        var quotient = new QuotientExpression { Id = "q", Numerator = force, Denominator = m };

        // SingleDerivedVariable: Reciprocal and Negated
        var reciprocal = new ReciprocalExpression(m, "recip");
        var negated = new NegatedExpression(a, "neg");

        system.Add(force);
        system.Add(totalMass);
        system.Add(quotient);
        system.Add(reciprocal);
        system.Add(negated);

        var restored = RoundTrip(system);

        ById(restored, "force").ComputeIfFullyDescribed()!.KmsValue.Should().BeApproximately(6, 1E-9);      // 2 * 3
        ById(restored, "totalMass").ComputeIfFullyDescribed()!.KmsValue.Should().BeApproximately(7, 1E-9);  // 2 + 5
        ById(restored, "q").ComputeIfFullyDescribed()!.KmsValue.Should().BeApproximately(3, 1E-9);          // 6 / 2
        ById(restored, "recip").ComputeIfFullyDescribed()!.KmsValue.Should().BeApproximately(0.5, 1E-9);    // 1 / 2
        ById(restored, "neg").ComputeIfFullyDescribed()!.KmsValue.Should().BeApproximately(-3, 1E-9);       // -3

        // Shared-reference integrity: the quotient's denominator is the same restored 'm' leaf.
        var restoredM = ById(restored, "m");
        ((QuotientExpression)ById(restored, "q")).Denominator.Should().BeSameAs(restoredM);
        ((ReciprocalExpression)ById(restored, "recip")).Reciprocand.Should().BeSameAs(restoredM);
    }

    [Fact]
    public void Operators_RoundTrip()
    {
        var system = ExpressionSystem.Create("operators", "definitions and constraints");

        var lhs = Valued("x", Dimensionality.Length, 10, 0.01);
        var rhs = Valued("y", Dimensionality.Length, 10, 0.02);
        system.Add(lhs);
        system.Add(rhs);

        var equality = new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
        {
            Id = "eq", Name = "x equals y", Description = "defn", Lhs = lhs, Rhs = rhs
        };
        var tolerance = new WithinBindingToleranceOperator
        {
            Id = "tol", Name = "x within y", Description = "constraint", Lhs = lhs, Rhs = rhs
        };
        // Both go into the one list; what each one does to the problem is carried by the operator, so the
        // views below are asserting that the role survived the trip.
        system.Add(equality);
        system.Add(tolerance);

        var restored = RoundTrip(system);

        var eq = restored.Equations.Single();
        eq.Should().BeOfType<EqualityOperator>();
        eq.Id.Should().Be("eq");
        eq.Name.Should().Be("x equals y");
        eq.Lhs.Id.Should().Be("x");
        eq.Rhs.Id.Should().Be("y");
        // Both sides read 10, so nominal agreement holds — and the reading itself came off the wire rather
        // than from whoever happened to be deserializing.
        eq.Should().BeOfType<EqualityOperator>().Which.Agreement.Should().Be(AgreementRule.Nominal);
        eq.IsSatisfied().Should().BeTrue();

        var tol = restored.Requirements.Single();
        tol.Should().BeOfType<WithinBindingToleranceOperator>();
        tol.Id.Should().Be("tol");
        tol.Lhs.Id.Should().Be("x");
        tol.Rhs.Id.Should().Be("y");
    }

    /// <remarks>
    /// The mapper writes one DTO per node the system lists and does not recurse, so a node nested inside another
    /// used to be referenced by id in its parent's payload without ever being written itself — a dangling
    /// reference that failed on load. Nothing has changed in the mapper: the system now contains the nested node
    /// because adding the outer one absorbs it, and being contained is what gets a node written.
    /// </remarks>
    [Fact]
    public void ANodeNestedInsideAnotherRoundTripsWithoutBeingAddedSeparately()
    {
        var a = Valued("a", Dimensionality.Mass, 1, 0);
        var b = Valued("b", Dimensionality.Mass, 2, 0);
        var c = Valued("c", Dimensionality.Mass, 3, 0);

        var inner = new SumExpression([a, b]) { Id = "inner" };
        var outer = new ProductExpression([inner, c]) { Id = "outer" };

        var system = ExpressionSystem.Create("nested", "only the outer node is added");
        system.Add(outer);

        var restored = RoundTrip(system);

        restored.DerivedExpressions.Select(e => e.Id).Should().BeEquivalentTo("inner", "outer");
        ((ProductExpression)ById(restored, "outer")).Factors.Select(f => f.Id)
            .Should().Equal("inner", "c");
        ((SumExpression)ById(restored, "inner")).Addends.Select(x => x.Id)
            .Should().Equal("a", "b");
    }
}
