using System;

namespace Measurement.Models;

public class UnitOfMeasure
{
    public readonly Dimensionality Dimensionality;
    public readonly string Symbol;
    internal readonly double KmsConversionFactor;

    internal UnitOfMeasure(
        Dimensionality dimensionality,
        string symbol,
        double kmsConversionFactor)
    {
        if (kmsConversionFactor == 0)
            throw new DivideByZeroException("Unit of measure conversion factor cannot be zero");

        if (kmsConversionFactor < 0)
            throw new ArgumentException(
                "Unit of measure conversion factor must be positive",
                nameof(kmsConversionFactor));

        if (double.IsNaN(kmsConversionFactor))
        {
            throw new ArgumentException(
                "Unit of measure conversion factor cannot be NaN",
                nameof(kmsConversionFactor));
        }

        if (double.IsInfinity(kmsConversionFactor))
        {
            throw new ArgumentException(
                "Unit of measure conversion factor must be finite",
                nameof(kmsConversionFactor));
        }

        Dimensionality = dimensionality;
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        KmsConversionFactor = kmsConversionFactor;
    }

    public override string ToString()
    {
        return Symbol;
    }

    public virtual double ConvertToKmsValue(double value)
    {
        return value * KmsConversionFactor;
    }

    public virtual double ConvertFromKmsValue(double value)
    {
        return value / KmsConversionFactor;
    }
}
