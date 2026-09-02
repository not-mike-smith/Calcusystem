using Calcusystem.Core.Extensions;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Comparison;

/// <summary>
/// Compares one <see cref="Landmark"/> of a measurand against one landmark of another, three ways plus
/// "incomparable".
/// </summary>
/// <remarks>
/// <para>
/// The single place a numeric comparison happens. Everything above it — the operators, the confidence ladders —
/// selects <i>which</i> landmarks to compare and what to do with the answer; none of them decides what "less
/// than" means.
/// </para>
/// <para>
/// Concrete rather than an injected strategy, deliberately. A strategy cannot be serialized, so a wire format
/// carrying only "this is an equality" leaves the reader to supply the semantics — two readers then get
/// different verdicts from identical bytes. It also has to be well behaved for the confidence ladders to hold
/// together: their implication chains assume the set of values judged equal to a given <c>y</c> is a contiguous
/// interval containing <c>y</c>, which an arbitrary strategy need not satisfy and this does.
/// </para>
/// </remarks>
public static class MeasurandComparer
{
    /// <summary>
    /// How far apart two values may sit, relative to the larger, and still be the same number. Sized for
    /// accumulated floating-point drift down a chain of arithmetic, not for measurement uncertainty — a double
    /// carries ~15-16 significant digits, so this leaves room for a few to be lost on the way.
    /// </summary>
    public const double RelativeDifferenceEpsilon = 1e-12;

    /// <summary>
    /// The fraction of the finest uncertainty bar below which a value is treated as zero. Uncertainty is what supplies
    /// a <i>scale</i> to the question "is this effectively nothing" — a quantity two orders finer than anything
    /// the measurement can resolve is not a small number, it is noise.
    /// </summary>
    public const double FractionOfAbsErrIsZero = 1e-3;

    /// <summary>
    /// Compares <paramref name="lhsLandmark"/> of <paramref name="l"/> against <paramref name="rhsLandmark"/>
    /// of <paramref name="r"/>.
    /// </summary>
    /// <remarks>
    /// A cascade, and the order matters. Dimensional mismatch and indeterminate values are answered before any
    /// arithmetic; then exact equality; then two ways of being equal that a bare <c>==</c> misses — relative
    /// drift, and both operands sitting below the resolution of the measurements. Only what survives all of
    /// that is ordered.
    /// </remarks>
    /// <returns>
    /// <see cref="ComparisonResult.Incomparable"/> when the question has no answer — different dimensions, a
    /// <see cref="double.NaN"/>, or two infinities of the same sign. Never a guess.
    /// </returns>
    public static ComparisonResult Compare(Measurand l, Landmark lhsLandmark, Measurand r, Landmark rhsLandmark)
    {
        // Kilograms and metres are not unequal, they are incomparable. Answering `false` here would let
        // "not equal" read as true and put a confident ordering on quantities that share no scale.
        if (l.Dimensionality != r.Dimensionality) return ComparisonResult.Incomparable;

        var lhs = l[lhsLandmark];
        var rhs = r[rhsLandmark];

        if (! lhs.IsComparisonDetermined(rhs)) return ComparisonResult.Incomparable;

        // Opposite-signed infinities, or one infinity against a bounded value, are ordered. Same-signed pairs
        // were already sent to Incomparable above.
        if (double.IsNegativeInfinity(lhs) || double.IsPositiveInfinity(rhs)) return ComparisonResult.LessThan;
        if (double.IsPositiveInfinity(lhs) || double.IsNegativeInfinity(rhs)) return ComparisonResult.GreaterThan;

        if (lhs == rhs) return ComparisonResult.Equal;

        // Relative, and scaled by the larger magnitude. That denominator is zero only when both values are,
        // which the exact check above has already answered.
        if (Math.Abs(lhs - rhs) / Math.Max(Math.Abs(lhs), Math.Abs(rhs)) < RelativeDifferenceEpsilon)
        {
            return ComparisonResult.Equal;
        }

        if (AreBothIndistinguishableFromZero(l, r, lhs, rhs)) return ComparisonResult.Equal;

        return lhs < rhs ? ComparisonResult.LessThan : ComparisonResult.GreaterThan;
    }

    /// <summary>
    /// Whether both values sit close enough to zero that their difference — including their signs — is below
    /// what the measurements can resolve.
    /// </summary>
    /// <remarks>
    /// The relative test above cannot answer this: values straddling zero are always far apart in relative
    /// terms, however tiny they are, because the denominator shrinks with them. Whether that matters depends on
    /// a scale, and the measurands carry one. Where neither does — an exact value has no uncertainty bar — the
    /// dimension supplies a floor instead, so a length below the Planck length is still nothing.
    /// </remarks>
    private static bool AreBothIndistinguishableFromZero(Measurand l, Measurand r, double lhs, double rhs)
    {
        var finestError = FinestNonZeroError(l, r);

        // Zero when every operand is exact, which leaves the dimensional floor standing alone.
        var epsilon = Math.Max(FractionOfAbsErrIsZero * finestError, l.Dimensionality.Epsilon);

        return Math.Abs(lhs) < epsilon && Math.Abs(rhs) < epsilon;
    }

    /// <summary>
    /// The smallest finite uncertainty bar either measurand actually has, or zero if neither has one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The finest resolution present sets the threshold: if one instrument resolves to 1e-18, then 1e-15 is a
    /// real quantity and not noise, whatever the other instrument can see.
    /// </para>
    /// <para>
    /// Exact values are skipped rather than counted as zero. An operand with no uncertainty does not make the
    /// other one better resolved — it simply has no opinion — and letting a zero win the minimum would collapse
    /// the threshold to the dimensional floor. That matters because comparing a measurement against an exact
    /// limit of zero is ordinary: with the zero counted, <c>1e-20 ± 1e-9</c> reports as strictly less than an
    /// exact <c>0</c>, though nothing about that measurement can tell the two apart.
    /// </para>
    /// <para>
    /// Infinite uncertainty bars are skipped for the opposite reason, and the omission was a real defect: an infinite
    /// bar made the threshold infinite, so <i>every</i> pair of finite values came back
    /// <see cref="ComparisonResult.Equal"/> — 5 kg agreed with 10 kg. An unbounded uncertainty says the
    /// measurement resolves nothing, which is not the same as saying two values are the same, and it must not be
    /// allowed to set a scale for anything.
    /// </para>
    /// </remarks>
    private static double FinestNonZeroError(Measurand l, Measurand r)
    {
        ReadOnlySpan<double> errors =
        [
            l.KmsLowerAbsoluteUncertainty, l.KmsUpperAbsoluteUncertainty,
            r.KmsLowerAbsoluteUncertainty, r.KmsUpperAbsoluteUncertainty,
        ];

        var finest = 0d;
        foreach (var error in errors)
        {
            if (error > 0 && double.IsFinite(error) && (finest == 0 || error < finest)) finest = error;
        }

        return finest;
    }
}
