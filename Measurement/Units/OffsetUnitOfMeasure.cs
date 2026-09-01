using System;
using Calcusystem.Measurement.Dimensions;

namespace Calcusystem.Measurement.Units;

/// <summary>
/// A <see cref="UnitOfMeasure"/> whose zero point is offset from the KMS zero, so conversion is affine
/// (<c>kms = (value + offset) × factor</c>) rather than a pure scaling. Used for temperature scales (0 °C ≠ 0 K)
/// and gauge pressure (zero = nominal atmospheric). The offset is fixed at construction, not a live ambient reading.
/// </summary>
public class OffsetUnitOfMeasure : UnitOfMeasure
{
    private readonly double _zeroOffset;

    /// <summary>
    /// The corresponding non-offset unit for expressing a <i>difference</i> in this unit (e.g. Δ°C), where the
    /// zero-point offset must not be applied. A temperature change of 5 °C is 5 K, not 278.15 K.
    /// </summary>
    public UnitOfMeasure DeltaUnit { get; }

    /// <summary>
    /// Constructed via <c>UnitFactory</c>. Validates that <paramref name="zeroOffset"/> is finite, and derives
    /// the <see cref="DeltaUnit"/> from the same scale without the offset.
    /// </summary>
    internal OffsetUnitOfMeasure(
        Dimensionality dimensionality,
        string symbol,
        double kmsConversionFactor,
        double zeroOffset)
        : base (dimensionality, symbol, kmsConversionFactor)
    {
        if (double.IsNaN(zeroOffset))
        {
            throw new ArgumentException( "zero offset cannot be NaN", nameof(zeroOffset));
        }

        if (double.IsInfinity(zeroOffset))
        {
            throw new ArgumentException("zero offset must be finite", nameof(zeroOffset));
        }

        _zeroOffset = zeroOffset;
        DeltaUnit = new UnitOfMeasure(
            Dimensionality,
            $"Δ{Symbol}",
            KmsConversionFactor);
    }

    /// <summary>Converts a value in this unit to KMS, applying the zero-point offset before scaling.</summary>
    public override double ConvertToKmsValue(double value)
    {
        return (value + _zeroOffset) * KmsConversionFactor;
    }

    /// <summary>Converts a KMS value back into this unit, removing the zero-point offset after scaling.</summary>
    public override double ConvertFromKmsValue(double value)
    {
        return value / KmsConversionFactor - _zeroOffset;
    }
}
