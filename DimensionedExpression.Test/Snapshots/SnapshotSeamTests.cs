using System.Collections.Generic;
using System.Linq;
using System;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Snapshots;

/// <summary>
/// The state seam on its own terms, with no serializer involved — these types hand out their state and rebuild
/// from it given only a way to look up neighbours by id.
/// </summary>
public class SnapshotSeamTests
{
    /// <summary>Minimal stand-in for whatever a persistence layer uses to resolve id references.</summary>
    private sealed class StubResolver : INodeResolver
    {
        private readonly Dictionary<string, IIdentified> _nodes = new();

        public StubResolver With(string id, IIdentified node)
        {
            _nodes[id] = node;
            return this;
        }

        public TNode Resolve<TNode>(string id) where TNode : class, IIdentified =>
            _nodes.TryGetValue(id, out var node)
                ? node as TNode ?? throw new InvalidOperationException($"'{id}' is not a {typeof(TNode).Name}")
                : throw new KeyNotFoundException(id);
    }

    private static Variable Leaf(string id, double value) => new(
        id,
        Dimensionality.Dimensionless.Quantity(value).Measurand(SymmetricUncertainty.FromRelative(0.01)),
        id);

    /// <summary>A leaf with an absolute uncertainty bar, for tests that need the bands to overlap or not.</summary>
    private static Variable Leaf(string id, double value, double absoluteUncertainty) => new(
        id,
        Dimensionality.Dimensionless.Quantity(value)
            .Measurand(SymmetricUncertainty.FromAbsolute(Dimensionality.Dimensionless.Quantity(absoluteUncertainty))),
        id);

    [Fact]
    public void VariableRoundTripsThroughItsOwnState()
    {
        var original = Leaf("m", 2);
        original.Provenance = ProvenanceFactory.Measured("SN-1");

        var restored = Variable.FromSnapshot(original.GetSnapshot());

        restored.Id.Should().Be("m");
        restored.Symbol.Should().Be("m");
        restored.Value!.KmsValue.Should().Be(2);
        restored.Provenance!.Summary().Should().Be(original.Provenance.Summary());
    }

    [Fact]
    public void UnsetVariableKeepsItsDimensionalityWithNoValue()
    {
        var original = new Variable("F", Dimensionality.Mass * Dimensionality.Length, "F");

        var restored = Variable.FromSnapshot(original.GetSnapshot());

        restored.Value.Should().BeNull();
        restored.Dimensionality.Should().Be(original.Dimensionality);
        restored.UnsetVariables().Should().Equal(restored);
    }

    [Fact]
    public void ProductStateNamesItsFactorsByIdAndRebuildsThroughTheResolver()
    {
        var a = Leaf("a", 3);
        var b = Leaf("b", 4);

        var product = new ProductExpression([a, b]) { Id = "p" };
        var state = product.GetSnapshot();
        state.Type.Should().Be(NaryExpressionType.Product);
        state.InnerIds.Should().Equal("a", "b");

        var restored = ProductExpression.FromSnapshot(state, new StubResolver().With("a", a).With("b", b));

        restored.Id.Should().Be("p");
        restored.ComputeIfFullyDescribed()!.KmsValue.Should().BeApproximately(12, 1e-12);
    }

    [Theory]
    [InlineData(UnaryExpressionType.Reciprocal, typeof(ReciprocalExpression))]
    [InlineData(UnaryExpressionType.Negated, typeof(NegatedExpression))]
    [InlineData(UnaryExpressionType.Sqrt, typeof(SqrtExpression))]
    [InlineData(UnaryExpressionType.Exponential, typeof(ExponentialExpression))]
    [InlineData(UnaryExpressionType.NaturalLog, typeof(NaturalLogExpression))]
    public void UnaryFactoryPicksTheTypeNamedByTheKind(UnaryExpressionType kind, Type expected)
    {
        var inner = Leaf("x", 1);

        var rebuilt = ExpressionFactory.FromSnapshot(
            new UnaryExpressionSnapshot(kind, "u", "x"),
            new StubResolver().With("x", inner));

        rebuilt.Should().BeOfType(expected);
        rebuilt.Id.Should().Be("u");
    }

    [Fact]
    public void OperatorStateCarriesKindOperandsAndAnnotations()
    {
        var lhs = Leaf("l", 1);
        var rhs = Leaf("r", 1);

        var op = new WhollyWithinToleranceOperator
        {
            Id = "op",
            Lhs = lhs,
            Rhs = rhs,
            Name = "check",
            Description = "a check",
        };

        var state = op.GetSnapshot();
        state.Type.Should().Be(BinaryOperatorType.WhollyWithinTolerance);
        state.LhsId.Should().Be("l");
        state.RhsId.Should().Be("r");

        var restored = BinaryOperatorFactory.FromSnapshot(
            state, new StubResolver().With("l", lhs).With("r", rhs));

        restored.Should().BeOfType<WhollyWithinToleranceOperator>();
        restored.Name.Should().Be("check");
        restored.Description.Should().Be("a check");
    }

    /// <remarks>
    /// The defect this replaced: equality used to take an injected strategy, so the document said "this is an
    /// equality" and the <i>reader</i> decided what equality meant. Two readers could reach opposite verdicts
    /// from identical bytes. The rule is state now, so the verdict travels with the model.
    /// </remarks>
    [Theory]
    [InlineData(AgreementRule.Nominal, false)]
    [InlineData(AgreementRule.Mutual, true)]
    [InlineData(AgreementRule.Overlapping, true)]
    public void AnEqualityCarriesItsOwnSemanticsAcrossTheSeam(AgreementRule rule, bool expected)
    {
        // 1 ± 1 and 2 ± 1: the reported values differ, but each lies inside the other's band.
        var lhs = Leaf("l", 1, 1);
        var rhs = Leaf("r", 2, 1);

        var restored = BinaryOperatorFactory.FromSnapshot(
            new BinaryOperatorSnapshot(
                BinaryOperatorType.Equality, "eq", "l", "r", SolvingRole.Requirement, rule, null, null, null, null),
            new StubResolver().With("l", lhs).With("r", rhs));

        restored.Should().BeOfType<EqualityOperator>().Which.Agreement.Should().Be(rule);
        restored.IsSatisfied().Should().Be(expected);
    }

    /// <remarks>
    /// A document that says "equality" and nothing more describes no particular relationship. Guessing a
    /// reading would reintroduce exactly the ambiguity the rule was added to remove, so it is refused.
    /// </remarks>
    [Fact]
    public void AnEqualityWithNoAgreementRuleIsRefusedRatherThanGuessed()
    {
        var resolve = new StubResolver().With("l", Leaf("l", 1)).With("r", Leaf("r", 1));

        var act = () => BinaryOperatorFactory.FromSnapshot(
            new BinaryOperatorSnapshot(
                BinaryOperatorType.Equality, "eq", "l", "r", SolvingRole.Requirement, null, null, null, null, null),
            resolve);

        act.Should().Throw<ArgumentException>().WithMessage("*agreement rule*");
    }

    /// <remarks>
    /// The general comparison carries its rule the way equality carries its agreement: as state. A kind alone
    /// cannot say which landmarks it compares, so a document that named only the kind would describe fourteen
    /// different relationships at once.
    /// </remarks>
    [Fact]
    public void ASimpleComparisonCarriesItsRuleAcrossTheSeam()
    {
        var lhs = Leaf("l", 1);
        var rhs = Leaf("r", 1);
        var rule = new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.LowerBound);

        var state = new SimpleComparison(rule) { Id = "c", Lhs = lhs, Rhs = rhs }.GetSnapshot();
        state.Rule.Should().Be(rule);

        var restored = BinaryOperatorFactory.FromSnapshot(
            state, new StubResolver().With("l", lhs).With("r", rhs));

        restored.Should().BeOfType<SimpleComparison>().Which.Rule.Should().Be(rule);
        restored.Symbol.Should().Be("·<⌟");
    }

    [Fact]
    public void ASimpleComparisonWithNoRuleIsRefusedRatherThanGuessed()
    {
        var resolve = new StubResolver().With("l", Leaf("l", 1)).With("r", Leaf("r", 1));

        var act = () => BinaryOperatorFactory.FromSnapshot(
            new BinaryOperatorSnapshot(
                BinaryOperatorType.SimpleComparison, "c", "l", "r", SolvingRole.Requirement,
                null, null, null, null, null),
            resolve);

        act.Should().Throw<ArgumentException>().WithMessage("*no rule*");
    }

    [Fact]
    public void ExpressionSystemResolvesTwoDifferentNodeTypes()
    {
        // The case a single typed resolver could not express: expressions in two lists, operators in the third.
        var a = Leaf("a", 1);
        var b = Leaf("b", 1);
        var sum = new SumExpression(new IExpression[] { a, b }) { Id = "s" };
        var op = new WhollyWithinToleranceOperator { Id = "op", Lhs = a, Rhs = b };

        var original = ExpressionSystem.Create("sys", "two node types");
        original.Add(a);
        original.Add(b);
        original.Add(sum);
        original.Add(op);

        var resolver = new StubResolver().With("a", a).With("b", b).With("s", sum).With("op", op);
        var restored = ExpressionSystem.FromSnapshot(original.GetSnapshot(), resolver);

        restored.Name.Should().Be("sys");
        restored.Variables.Should().HaveCount(2);
        restored.DerivedExpressions.Single().Should().BeSameAs(sum);
        restored.Requirements.Single().Should().BeSameAs(op);
    }
}
