using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Snapshots;
using Calcusystem.Measurement.Factories;

namespace Calcusystem.Measurement.Uncertainties;

/// <summary>
/// Asymmetric uncertainty with independent magnitudes above and below the nominal value. Both are stored in the same
/// form (relative or absolute, see <see cref="IsStoredAsAbs"/>). For propagation through arithmetic the larger of the two
/// is used as a conservative estimate.
/// </summary>
public sealed class AsymmetricUncertainty : IUncertainty
{
    /// <summary>Whether the magnitudes are relative fractions or absolute KMS uncertainties.</summary>
    /// <remarks>An implementation detail of the storage convention; it leaves the assembly only as part of
    /// <see cref="UncertaintySnapshot"/>.</remarks>
    internal bool IsStoredAsAbs { get; }

    /// <summary>The stored uncertainty above the nominal value — relative or absolute per <see cref="IsStoredAsAbs"/>.</summary>
    internal double UpperMagnitude { get; }

    /// <summary>The stored uncertainty below the nominal value — relative or absolute per <see cref="IsStoredAsAbs"/>.</summary>
    internal double LowerMagnitude { get; }

    private AsymmetricUncertainty(bool isStoredAsAbs, double upper, double lower)
    {
        if (double.IsNaN(upper) || double.IsNegative(upper) || double.IsNaN(lower) || double.IsNegative(lower))
            throw new ArgumentException("Uncertainty magnitudes cannot be negative or NaN.");

        IsStoredAsAbs = isStoredAsAbs;
        UpperMagnitude = upper;
        LowerMagnitude = lower;
    }

    /// <inheritdoc/>
    public double UpperAbsoluteUncertainty(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => UpperMagnitude,
        false => UpperMagnitude * Math.Abs(nominalKmsValue),
    };

    /// <inheritdoc/>
    public double LowerAbsoluteUncertainty(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => LowerMagnitude,
        false => LowerMagnitude * Math.Abs(nominalKmsValue),
    };

    /// <inheritdoc/>
    public double UpperRelativeUncertainty(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => UpperMagnitude.SafeDivide(Math.Abs(nominalKmsValue)),
        false => UpperMagnitude,
    };

    /// <inheritdoc/>
    public double LowerRelativeUncertainty(double nominalKmsValue) => IsStoredAsAbs switch
    {
        true => LowerMagnitude.SafeDivide(Math.Abs(nominalKmsValue)),
        false => LowerMagnitude,
    };

    /// <inheritdoc/>
    public double RelativeUncertainty(double nominalKmsValue) => 
        Math.Max(UpperRelativeUncertainty(nominalKmsValue), LowerRelativeUncertainty(nominalKmsValue));

    /// <inheritdoc/>
    public double AbsoluteUncertainty(double nominalKmsValue) =>
        Math.Max(UpperAbsoluteUncertainty(nominalKmsValue), LowerAbsoluteUncertainty(nominalKmsValue));

    /// <summary>Rebuilds an uncertainty from its stored form. Reached from outside the assembly only through
    /// <see cref="UncertaintyFactory.FromSnapshot"/>.</summary>
    internal static AsymmetricUncertainty From(bool isStoredAsAbs, double upperMagnitude, double lowerMagnitude) =>
        new(isStoredAsAbs, upperMagnitude, lowerMagnitude);

    /// <summary>
    /// Creates an asymmetric uncertainty from independent upper/lower absolute uncertainties.
    /// </summary>
    public static AsymmetricUncertainty FromAbsolute(Quantity upperAbsoluteUncertainty, Quantity lowerAbsoluteUncertainty)
    {
        return new AsymmetricUncertainty(
            true,
            upperAbsoluteUncertainty.KmsValue,
            lowerAbsoluteUncertainty.KmsValue);
    }

    /// <summary>
    /// Creates an asymmetric uncertainty from independent upper/lower relative uncertainties (fractions).
    /// </summary>
    public static AsymmetricUncertainty FromRelative(double upperRelativeUncertainty, double lowerRelativeUncertainty)
    {
        return new AsymmetricUncertainty(
            false,
            upperRelativeUncertainty,
            lowerRelativeUncertainty);
    }

    /// <inheritdoc/>
    public IUncertainty Exponentiated(double nominalKmsValue, int exponentNumerator, int exponentDenominator)
    {
        // Relative uncertainty of x^p is |p| times the relative uncertainty of x; a negative p makes x^p decreasing,
        // swapping the directional bounds.
        var factor = Math.Abs((double)exponentNumerator / exponentDenominator);
        var upper = UpperRelativeUncertainty(nominalKmsValue) * factor;
        var lower = LowerRelativeUncertainty(nominalKmsValue) * factor;

        var decreasing = (exponentNumerator < 0) ^ (exponentDenominator < 0);
        return decreasing ? From(false, lower, upper) : From(false, upper, lower);
    }

    /// <inheritdoc/>
    public IUncertainty Reciprocal(double nominalKmsValue)
    {
        return new AsymmetricUncertainty(
            false,
            LowerRelativeUncertainty(nominalKmsValue),
            UpperRelativeUncertainty(nominalKmsValue));
    }

    /// <inheritdoc/>
    public IUncertainty Negated(double nominalKmsValue) =>
        new AsymmetricUncertainty(IsStoredAsAbs, LowerMagnitude, UpperMagnitude);

    /// <remarks>Explicit implementation: the storage form is reachable through <see cref="IUncertainty"/>, but is
    /// not part of this class's own public surface.</remarks>
    UncertaintySnapshot IUncertainty.GetSnapshot() =>
        UncertaintySnapshot.Asymmetric(IsStoredAsAbs, UpperMagnitude, LowerMagnitude);
}
