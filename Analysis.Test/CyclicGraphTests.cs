using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Analysis.Test;

/// <summary>
/// A cycle is unreachable through ordinary construction — a node is given children that already exist — so
/// these tie the knot with a purpose-built node whose operands are mutable afterwards.
/// </summary>
/// <remarks>
/// The real node types cannot be used: their <c>Dimensionality</c> is derived from their children's, so closing
/// a loop out of them overflows the stack while the graph is still being built, before anything could be
/// calculated. That is a construction-time hazard, and a different one from what these tests are about.
/// </remarks>
public class CyclicGraphTests
{
    [Fact]
    public void CalculatingACyclicSystemThrowsRatherThanReportingAMissingValue()
    {
        var (system, a, b) = TwoNodeCycle();

        var act = () => system.Calculate();

        // Left undetected this returned a calculation with both nodes unresolved and *nothing* listed as
        // missing — a contradiction that reads as "a value is absent" and sends the reader hunting for one.
        var thrown = act.Should().Throw<CyclicExpressionGraphException>().Which;
        new[] { thrown.NodeId, thrown.OperandId }.Should().BeEquivalentTo(a.Id, b.Id);
    }

    [Fact]
    public void TheMessageNamesBothEndsOfTheCycle()
    {
        var (system, _, _) = TwoNodeCycle();

        var act = () => system.Calculate();

        act.Should().Throw<CyclicExpressionGraphException>()
            .WithMessage("*'a'*'b'*acyclic*");
    }

    [Fact]
    public void ASingleNodesOwnWalkIsProtectedToo()
    {
        // `ComputeIfFullyDescribed` shares the ordering, so it reports the cycle instead of recursing until
        // the stack dies — a StackOverflowException cannot be caught and would take the process with it.
        var (_, a, _) = TwoNodeCycle();

        var act = () => a.ComputeIfFullyDescribed();

        act.Should().Throw<CyclicExpressionGraphException>();
    }

    [Fact]
    public void ANodeThatIsItsOwnOperandIsACycle()
    {
        var self = new Knot("self");
        self.Operands.Add(self);

        var system = ExpressionSystem.Create("self", "");
        system.Add(self);

        var act = () => system.Calculate();

        var thrown = act.Should().Throw<CyclicExpressionGraphException>().Which;
        thrown.NodeId.Should().Be("self");
        thrown.OperandId.Should().Be("self");
    }

    [Fact]
    public void SharingIsNotMistakenForACycle()
    {
        // The check must distinguish "reachable twice" from "reachable from itself". A diamond is both a
        // legitimate shape and the one a naive visited-set check gets wrong.
        var leaf = new Knot("leaf");
        var left = new Knot("left");
        var right = new Knot("right");
        var top = new Knot("top");
        left.Operands.Add(leaf);
        right.Operands.Add(leaf);
        top.Operands.Add(left);
        top.Operands.Add(right);

        var system = ExpressionSystem.Create("diamond", "");
        system.Add(top);

        var act = () => system.Calculate();

        act.Should().NotThrow();
        system.Calculate().Values.Should().HaveCount(4);
    }

    private static (ExpressionSystem System, Knot A, Knot B) TwoNodeCycle()
    {
        var a = new Knot("a");
        var b = new Knot("b");
        a.Operands.Add(b);
        b.Operands.Add(a);

        var system = ExpressionSystem.Create("cycle", "");
        system.Add(a);
        system.Add(b);
        return (system, a, b);
    }

    /// <summary>
    /// A node whose operands stay mutable, so a test can close a loop the real types cannot. Derives from
    /// <see cref="ExpressionBase"/> like every real node: the walks are declared on <c>IExpression</c> and
    /// implemented once there, so implementing the interface directly would mean reimplementing all of them.
    /// </summary>
    private sealed class Knot(string id) : ExpressionBase(id)
    {
        public List<IExpression> Operands { get; } = [];
        public override bool IsDirectlyMutable => false;
        public override bool IsFullyDescribed => true;
        public override Dimensionality Dimensionality => Dimensionality.Dimensionless;
        public override IEnumerable<IExpression> Children => Operands;

        public override Measurand? ComputeFrom(
            IReadOnlyDictionary<IExpression, Measurand> known,
            IUncertaintyPropagator? propagator = null) =>
            Dimensionality.Dimensionless.Quantity(1).Measurand(SymmetricUncertainty.FromRelative(0));
    }
}
