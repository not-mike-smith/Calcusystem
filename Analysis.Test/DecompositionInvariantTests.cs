using Calcusystem.Analysis.Extensions;
using Calcusystem.Analysis.Outcomes;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Analysis.Test;

/* The properties a decomposition must hold for *every* system, checked over generated ones. DecompositionTests
   pins the cases the design was reasoned about; these pin the two claims the reasoning rests on, which a
   hand-built case can only sample. Both are theorems about the Dulmage–Mendelsohn construction — what is being
   tested is that the implementation has them.

   Seeded rather than random, so a failure is reproducible. */
public class DecompositionInvariantTests
{
    /* Builds a system of `unknownCount` unset variables and `equationCount` equations, each reaching an
       arbitrary subset of them. An equation reaches several by summing them on its left against a known value
       on its right, which is the shape a conservation law takes. */
    private static ExpressionSystem Generate(int unknownCount, int equationCount, int seed)
    {
        var random = new Random(seed);
        var system = ExpressionSystem.Create($"generated {seed}", "");

        var unknowns = Enumerable.Range(0, unknownCount)
            .Select(i => new Variable($"u{i}", Dimensionality.Mass, $"u{i}"))
            .ToList();
        foreach (var u in unknowns) system.Add(u);

        for (var e = 0; e < equationCount; e++)
        {
            var reached = unknowns.Where(_ => random.Next(2) == 0).ToList();

            // An equation over no unknown is a redundancy check, which takes no part in a decomposition. Keep
            // some — their exclusion is exactly what makes the sum invariant exact — but not all of them.
            if (reached.Count == 0 && random.Next(3) != 0)
                reached.Add(unknowns[random.Next(unknowns.Count)]);

            var known = new Variable(
                $"k{e}",
                new Quantity(e + 1, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelative(0.01)),
                $"k{e}");

            IExpression lhs = reached.Count switch
            {
                0 => known,
                1 => reached[0],
                _ => new SumExpression(reached.Cast<IExpression>().ToList()) { Id = $"sum{e}" },
            };

            system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
            {
                Id = $"eq{e}", Lhs = lhs, Rhs = known,
            });
        }

        return system;
    }

    public static TheoryData<int, int, int> Shapes()
    {
        var data = new TheoryData<int, int, int>();
        for (var unknowns = 1; unknowns <= 5; unknowns++)
            for (var equations = 0; equations <= 6; equations++)
                for (var seed = 0; seed < 6; seed++)
                    data.Add(unknowns, equations, seed);
        return data;
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void ThePartsPartitionTheSystem(int unknownCount, int equationCount, int seed)
    {
        var flat = Generate(unknownCount, equationCount, seed).Flatten();
        var decomposition = flat.Decompose();

        SystemBlock[] parts =
            [decomposition.Underdetermined, decomposition.WellDetermined, decomposition.Overdetermined];

        var placedUnknowns = parts.SelectMany(p => p.Unknowns).ToList();
        placedUnknowns.Should().OnlyHaveUniqueItems("the two tails are disjoint");
        placedUnknowns.Should().BeEquivalentTo(flat.Unknowns);

        var placedEquations = parts.SelectMany(p => p.Equations).ToList();
        placedEquations.Should().OnlyHaveUniqueItems();
        placedEquations.Should().BeEquivalentTo(flat.Equations.Except(flat.RedundantEquations));
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void ThePartsLocalCountsSumToTheAggregate(int unknownCount, int equationCount, int seed)
    {
        var flat = Generate(unknownCount, equationCount, seed).Flatten();
        var decomposition = flat.Decompose();

        var summed = decomposition.Underdetermined.DegreesOfFreedom
                   + decomposition.WellDetermined.DegreesOfFreedom
                   + decomposition.Overdetermined.DegreesOfFreedom;

        summed.Should().Be(flat.DegreesOfFreedom);
    }

    /* Each tail leans the way its name says when it arises at all — an absent tail is empty, and an empty part
       counts zero. A part contradicting its own name would mean the reachability sweeps claimed wrong nodes. */
    [Theory]
    [MemberData(nameof(Shapes))]
    public void EachPartLeansTheWayItsNameSays(int unknownCount, int equationCount, int seed)
    {
        var decomposition = Generate(unknownCount, equationCount, seed).Flatten().Decompose();

        if (! decomposition.Underdetermined.IsEmpty)
            decomposition.Underdetermined.DegreesOfFreedom.Should().BeGreaterThan(
                0, "an underdetermined part has unknowns nothing can reach");

        decomposition.WellDetermined.DegreesOfFreedom.Should().Be(0);

        if (! decomposition.Overdetermined.IsEmpty)
            decomposition.Overdetermined.DegreesOfFreedom.Should().BeLessThan(
                0, "an overdetermined part has equations left over");
    }
}
