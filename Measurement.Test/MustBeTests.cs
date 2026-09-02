using Calcusystem.Measurement.Enums;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test;

/// <summary>
/// <see cref="MustBe"/> is a mask over <see cref="ComparisonResult"/>'s bits. Nothing in the type system
/// says so, and everything above depends on it, so it is asserted here.
/// </summary>
public class MustBeTests
{
    private static readonly ComparisonResult[] Determinate =
        [ComparisonResult.LessThan, ComparisonResult.Equal, ComparisonResult.GreaterThan];

    /// <remarks>
    /// The load-bearing correspondence. If a bit ever moved on one enum and not the other, every comparison in
    /// the library would silently start accepting the wrong outcomes — no compiler error, no exception, just
    /// wrong verdicts. This is the only thing standing between that and a release.
    /// </remarks>
    [Theory]
    [InlineData(ComparisonResult.LessThan, MustBe.LessThan)]
    [InlineData(ComparisonResult.Equal, MustBe.EqualTo)]
    [InlineData(ComparisonResult.GreaterThan, MustBe.GreaterThan)]
    [InlineData(ComparisonResult.Incomparable, MustBe.Impossible)]
    public void EachResultSharesItsBitWithTheTypeThatNamesIt(ComparisonResult result, MustBe type) =>
        ((byte)result).Should().Be((byte)type);

    /// <remarks>
    /// Mutually exclusive outcomes, so a mask accepts a result or it does not — there is no partial match to
    /// reason about, and <c>(result &amp; type) != 0</c> is the whole of the evaluation.
    /// </remarks>
    [Fact]
    public void TheThreeDeterminateResultsOccupyDistinctSingleBits()
    {
        foreach (var result in Determinate)
        {
            var bits = (byte)result;
            (bits & (bits - 1)).Should().Be(0, $"{result} should be a single bit");
        }

        Determinate.Aggregate(0, (all, r) => all | (byte)r).Should().Be((byte)MustBe.Comparable);
    }

    /// <remarks>
    /// Why <see cref="ComparisonResult.Incomparable"/> is zero rather than a fourth bit: it must be rejected by
    /// every mask including the accept-everything one, because "no answer" is not an answer that any rule
    /// accepts. Callers tell it apart by testing the result, not the mask — see <c>ComparisonRule</c>.
    /// </remarks>
    [Fact]
    public void IncomparableSatisfiesNoMaskAtAllIncludingAny()
    {
        foreach (var type in Enum.GetValues<MustBe>())
        {
            ((MustBe)ComparisonResult.Incomparable & type).Should().Be(MustBe.Impossible);
        }
    }

    /// <remarks>
    /// The composite masks are unions of the primitives rather than separate relations, which is what makes
    /// <c>≤</c> cost nothing to support beyond naming it.
    /// </remarks>
    [Theory]
    [InlineData(MustBe.LessThanOrEqualTo, ComparisonResult.LessThan, ComparisonResult.Equal)]
    [InlineData(MustBe.GreaterThanOrEqualTo, ComparisonResult.GreaterThan, ComparisonResult.Equal)]
    [InlineData(MustBe.InequalTo, ComparisonResult.LessThan, ComparisonResult.GreaterThan)]
    public void ACompositeMaskAcceptsExactlyTheTwoResultsItUnions(
        MustBe type, ComparisonResult first, ComparisonResult second)
    {
        var accepted = Determinate.Where(r => (r & (ComparisonResult)type) != 0).ToList();

        accepted.Should().HaveCount(2).And.Contain(first).And.Contain(second);
    }

    /// <remarks>
    /// Negation is complement against <see cref="MustBe.Comparable"/> and needs no case analysis — the reason
    /// the zero-is-empty layout was worth having over one where <c>EqualTo</c> was zero.
    /// </remarks>
    [Theory]
    [InlineData(MustBe.LessThan, MustBe.GreaterThanOrEqualTo)]
    [InlineData(MustBe.EqualTo, MustBe.InequalTo)]
    [InlineData(MustBe.Impossible, MustBe.Comparable)]
    public void ComplementingAMaskNegatesTheRelationItNames(MustBe type, MustBe expected) =>
        (MustBe.Comparable & ~type).Should().Be(expected);
}
