using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Enums;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Expressions;

/* Spec A §4.1 rules 2 to 4: who has a name, that it is checked where the node is built, and that it is stored
   normalized. */
public class NodeNameOnExpressionsTests
{
    private sealed class Nodes : INodeResolver
    {
        private readonly Dictionary<string, IIdentified> _byId = [];

        public Nodes With(IIdentified node)
        {
            _byId[node.Id] = node;
            return this;
        }

        public TNode Resolve<TNode>(string id) where TNode : class, IIdentified => (TNode)_byId[id];
    }

    private static Variable M() => new("m", Dimensionality.Mass, "id-m");
    private static Variable Pure() => new("x", Dimensionality.Dimensionless, "id-x");

    [Fact]
    public void AVariableKeepsTheNameItWasBuiltWith()
    {
        M().Name.Should().Be("m");
        ((IExpression)M()).Name.Should().Be("m");
        M().ToString().Should().Be("m");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" m")]
    [InlineData("a​b")]
    public void AVariableRefusesAnInvalidName(string name)
    {
        var unset = () => new Variable(name, Dimensionality.Mass);
        unset.Should().Throw<InvalidNameException>();

        var valued = () => new Variable(name, new Quantity(1, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelative(0)));
        valued.Should().Throw<InvalidNameException>();
    }

    [Fact]
    public void AVariableRefusesToBeBuiltWithNoName()
    {
        var act = () => new Variable(null!, Dimensionality.Mass);
        act.Should().Throw<InvalidNameException>();
    }

    [Fact]
    public void AVariableStoresItsNameNormalized() =>
        new Variable("é", Dimensionality.Mass).Name.Should().Be("é");

    [Fact]
    public void AComputedNodeHasNoNameUnlessGivenOne()
    {
        new ProductExpression([M()]).Name.Should().BeNull();
        new ProductExpression([M()]) { Name = "p" }.Name.Should().Be("p");
    }

    /* Every computed kind, whatever shape its constructor has. */
    [Fact]
    public void EveryComputedKindTakesAName()
    {
        IExpression[] named =
        [
            new ProductExpression([M()]) { Name = "n" },
            new SumExpression([M()]) { Name = "n" },
            new QuotientExpression { Numerator = M(), Denominator = M(), Name = "n" },
            new NegatedExpression(M()) { Name = "n" },
            new ReciprocalExpression(M()) { Name = "n" },
            new SqrtExpression(Pure()) { Name = "n" },
            new ExponentialExpression(Pure()) { Name = "n" },
            new NaturalLogExpression(Pure()) { Name = "n" },
        ];

        named.Should().OnlyContain(node => node.Name == "n");
    }

    [Fact]
    public void AComputedNodeRefusesAnInvalidNameAndStoresAValidOneNormalized()
    {
        var empty = () => new ProductExpression([M()]) { Name = "" };
        empty.Should().Throw<InvalidNameException>();

        var padded = () => new NegatedExpression(M()) { Name = "p " };
        padded.Should().Throw<InvalidNameException>();

        new SumExpression([M()]) { Name = "é" }.Name.Should().Be("é");
    }

    [Fact]
    public void ARelationshipRefusesAnInvalidNameAndStoresAValidOneNormalized()
    {
        var m = M();

        var act = () => new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = m, Name = " " };
        act.Should().Throw<InvalidNameException>();

        new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = m }.Name.Should().BeNull();
        new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = m, Name = "é" }.Name.Should().Be("é");
    }

    /* Each operator kind sets its name in its own initializer, so a kind added without one would compile and
       lose every name it was saved with. */
    [Fact]
    public void EveryKindOfRelationshipKeepsItsNameThroughTheFactory()
    {
        var m = M();
        var nodes = new Nodes().With(m);

        foreach (var type in Enum.GetValues<BinaryOperatorType>())
        {
            var snapshot = new BinaryOperatorSnapshot(
                type,
                "r",
                m.Id,
                m.Id,
                SolvingRole.Requirement,
                AgreementRule.Nominal,
                new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.Nominal),
                "named",
                null,
                null);

            BinaryOperatorFactory.FromSnapshot(snapshot, nodes).Name.Should().Be("named", type.ToString());
        }
    }

    /* The snapshot seam, for every arity. A name left out of a snapshot would be lost by a save. */
    [Fact]
    public void ANameSurvivesASnapshot()
    {
        var m = M();
        var x = Pure();
        var nodes = new Nodes().With(m).With(x);

        Variable.FromSnapshot(m.GetSnapshot()).Name.Should().Be("m");

        var product = new ProductExpression([m]) { Name = "p" };
        ExpressionFactory.FromSnapshot(product.GetSnapshot(), nodes).Name.Should().Be("p");

        var sum = new SumExpression([m]) { Name = "s" };
        ExpressionFactory.FromSnapshot(sum.GetSnapshot(), nodes).Name.Should().Be("s");

        var quotient = new QuotientExpression { Numerator = m, Denominator = m, Name = "q" };
        ExpressionFactory.FromSnapshot(quotient.GetSnapshot(), nodes).Name.Should().Be("q");

        IExpression[] unary =
        [
            new NegatedExpression(m) { Name = "u" },
            new ReciprocalExpression(m) { Name = "u" },
            new SqrtExpression(x) { Name = "u" },
            new ExponentialExpression(x) { Name = "u" },
            new NaturalLogExpression(x) { Name = "u" },
        ];

        foreach (var node in unary)
        {
            var snapshot = node switch
            {
                NegatedExpression n => n.GetSnapshot(),
                ReciprocalExpression n => n.GetSnapshot(),
                SqrtExpression n => n.GetSnapshot(),
                ExponentialExpression n => n.GetSnapshot(),
                NaturalLogExpression n => n.GetSnapshot(),
                _ => throw new InvalidOperationException(),
            };

            ExpressionFactory.FromSnapshot(snapshot, nodes).Name.Should().Be("u", node.GetType().Name);
        }

        var unnamed = new ProductExpression([m]);
        ExpressionFactory.FromSnapshot(unnamed.GetSnapshot(), nodes).Name.Should().BeNull();

        var relationship = new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = m, Name = "check" };
        BinaryOperatorFactory.FromSnapshot(relationship.GetSnapshot(), nodes).Name.Should().Be("check");
    }
}
