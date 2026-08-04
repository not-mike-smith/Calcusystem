using Measurement.Interfaces;

namespace Measurement.Uncertainty;

/// <summary>
/// Symmetric uncertainty: the same error above and below the nominal value. The error is stored either as a
/// relative fraction or as an absolute KMS value (see <see cref="Kind"/>) — absolute storage is what lets a
/// zero-valued quantity carry a meaningful uncertainty. Which form is stored is invisible to consumers, who read
/// absolute or relative error through <see cref="IUncertainty"/>.
/// </summary>
public sealed class GaussianUncertainty : ISymmetricUncertainty
{
    /// <summary>Whether <see cref="Magnitude"/> is a relative fraction or an absolute KMS error.</summary>
    public UncertaintyKind Kind { get; }

    /// <summary>The stored error — a relative fraction or an absolute KMS value, per <see cref="Kind"/>.</summary>
    public double Magnitude { get; }

    private GaussianUncertainty(UncertaintyKind kind, double magnitude)
    {
        if (double.IsNaN(magnitude) || double.IsNegative(magnitude))
            throw new ArgumentException("Uncertainty magnitude cannot be negative or NaN.", nameof(magnitude));

        Kind = kind;
        Magnitude = magnitude;
    }

    public double RelativeError(double nominalKmsValue) =>
        Kind == UncertaintyKind.Relative
            ? Magnitude
            : nominalKmsValue == 0 ? double.PositiveInfinity : Magnitude / Math.Abs(nominalKmsValue);

    public double AbsoluteError(double nominalKmsValue) =>
        Kind == UncertaintyKind.Absolute
            ? Magnitude
            : Magnitude * Math.Abs(nominalKmsValue);

    /// <summary>Creates a symmetric uncertainty from a relative error (a fraction).</summary>
    public static GaussianUncertainty FromRelErr(double relativeError) =>
        new(UncertaintyKind.Relative, relativeError);

    /// <summary>Creates a symmetric uncertainty from an absolute error already in KMS units.</summary>
    internal static GaussianUncertainty FromKmsAbsErr(double kmsAbsoluteError) =>
        new(UncertaintyKind.Absolute, Math.Abs(kmsAbsoluteError));

    /// <summary>Rebuilds an uncertainty from its stored form (used by deserialization).</summary>
    public static GaussianUncertainty From(UncertaintyKind kind, double magnitude) =>
        new(kind, magnitude);

    /// <summary>
    /// Creates a symmetric uncertainty from an absolute error. Absolute error is stored directly and needs no
    /// nominal value, so the returned delegate ignores the value it is given.
    /// </summary>
    public static UncertaintyFromNominalValue FromAbsErr(Quantity absoluteError)
    {
        var uncertainty = FromKmsAbsErr(absoluteError.KmsValue);
        return _ => uncertainty;
    }

    public IUncertainty Reciprocal(double nominalKmsValue) =>
        Kind == UncertaintyKind.Relative
            ? this // relative error is invariant under reciprocal
            : new GaussianUncertainty(UncertaintyKind.Absolute, Magnitude / (nominalKmsValue * nominalKmsValue));

    public IUncertainty Negated(double nominalKmsValue) => this; // negation preserves both stored forms
}
