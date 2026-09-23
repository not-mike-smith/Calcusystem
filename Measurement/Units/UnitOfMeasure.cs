using System;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Units;

/// <summary>
/// A named unit in which a <see cref="Quantity"/> can be expressed — a symbol, a
/// <see cref="Dimensionality"/>, and the linear factor that converts a value in this unit to and from the
/// internal KMS (kg-m-s) representation. Instances are created through <c>UnitFactory</c>, not directly.
/// </summary>
/// <remarks>
/// Conversion is a pure scaling (<c>kms = value × factor</c>) for the base class. Units whose zero point is
/// offset from the KMS zero — temperature scales, gauge pressure — are handled by the derived
/// <see cref="OffsetUnitOfMeasure"/>, which overrides the conversion methods; consumers interact with both
/// through this common type.
/// </remarks>
public class UnitOfMeasure // TODO: should this be a record?
{
    /// <summary>The physical dimension this unit measures.</summary>
    public readonly Dimensionality Dimensionality;

    /// <summary>The unit's display symbol (e.g. "N", "lbf", "°C").</summary>
    public readonly string Symbol;
    internal readonly double KmsConversionFactor;

    /// <summary>
    /// Constructed via <c>UnitFactory</c>. Validates that <paramref name="kmsConversionFactor"/> is positive,
    /// finite, and non-zero, and that <paramref name="symbol"/> is non-null.
    /// </summary>
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

    /// <summary>Returns the <see cref="Symbol"/>.</summary>
    public override string ToString()
    {
        return Symbol;
    }

    /// <summary>
    /// Converts a value expressed in this unit to its KMS-normalized equivalent. Overridden by
    /// <see cref="OffsetUnitOfMeasure"/> to apply a zero-point offset.
    /// </summary>
    public virtual double ConvertToKmsValue(double value)
    {
        return value * KmsConversionFactor;
    }

    /// <summary>
    /// Converts a KMS-normalized value back into this unit. Overridden by
    /// <see cref="OffsetUnitOfMeasure"/> to apply a zero-point offset.
    /// </summary>
    public virtual double ConvertFromKmsValue(double value)
    {
        return value / KmsConversionFactor;
    }

    /// <summary>
    /// Creates a <see cref="Quantity"/> from a value expressed in this unit, converting it to KMS.
    /// </summary>
    public Quantity Quantity(double value)
    {
        return new Quantity(value, this);
    }
}
