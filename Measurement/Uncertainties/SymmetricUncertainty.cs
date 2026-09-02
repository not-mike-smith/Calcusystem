using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Snapshots;
using Calcusystem.Measurement.Factories;

namespace Calcusystem.Measurement.Uncertainties;

/// <summary>
/// Symmetric uncertainty: the same error above and below the nominal value. The error is stored either as a
/// relative fraction or as an absolute KMS value (see <see cref="IsStoredAsAbs"/>) — absolute storage is what lets a
/// zero-valued quantity carry a meaningful uncertainty. Which form is stored is invisible to consumers, who read
/// absolute or relative error through <see cref="IUncertainty"/>.
/// </summary>
public sealed class SymmetricUncertainty : ISymmetricUncertainty
{
    /// <summary>Whether <see cref="Magnitude"/> is a relative fraction or an absolute KMS error.</summary>
    /// <remarks>An implementation detail of the storage convention; it leaves the assembly only as part of
    /// <see cref="UncertaintySnapshot"/>.</remarks>
    internal bool IsStoredAsAbs { get; }

    /// <summary>The stored error — a relative fraction or an absolute KMS value, per <see cref="IsStoredAsAbs"/>.</summary>
    internal double Magnitude { get; }

    private SymmetricUncertainty(bool isStoredAsAbs, double magnitude)
    {
        if (double.IsNaN(magnitude) || double.IsNegative(magnitude))
            throw new ArgumentException("Uncertainty magnitude cannot be negative or NaN.", nameof(magnitude));

        IsStoredAsAbs = isStoredAsAbs;
        Magnitude = magnitude;
    }

    public double RelativeUncertainty(double nominalKmsValue) =>
        IsStoredAsAbs
            ? Magnitude.SafeDivide(Math.Abs(nominalKmsValue))
            : Magnitude;

    public double AbsoluteUncertainty(double nominalKmsValue) =>
        IsStoredAsAbs
            ? Magnitude
            : Magnitude * Math.Abs(nominalKmsValue);

    /// <summary>Creates a symmetric uncertainty from a relative error (a fraction).</summary>
    public static SymmetricUncertainty FromRelative(double relativeUncertainty) => new(false, relativeUncertainty);

    /// <summary>Creates a symmetric uncertainty from an absolute error already in KMS units.</summary>
    internal static SymmetricUncertainty FromKmsAbsErr(double kmsAbsoluteUncertainty) =>
        new(true, Math.Abs(kmsAbsoluteUncertainty));

    /// <summary>Rebuilds an uncertainty from its stored form. Reached from outside the assembly only through
    /// <see cref="UncertaintyFactory.FromSnapshot"/>.</summary>
    internal static SymmetricUncertainty From(bool isStoredAsAbs, double magnitude) =>
        new(isStoredAsAbs, magnitude);

    /// <summary>
    /// Creates a symmetric uncertainty from an absolute error, stored directly (the nominal value is not needed).
    /// </summary>
    public static SymmetricUncertainty FromAbsolute(Quantity absoluteUncertainty)
    {
        return FromKmsAbsErr(absoluteUncertainty.KmsValue);
    }

    public static SymmetricUncertainty Exact() => new(false, 0d);

    public IUncertainty Exponentiated(double nominalKmsValue, int exponentNumerator, int exponentDenominator)
    {
        // Relative error of x^p is |p| times the relative error of x; the result is symmetric.
        var scaledRelativeUncertainty = RelativeUncertainty(nominalKmsValue) * exponentNumerator / exponentDenominator;
        return FromRelative(Math.Abs(scaledRelativeUncertainty));
    }

    public IUncertainty Reciprocal(double nominalKmsValue) =>
        new SymmetricUncertainty(false, RelativeUncertainty(nominalKmsValue)); // relative error is invariant under reciprocal

    public IUncertainty Negated(double nominalKmsValue) => this; // negation preserves both stored forms

    /// <remarks>Explicit implementation: the storage form is reachable through <see cref="IUncertainty"/>, but is
    /// not part of this class's own public surface.</remarks>
    UncertaintySnapshot IUncertainty.GetSnapshot() => UncertaintySnapshot.Symmetric(IsStoredAsAbs, Magnitude);
}
