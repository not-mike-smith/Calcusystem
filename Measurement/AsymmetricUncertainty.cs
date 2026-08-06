using Measurement.Interfaces;
using Measurement.Extensions;
using Measurement.State;

namespace Measurement;

/// <summary>
/// Asymmetric uncertainty with independent errors above and below the nominal value. Both are stored in the same
/// form (relative or absolute, see <see cref="IsStoredAsAbs"/>). For propagation through arithmetic the larger of the two
/// is used as a conservative estimate; Monte Carlo propagation preserving the asymmetry is deferred to a later
/// milestone.
/// </summary>
public sealed class AsymmetricUncertainty : IUncertainty
{
    /// <summary>Whether the magnitudes are relative fractions or absolute KMS errors.</summary>
    /// <remarks>An implementation detail of the storage convention; it leaves the assembly only as part of
    /// <see cref="UncertaintyState"/>.</remarks>
    internal bool IsStoredAsAbs { get; }

    /// <summary>The stored error above the nominal value — relative or absolute per <see cref="IsStoredAsAbs"/>.</summary>
    internal double UpperMagnitude { get; }

    /// <summary>The stored error below the nominal value — relative or absolute per <see cref="IsStoredAsAbs"/>.</summary>
    internal double LowerMagnitude { get; }

    private AsymmetricUncertainty(bool isStoredAsAbs, double upper, double lower)
    {
        if (double.IsNaN(upper) || double.IsNegative(upper) || double.IsNaN(lower) || double.IsNegative(lower))
            throw new ArgumentException("Uncertainty magnitudes cannot be negative or NaN.");

        IsStoredAsAbs = isStoredAsAbs;
        UpperMagnitude = upper;
        LowerMagnitude = lower;
    }

    public double UpperAbsoluteError(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => UpperMagnitude,
        false => UpperMagnitude * Math.Abs(nominalKmsValue),
    };

    public double LowerAbsoluteError(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => LowerMagnitude,
        false => LowerMagnitude * Math.Abs(nominalKmsValue),
    };

    public double UpperRelativeError(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => UpperMagnitude.SafeDivide(Math.Abs(nominalKmsValue)),
        false => UpperMagnitude,
    };

    public double LowerRelativeError(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => LowerMagnitude.SafeDivide(Math.Abs(nominalKmsValue)),
        false => LowerMagnitude,
    };

    public double RelativeError(double nominalKmsValue) => 
        Math.Max(UpperRelativeError(nominalKmsValue), LowerRelativeError(nominalKmsValue));

    public double AbsoluteError(double nominalKmsValue) =>
        Math.Max(UpperAbsoluteError(nominalKmsValue), LowerAbsoluteError(nominalKmsValue));

    /// <summary>Rebuilds an uncertainty from its stored form. Reached from outside the assembly only through
    /// <see cref="UncertaintyFactory.FromState"/>.</summary>
    internal static AsymmetricUncertainty From(bool isStoredAsAbs, double upperMagnitude, double lowerMagnitude) =>
        new(isStoredAsAbs, upperMagnitude, lowerMagnitude);

    /// <summary>
    /// Creates an asymmetric uncertainty from independent upper/lower absolute errors.
    /// </summary>
    public static AsymmetricUncertainty FromAbsErr(Quantity upperAbsoluteError, Quantity lowerAbsoluteError)
    {
        return new AsymmetricUncertainty(
            true,
            upperAbsoluteError.KmsValue,
            lowerAbsoluteError.KmsValue);
    }

    /// <summary>
    /// Creates an asymmetric uncertainty from independent upper/lower relative errors (fractions).
    /// </summary>
    public static AsymmetricUncertainty FromRelErr(double upperRelativeError, double lowerRelativeError)
    {
        return new AsymmetricUncertainty(
            false,
            upperRelativeError,
            lowerRelativeError);
    }

    public IUncertainty Exponentiated(double nominalKmsValue, int exponentNumerator, int exponentDenominator)
    {
        // Relative error of x^p is |p| times the relative error of x; a negative p makes x^p decreasing,
        // swapping the directional bounds.
        var factor = Math.Abs((double)exponentNumerator / exponentDenominator);
        var upper = UpperRelativeError(nominalKmsValue) * factor;
        var lower = LowerRelativeError(nominalKmsValue) * factor;

        var decreasing = (exponentNumerator < 0) ^ (exponentDenominator < 0);
        return decreasing ? From(false, lower, upper) : From(false, upper, lower);
    }

    public IUncertainty Reciprocal(double nominalKmsValue)
    {
        return new AsymmetricUncertainty(
            false,
            LowerRelativeError(nominalKmsValue),
            UpperRelativeError(nominalKmsValue));
    }

    public IUncertainty Negated(double nominalKmsValue) =>
        new AsymmetricUncertainty(IsStoredAsAbs, LowerMagnitude, UpperMagnitude);

    /// <remarks>Explicit implementation: the storage form is reachable through <see cref="IUncertainty"/>, but is
    /// not part of this class's own public surface.</remarks>
    UncertaintyState IUncertainty.GetState() =>
        UncertaintyState.Asymmetric(IsStoredAsAbs, UpperMagnitude, LowerMagnitude);
}
