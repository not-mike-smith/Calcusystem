using Measurement.Interfaces;

namespace Measurement.Uncertainty;

/// <summary>
/// Asymmetric uncertainty with independent errors above and below the nominal value. Both are stored in the same
/// form (relative or absolute, see <see cref="Kind"/>). For propagation through arithmetic the larger of the two
/// is used as a conservative estimate; Monte Carlo propagation preserving the asymmetry is deferred to a later
/// milestone.
/// </summary>
public sealed class AsymmetricUncertainty : IUncertainty
{
    /// <summary>Whether the magnitudes are relative fractions or absolute KMS errors.</summary>
    public UncertaintyKind Kind { get; }

    /// <summary>The stored error above the nominal value — relative or absolute per <see cref="Kind"/>.</summary>
    public double UpperMagnitude { get; }

    /// <summary>The stored error below the nominal value — relative or absolute per <see cref="Kind"/>.</summary>
    public double LowerMagnitude { get; }

    /// <summary>Creates an asymmetric uncertainty from independent upper/lower relative errors.</summary>
    public AsymmetricUncertainty(double upperRelativeError, double lowerRelativeError)
        : this(UncertaintyKind.Relative, upperRelativeError, lowerRelativeError)
    {
    }

    private AsymmetricUncertainty(UncertaintyKind kind, double upper, double lower)
    {
        if (double.IsNaN(upper) || double.IsNegative(upper) || double.IsNaN(lower) || double.IsNegative(lower))
            throw new ArgumentException("Uncertainty magnitudes cannot be negative or NaN.");

        Kind = kind;
        UpperMagnitude = upper;
        LowerMagnitude = lower;
    }

    public double UpperAbsoluteError(double nominalKmsValue) =>
        Kind == UncertaintyKind.Absolute ? UpperMagnitude : UpperMagnitude * Math.Abs(nominalKmsValue);

    public double LowerAbsoluteError(double nominalKmsValue) =>
        Kind == UncertaintyKind.Absolute ? LowerMagnitude : LowerMagnitude * Math.Abs(nominalKmsValue);

    public double RelativeError(double nominalKmsValue) =>
        Kind == UncertaintyKind.Relative
            ? Math.Max(UpperMagnitude, LowerMagnitude)
            : nominalKmsValue == 0
                ? double.PositiveInfinity
                : Math.Max(UpperMagnitude, LowerMagnitude) / Math.Abs(nominalKmsValue);

    public double AbsoluteError(double nominalKmsValue) =>
        Math.Max(UpperAbsoluteError(nominalKmsValue), LowerAbsoluteError(nominalKmsValue));

    /// <summary>Rebuilds an uncertainty from its stored form (used by deserialization).</summary>
    public static AsymmetricUncertainty From(UncertaintyKind kind, double upperMagnitude, double lowerMagnitude) =>
        new(kind, upperMagnitude, lowerMagnitude);

    /// <summary>
    /// Creates an asymmetric uncertainty from independent upper/lower absolute errors. Absolute errors are stored
    /// directly and need no nominal value, so the returned delegate ignores the value it is given.
    /// </summary>
    public static UncertaintyFromNominalValue FromAbsErr(Quantity upperAbsoluteError, Quantity lowerAbsoluteError)
    {
        var uncertainty = new AsymmetricUncertainty(
            UncertaintyKind.Absolute,
            Math.Abs(upperAbsoluteError.KmsValue),
            Math.Abs(lowerAbsoluteError.KmsValue));
        return _ => uncertainty;
    }

    public IUncertainty Reciprocal(double nominalKmsValue) =>
        Kind == UncertaintyKind.Relative
            ? new AsymmetricUncertainty(UncertaintyKind.Relative, LowerMagnitude, UpperMagnitude)
            : new AsymmetricUncertainty(
                UncertaintyKind.Absolute,
                LowerMagnitude / (nominalKmsValue * nominalKmsValue),
                UpperMagnitude / (nominalKmsValue * nominalKmsValue));

    public IUncertainty Negated(double nominalKmsValue) =>
        new AsymmetricUncertainty(Kind, LowerMagnitude, UpperMagnitude);
}
