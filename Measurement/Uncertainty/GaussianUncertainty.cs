using Measurement.Interfaces;
using Measurement.Extensions;

namespace Measurement.Uncertainty; // TODO: move to Measurement namespace

/// <summary>
/// Symmetric uncertainty: the same error above and below the nominal value. The error is stored either as a
/// relative fraction or as an absolute KMS value (see <see cref="IsStoredAsAbs"/>) — absolute storage is what lets a
/// zero-valued quantity carry a meaningful uncertainty. Which form is stored is invisible to consumers, who read
/// absolute or relative error through <see cref="IUncertainty"/>.
/// </summary>
public sealed class GaussianUncertainty : ISymmetricUncertainty // TODO rename to SymmetricUncertainty
{
    /// <summary>Whether <see cref="Magnitude"/> is a relative fraction or an absolute KMS error.</summary>
    public bool IsStoredAsAbs { get; }

    /// <summary>The stored error — a relative fraction or an absolute KMS value, per <see cref="IsStoredAsAbs"/>.</summary>
    public double Magnitude { get; }

    private GaussianUncertainty(bool isStoredAsAbs, double magnitude)
    {
        if (double.IsNaN(magnitude) || double.IsNegative(magnitude))
            throw new ArgumentException("Uncertainty magnitude cannot be negative or NaN.", nameof(magnitude));

        IsStoredAsAbs = isStoredAsAbs;
        Magnitude = magnitude;
    }

    public double RelativeError(double nominalKmsValue) =>
        IsStoredAsAbs
            ? Magnitude.SafeDivide(Math.Abs(nominalKmsValue))
            : Magnitude;

    public double AbsoluteError(double nominalKmsValue) =>
        IsStoredAsAbs
            ? Magnitude
            : Magnitude * Math.Abs(nominalKmsValue);

    /// <summary>Creates a symmetric uncertainty from a relative error (a fraction).</summary>
    public static GaussianUncertainty FromRelErr(double relativeError) => new(false, relativeError);

    /// <summary>Creates a symmetric uncertainty from an absolute error already in KMS units.</summary>
    internal static GaussianUncertainty FromKmsAbsErr(double kmsAbsoluteError) =>
        new(true, Math.Abs(kmsAbsoluteError));

    /// <summary>Rebuilds an uncertainty from its stored form (used by deserialization).</summary>
    public static GaussianUncertainty From(bool isStoredAsAbs, double magnitude) =>
        new(isStoredAsAbs, magnitude);

    /// <summary>
    /// Creates a symmetric uncertainty from an absolute error, stored directly (the nominal value is not needed).
    /// </summary>
    public static GaussianUncertainty FromAbsErr(Quantity absoluteError)
    {
        return FromKmsAbsErr(absoluteError.KmsValue);
    }

    public IUncertainty Exponentiated(double nominalKmsValue, int exponentNumerator, int exponentDenominator)
    {
        // Relative error of x^p is |p| times the relative error of x; the result is symmetric.
        var scaledRelativeError = RelativeError(nominalKmsValue) * exponentNumerator / exponentDenominator;
        return FromRelErr(Math.Abs(scaledRelativeError));
    }

    public IUncertainty Reciprocal(double nominalKmsValue) =>
        new GaussianUncertainty(false, RelativeError(nominalKmsValue)); // relative error is invariant under reciprocal

    public IUncertainty Negated(double nominalKmsValue) => this; // negation preserves both stored forms
}
