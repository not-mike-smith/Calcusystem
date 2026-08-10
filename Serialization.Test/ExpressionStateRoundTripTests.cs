using System.Linq;
using System.Text.Json;
using Calcusystem.Serialization.Mappers;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Serialization.Test;

/// <summary>
/// Round trips for expression types the mappers previously could not handle at all, plus the state the DTOs
/// previously dropped. Every case goes through real JSON, since object-to-object mapping alone cannot show that
/// a payload survives storage.
/// </summary>
public class ExpressionStateRoundTripTests
{
    private static ExpressionSystem RoundTrip(ExpressionSystem system)
    {
        var dto = new SerializingMapper().Map(system);
        var json = JsonSerializer.Serialize(dto);
        var revived = JsonSerializer.Deserialize<Dtos.ExpressionSystem>(json)!;

        return new DeserializingMapper(new DeserializationContext(), new AlwaysEqual()).Map(revived);
    }

    private static ExpressionSystem SystemWith(Variable leaf, IExpression derived, string name)
    {
        var system = ExpressionSystem.Create(name, "");
        system.DirectExpressions.Add(leaf);
        system.DerivedExpressions.Add(derived);
        return system;
    }

    private static Variable Dimensionless(string id, double value) => new(
        id,
        Dimensionality.Dimensionless.Quantity(value).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
        id);

    [Fact]
    public void SqrtExpressionRoundTrips()
    {
        var area = new Variable(
            "a",
            (Dimensionality.Length * Dimensionality.Length).Quantity(9).Measurand(
                SymmetricUncertainty.FromRelErr(0.02)),
            "a");

        var restored = RoundTrip(SystemWith(area, new SqrtExpression(area, "root"), "sqrt"));

        var root = restored.DerivedExpressions.Single();
        root.Should().BeOfType<SqrtExpression>();
        root.Id.Should().Be("root");
        root.Value!.In(Length.Meter).Should().BeApproximately(3, 1e-12);
    }

    [Fact]
    public void ExponentialExpressionRoundTrips()
    {
        var x = Dimensionless("x", 2);

        var restored = RoundTrip(SystemWith(x, new ExponentialExpression(x, "exp"), "exponential"));

        var exp = restored.DerivedExpressions.Single();
        exp.Should().BeOfType<ExponentialExpression>();
        exp.Id.Should().Be("exp");
        exp.Value!.KmsValue.Should().BeApproximately(System.Math.E * System.Math.E, 1e-12);
    }

    [Fact]
    public void NaturalLogExpressionRoundTrips()
    {
        var x = Dimensionless("x", System.Math.E);

        var restored = RoundTrip(SystemWith(x, new NaturalLogExpression(x, "ln"), "naturalLog"));

        var ln = restored.DerivedExpressions.Single();
        ln.Should().BeOfType<NaturalLogExpression>();
        ln.Id.Should().Be("ln");
        ln.Value!.KmsValue.Should().BeApproximately(1, 1e-12);
    }

    [Fact]
    public void QuotientKeepsItsErrorPropagationMethod()
    {
        // PairDerivedVariable carried no ErrorPropagation, so a quotient configured as Correlated came back
        // Uncorrelated with no indication anything had changed.
        var numerator = Dimensionless("n", 6);
        var denominator = Dimensionless("d", 3);

        var system = ExpressionSystem.Create("quotient", "");
        system.DirectExpressions.Add(numerator);
        system.DirectExpressions.Add(denominator);
        system.DerivedExpressions.Add(new QuotientExpression
        {
            Id = "q",
            Numerator = numerator,
            Denominator = denominator,
            ErrorPropagation = ErrorPropagationMethod.Correlated,
        });

        var restored = (QuotientExpression)RoundTrip(system).DerivedExpressions.Single();

        restored.ErrorPropagation.Should().Be(ErrorPropagationMethod.Correlated);
        restored.Value!.KmsValue.Should().BeApproximately(2, 1e-12);
    }

    [Fact]
    public void SharedSubExpressionStaysSharedNotDuplicated()
    {
        // The reason states reference children by id rather than nesting them: a nested representation would
        // write this leaf twice and rebuild two independent copies.
        var shared = Dimensionless("shared", 4);

        var system = ExpressionSystem.Create("sharing", "");
        system.DirectExpressions.Add(shared);

        var product = new ProductExpression { Id = "p" };
        product.AddFactor(shared);
        product.AddFactor(shared);
        system.DerivedExpressions.Add(product);

        var restored = RoundTrip(system);

        var factors = ((ProductExpression)restored.DerivedExpressions.Single()).Factors;
        factors.Should().HaveCount(2);
        factors[0].Should().BeSameAs(factors[1]);
        factors[0].Should().BeSameAs(restored.DirectExpressions.Single());
    }

    [Fact]
    public void DeeplyNestedExpressionsRebuildRegardlessOfPayloadOrder()
    {
        // sqrt(x / y) — the sqrt is written before the quotient it depends on, so the worklist has to defer it.
        var x = Dimensionless("x", 8);
        var y = Dimensionless("y", 2);

        var quotient = new QuotientExpression { Id = "q", Numerator = x, Denominator = y };
        var root = new SqrtExpression(quotient, "root");

        var system = ExpressionSystem.Create("nested", "");
        system.DirectExpressions.Add(x);
        system.DirectExpressions.Add(y);
        system.DerivedExpressions.Add(root);
        system.DerivedExpressions.Add(quotient);

        var restored = RoundTrip(system);

        var restoredRoot = restored.DerivedExpressions.OfType<SqrtExpression>().Single();
        restoredRoot.Value!.KmsValue.Should().BeApproximately(2, 1e-12);
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
