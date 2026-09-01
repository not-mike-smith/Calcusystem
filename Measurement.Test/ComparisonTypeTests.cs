using Calcusystem.Measurement.Enums;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test;

/// <summary>
/// <see cref="ComparisonType"/> is a mask over <see cref="ComparisonResult"/>'s bits. Nothing in the type system
/// says so, and everything above depends on it, so it is asserted here.
/// </summary>
public class ComparisonTypeTests
{
    private static readonly ComparisonResult[] Determinate =
        [ComparisonResult.LessThan, ComparisonResult.Equal, ComparisonResult.GreaterThan];

    /// <remarks>
    /// The load-bearing correspondence. If a bit ever moved on one enum and not the other, every comparison in
    /// the library would silently start accepting the wrong outcomes — no compiler error, no exception, just
    /// wrong verdicts. This is the only thing standing between that and a release.
    /// </remarks>
    [Theory]
    [InlineData(ComparisonResult.LessThan, ComparisonType.LessThan)]
    [InlineData(ComparisonResult.Equal, ComparisonType.EqualTo)]
    [InlineData(ComparisonResult.GreaterThan, ComparisonType.GreaterThan)]
    [InlineData(ComparisonResult.Incomparable, ComparisonType.None)]
    public void EachResultSharesItsBitWithTheTypeThatNamesIt(ComparisonResult result, ComparisonType type) =>
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

        Determinate.Aggregate(0, (all, r) => all | (byte)r).Should().Be((byte)ComparisonType.Any);
    }

    /// <remarks>
    /// Why <see cref="ComparisonResult.Incomparable"/> is zero rather than a fourth bit: it must be rejected by
    /// every mask including the accept-everything one, because "no answer" is not an answer that any rule
    /// accepts. Callers tell it apart by testing the result, not the mask — see <c>ComparisonRule</c>.
    /// </remarks>
    [Fact]
    public void IncomparableSatisfiesNoMaskAtAllIncludingAny()
    {
        foreach (var type in Enum.GetValues<ComparisonType>())
        {
            ((ComparisonType)ComparisonResult.Incomparable & type).Should().Be(ComparisonType.None);
        }
    }

    /// <remarks>
    /// The composite masks are unions of the primitives rather than separate relations, which is what makes
    /// <c>≤</c> cost nothing to support beyond naming it.
    /// </remarks>
    [Theory]
    [InlineData(ComparisonType.LessThanOrEqualTo, ComparisonResult.LessThan, ComparisonResult.Equal)]
    [InlineData(ComparisonType.GreaterThanOrEqualTo, ComparisonResult.GreaterThan, ComparisonResult.Equal)]
    [InlineData(ComparisonType.InequalTo, ComparisonResult.LessThan, ComparisonResult.GreaterThan)]
    public void ACompositeMaskAcceptsExactlyTheTwoResultsItUnions(
        ComparisonType type, ComparisonResult first, ComparisonResult second)
    {
        var accepted = Determinate.Where(r => (r & (ComparisonResult)type) != 0).ToList();

        accepted.Should().HaveCount(2).And.Contain(first).And.Contain(second);
    }

    /// <remarks>
    /// Negation is complement against <see cref="ComparisonType.Any"/> and needs no case analysis — the reason
    /// the zero-is-empty layout was worth having over one where <c>EqualTo</c> was zero.
    /// </remarks>
    [Theory]
    [InlineData(ComparisonType.LessThan, ComparisonType.GreaterThanOrEqualTo)]
    [InlineData(ComparisonType.EqualTo, ComparisonType.InequalTo)]
    [InlineData(ComparisonType.None, ComparisonType.Any)]
    public void ComplementingAMaskNegatesTheRelationItNames(ComparisonType type, ComparisonType expected) =>
        (ComparisonType.Any & ~type).Should().Be(expected);
}
