using System;
using System.Collections.Generic;
using System.Linq;
using Calcusystem.Core;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.State;

/// <summary>
/// The state seam on its own terms, with no serializer involved — these types hand out their state and rebuild
/// from it given only a way to look up neighbours by id.
/// </summary>
public class StateSeamTests
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
        Dimensionality.Dimensionless.Quantity(value).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
        id);

    [Fact]
    public void VariableRoundTripsThroughItsOwnState()
    {
        var original = Leaf("m", 2);
        original.Provenance = ProvenanceFactory.Measured("SN-1");

        var restored = Variable.FromState(original.GetState());

        restored.Id.Should().Be("m");
        restored.Symbol.Should().Be("m");
        restored.Value!.KmsValue.Should().Be(2);
        restored.Provenance!.Summary().Should().Be(original.Provenance.Summary());
    }

    [Fact]
    public void UnboundVariableKeepsItsDimensionalityWithNoValue()
    {
        var original = new Variable("F", Dimensionality.Mass * Dimensionality.Length, "F");

        var restored = Variable.FromState(original.GetState());

        restored.Value.Should().BeNull();
        restored.Dimensionality.Should().Be(original.Dimensionality);
        restored.FreeVariables().Should().Equal(restored);
    }

    [Fact]
    public void ProductStateNamesItsFactorsByIdAndRebuildsThroughTheResolver()
    {
        var a = Leaf("a", 3);
        var b = Leaf("b", 4);

        var product = new ProductExpression { Id = "p" };
        product.AddFactor(a);
        product.AddFactor(b);

        var state = product.GetState();
        state.Kind.Should().Be(NaryExpressionKind.Product);
        state.InnerIds.Should().Equal("a", "b");

        var restored = ProductExpression.FromState(state, new StubResolver().With("a", a).With("b", b));

        restored.Id.Should().Be("p");
        restored.ComputeIfDetermined()!.KmsValue.Should().BeApproximately(12, 1e-12);
    }

    [Theory]
    [InlineData(UnaryExpressionKind.Reciprocal, typeof(ReciprocalExpression))]
    [InlineData(UnaryExpressionKind.Negated, typeof(NegatedExpression))]
    [InlineData(UnaryExpressionKind.Sqrt, typeof(SqrtExpression))]
    [InlineData(UnaryExpressionKind.Exponential, typeof(ExponentialExpression))]
    [InlineData(UnaryExpressionKind.NaturalLog, typeof(NaturalLogExpression))]
    public void UnaryFactoryPicksTheTypeNamedByTheKind(UnaryExpressionKind kind, Type expected)
    {
        var inner = Leaf("x", 1);

        var rebuilt = ExpressionFactory.FromState(
            new UnaryExpressionState(kind, "u", "x"),
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

        var state = op.GetState();
        state.Kind.Should().Be(BinaryOperatorKind.WhollyWithinTolerance);
        state.LhsId.Should().Be("l");
        state.RhsId.Should().Be("r");

        var restored = BinaryOperatorFactory.FromState(
            state, new StubResolver().With("l", lhs).With("r", rhs), new AlwaysEqual());

        restored.Should().BeOfType<WhollyWithinToleranceOperator>();
        restored.Name.Should().Be("check");
        restored.Description.Should().Be("a check");
    }

    [Fact]
    public void EqualityOperatorReceivesTheInjectedStrategy()
    {
        // The one operator with a dependency, which is why reconstruction is a factory taking the strategy
        // rather than a per-type FromState that has nowhere to accept one.
        var lhs = Leaf("l", 1);
        var rhs = Leaf("r", 2);

        var restored = BinaryOperatorFactory.FromState(
            new BinaryOperatorState(BinaryOperatorKind.Equality, "eq", "l", "r", false, null, null, null),
            new StubResolver().With("l", lhs).With("r", rhs),
            new AlwaysEqual());

        restored.Should().BeOfType<EqualityOperator>();
        restored.IsSatisfied().Should().BeTrue();
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
        original.DirectExpressions.Add(a);
        original.DirectExpressions.Add(b);
        original.DerivedExpressions.Add(sum);
        original.Relationships.Add(op);

        var resolver = new StubResolver().With("a", a).With("b", b).With("s", sum).With("op", op);
        var restored = ExpressionSystem.FromState(original.GetState(), resolver);

        restored.Name.Should().Be("sys");
        restored.DirectExpressions.Should().HaveCount(2);
        restored.DerivedExpressions.Single().Should().BeSameAs(sum);
        restored.Constraints.Single().Should().BeSameAs(op);
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
