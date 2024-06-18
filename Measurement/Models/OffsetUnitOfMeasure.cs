using System;

namespace Measurement.Models;

public class OffsetUnitOfMeasure : UnitOfMeasure
{
    private readonly double _zeroOffset;
    public UnitOfMeasure DeltaUnit { get; }

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

    public override double ConvertToKmsValue(double value)
    {
        return (value + _zeroOffset) * KmsConversionFactor;
    }

    public override double ConvertFromKmsValue(double value)
    {
        return value / KmsConversionFactor - _zeroOffset;
    }
}
