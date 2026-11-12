using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Systems;

/* Spec A §4.1 rules 5 to 7: names are unique within a system, across expressions and relationships; a refused
   Add changes nothing; and a node is found by its name. */
public class ExpressionSystemNameTests
{
    private static Variable Mass(string name, string? id = null) => new(name, Dimensionality.Mass, id ?? $"id-{name}");

    [Fact]
    public void AnExpressionIsFoundByItsName()
    {
        var m = Mass("m");
        var doubled = new SumExpression([m, m]) { Name = "2m" };
        var system = ExpressionSystem.Create("find");
        system.Add(doubled);

        system.Find("m").Should().BeSameAs(m);
        system.Find("2m").Should().BeSameAs(doubled);
        system.Find("absent").Should().BeNull();
        system.Find("M").Should().BeNull("names are case-sensitive");
    }

    /* The caller's string may be encoded differently from the stored, normalized name. */
    [Fact]
    public void FindingComparesAfterNormalization()
    {
        var system = ExpressionSystem.Create("find");
        var e = Mass("é");
        system.Add(e);

        system.Find("é").Should().BeSameAs(e);
    }

    [Fact]
    public void ARelationshipIsFoundByItsNameAndNotAsAnExpression()
    {
        var m = Mass("m");
        var limit = Mass("limit");
        var check = new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = limit, Name = "under the limit" };
        var system = ExpressionSystem.Create("find");
        system.Add(check);

        system.FindRelationship("under the limit").Should().BeSameAs(check);
        system.FindRelationship("m").Should().BeNull();
        system.Find("under the limit").Should().BeNull();
    }

    [Fact]
    public void AnUnnamedNodeIsNotFoundByAnything()
    {
        var system = ExpressionSystem.Create("find");
        system.Add(new SumExpression([Mass("m")]));

        system.Find("").Should().BeNull();
        system.Find(" ").Should().BeNull();
    }

    [Fact]
    public void ASecondExpressionWithATakenNameIsRefused()
    {
        var system = ExpressionSystem.Create("names");
        system.Add(Mass("m", "first"));

        var act = () => system.Add(Mass("m", "second"));

        act.Should().Throw<DuplicateNameException>().Which.Name.Should().Be("m");
        system.Variables.Should().ContainSingle().Which.Id.Should().Be("first");
    }

    /* The check must not copy the silence a reused id gets: this node is a different one, and dropping it would
       leave a model that computes without the node the author added. */
    [Fact]
    public void TwoNodesInOneAddedSubgraphSharingANameAreRefused()
    {
        var system = ExpressionSystem.Create("names");
        var sum = new SumExpression([Mass("m", "first"), Mass("m", "second")]);

        var act = () => system.Add(sum);

        act.Should().Throw<DuplicateNameException>();
        system.GetAllExpressions().Should().BeEmpty();
    }

    /* Atomic: the subgraph holds a fresh, well-named leaf as well as the clash, and neither may be absorbed. */
    [Fact]
    public void ARefusedAddLeavesTheSystemExactlyAsItWas()
    {
        var system = ExpressionSystem.Create("atomic");
        var m = Mass("m", "first");
        system.Add(m);

        var clash = new SumExpression([Mass("fresh"), Mass("m", "second")]) { Name = "total" };
        var act = () => system.Add(clash);

        act.Should().Throw<DuplicateNameException>();
        system.GetAllExpressions().Should().Equal(m);
        system.Find("fresh").Should().BeNull();
        system.Find("total").Should().BeNull();

        // And the system still accepts the names the refused nodes would have taken.
        system.Add(Mass("fresh", "another"));
        system.Find("fresh")!.Id.Should().Be("another");
    }

    [Fact]
    public void ARefusedRelationshipLeavesItsOperandsOut()
    {
        var system = ExpressionSystem.Create("atomic");
        system.Add(Mass("m", "first"));

        var check = new DefinitelyLessThanOperator
        {
            Id = "r", Lhs = Mass("fresh"), Rhs = Mass("m", "second"), Name = "check",
        };
        var act = () => system.Add(check);

        act.Should().Throw<DuplicateNameException>();
        system.Relationships.Should().BeEmpty();
        system.Variables.Should().ContainSingle();
        system.FindRelationship("check").Should().BeNull();
    }

    /* One namespace: Find is never ambiguous about which kind of node a name belongs to. */
    [Fact]
    public void ARelationshipCannotTakeAnExpressionsNameNorTheReverse()
    {
        var m = Mass("m");
        var limit = Mass("limit");

        var system = ExpressionSystem.Create("one namespace");
        var takesExpressionName = () => system.Add(
            new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = limit, Name = "m" });
        takesExpressionName.Should().Throw<DuplicateNameException>();
        system.Variables.Should().BeEmpty();

        system.Add(new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = limit, Name = "check" });
        var takesRelationshipName = () => system.Add(Mass("check"));
        takesRelationshipName.Should().Throw<DuplicateNameException>();
    }

    [Fact]
    public void TwoRelationshipsCannotShareAName()
    {
        var m = Mass("m");
        var limit = Mass("limit");
        var system = ExpressionSystem.Create("names");
        system.Add(new DefinitelyLessThanOperator { Id = "r1", Lhs = m, Rhs = limit, Name = "check" });

        var act = () => system.Add(
            new DefinitelyGreaterThanOperator { Id = "r2", Lhs = limit, Rhs = m, Name = "check" });

        act.Should().Throw<DuplicateNameException>();
        system.Relationships.Should().ContainSingle();
    }

    /* A different relationship that reuses both the id and the name is still a second claim on the name. */
    [Fact]
    public void ADifferentRelationshipReusingAnIdAndANameIsRefused()
    {
        var m = Mass("m");
        var limit = Mass("limit");
        var system = ExpressionSystem.Create("names");
        var first = new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = limit, Name = "check" };
        system.Add(first);

        var act = () => system.Add(
            new DefinitelyGreaterThanOperator { Id = "r", Lhs = limit, Rhs = m, Name = "check" });

        act.Should().Throw<DuplicateNameException>();
        system.Relationships.Should().ContainSingle();
        system.FindRelationship("check").Should().BeSameAs(first);
    }

    /* A shared sub-expression reaches Add more than once, and that is membership, not a clash. */
    [Fact]
    public void AddingANodeTheSystemAlreadyHoldsIsNotADuplicate()
    {
        var m = Mass("m");
        var system = ExpressionSystem.Create("shared");
        system.Add(m);

        system.Add(m);
        system.Add(new SumExpression([m, m]) { Name = "2m" });
        system.Add(new DefinitelyLessThanOperator { Id = "r", Lhs = m, Rhs = m, Name = "check" });

        system.Variables.Should().ContainSingle();
    }

    [Fact]
    public void UnnamedNodesNeverClash()
    {
        var m = Mass("m");
        var system = ExpressionSystem.Create("unnamed");

        system.Add(new SumExpression([m]));
        system.Add(new SumExpression([m, m]));
        system.Add(new DefinitelyLessThanOperator { Id = "r1", Lhs = m, Rhs = m });
        system.Add(new DefinitelyLessThanOperator { Id = "r2", Lhs = m, Rhs = m });

        system.DerivedExpressions.Should().HaveCount(2);
        system.Relationships.Should().HaveCount(2);
    }

    /* Names differing only in how their characters are encoded are one name. */
    [Fact]
    public void NamesThatNormalizeAlikeClash()
    {
        var system = ExpressionSystem.Create("normalized");
        system.Add(Mass("é", "composed"));

        var act = () => system.Add(Mass("é", "decomposed"));

        act.Should().Throw<DuplicateNameException>();
    }
}
