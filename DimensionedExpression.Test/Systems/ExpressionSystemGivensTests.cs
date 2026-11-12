using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Systems;

/* Spec A §4.2 and acceptance 4: where a system lists its fixed values. Constants and reference values are in
   Givens; a scalar is in no public list (S2-1); none of them is ever a variable. */
public class ExpressionSystemGivensTests
{
    private static Measurand Exact(double kms) =>
        new Quantity(kms, Dimensionality.Dimensionless).WithoutUncertainty();

    private static Constant NewConstant(string name, double kms) =>
        Constant.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Constant, name, name, Exact(kms).GetSnapshot(), null));

    private static ReferenceValue Reference(string name, double kms) =>
        ReferenceValue.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.ReferenceValue,
            name,
            name,
            new Quantity(kms, Dimensionality.Dimensionless).Measurand(SymmetricUncertainty.FromRelative(0.01)).GetSnapshot(),
            null));

    private static Scalar NewScalar(string id, double kms) =>
        Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar, id, null, Exact(kms).GetSnapshot(), null));

    private sealed class Resolver(params IIdentified[] nodes) : INodeResolver
    {
        public TNode Resolve<TNode>(string id) where TNode : class, IIdentified =>
            nodes.Single(n => n.Id == id) as TNode ?? throw new KeyNotFoundException(id);
    }

    [Fact]
    public void ConstantsAndReferenceValuesAreGivensAndNeverVariables()
    {
        var system = ExpressionSystem.Create("givens");
        var g = NewConstant("g", 9.8);
        var bigG = Reference("G", 6.67e-11);

        system.Add(g);
        system.Add(bigG);

        system.Givens.Should().Equal(g, bigG);
        system.Variables.Should().BeEmpty();
        system.DerivedExpressions.Should().BeEmpty();
    }

    [Fact]
    public void AScalarIsInNoListButIsPartOfTheSystem()
    {
        var system = ExpressionSystem.Create("scalars");
        var two = NewScalar("two", 2);

        system.Add(two);

        system.Givens.Should().BeEmpty();
        system.Variables.Should().BeEmpty();
        system.DerivedExpressions.Should().BeEmpty();
        system.GetAllExpressions().Should().Equal(two);
        system.InDependencyOrder().Should().Equal(two);
    }

    /* Membership is reachability for fixed values as for variables: adding a formula adds what is beneath it. */
    [Fact]
    public void FixedValuesBeneathAFormulaAreAbsorbedWithIt()
    {
        var system = ExpressionSystem.Create("reached");
        var g = NewConstant("g", 9.8);
        var two = NewScalar("two", 2);
        var h = new Variable("h", Dimensionality.Dimensionless);
        var product = new ProductExpression([two, g, h]);

        system.Add(product);

        system.Givens.Should().Equal(g);
        system.Variables.Should().Equal(h);
        system.DerivedExpressions.Should().Equal(product);
        system.GetAllExpressions().Should().BeEquivalentTo(new IExpression[] { two, g, h, product });
    }

    [Fact]
    public void AGivenIsFoundByItsName()
    {
        var system = ExpressionSystem.Create("named");
        var g = NewConstant("g", 9.8);
        system.Add(g);

        system.Find("g").Should().BeSameAs(g);
    }

    /* One namespace: a constant cannot take a variable's name, and the refused Add leaves nothing behind. */
    [Fact]
    public void AGivenSharesTheSystemsOneNamespace()
    {
        var system = ExpressionSystem.Create("names");
        system.Add(new Variable("g", Dimensionality.Dimensionless));

        var act = () => system.Add(NewConstant("g", 9.8));

        act.Should().Throw<DuplicateNameException>();
        system.Givens.Should().BeEmpty();
    }

    [Fact]
    public void TheSnapshotListsGivensAndScalarsSeparately()
    {
        var system = ExpressionSystem.Create("snapshot");
        var g = NewConstant("g", 9.8);
        var bigG = Reference("G", 6.67e-11);
        var two = NewScalar("two", 2);
        var h = new Variable("h", Dimensionality.Dimensionless, "h");
        var product = new ProductExpression([two, g, bigG, h]) { Id = "p" };
        system.Add(product);

        var snapshot = system.GetSnapshot();

        snapshot.GivenIds.Should().BeEquivalentTo("g", "G");
        snapshot.ScalarIds.Should().Equal("two");
        snapshot.VariableIds.Should().Equal("h");
        snapshot.DerivedExpressionIds.Should().Equal("p");

        var restored = ExpressionSystem.FromSnapshot(snapshot, new Resolver(g, bigG, two, h, product));

        restored.Givens.Should().BeEquivalentTo(new IFixedExpression[] { g, bigG });
        restored.GetAllExpressions().Should().Contain(two);
        restored.Variables.Should().Equal(h);
    }

    /* A scalar standing alone, reached by nothing, still survives the snapshot: ScalarIds is what carries it. */
    [Fact]
    public void AScalarNothingRefersToSurvivesTheSnapshot()
    {
        var system = ExpressionSystem.Create("lone scalar");
        var two = NewScalar("two", 2);
        system.Add(two);

        var restored = ExpressionSystem.FromSnapshot(system.GetSnapshot(), new Resolver(two));

        restored.GetAllExpressions().Should().Equal(two);
    }
}
