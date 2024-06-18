using System;
using System.Linq;
using Measurement.Extensions;
using Measurement.Models;

namespace Measurement.Factories;

public static class UnitFactory
{
    public static UnitOfMeasure Create(
        string symbol,
        Dimensionality dimensionality,
        double kmsConversionFactor)
    {
        return new UnitOfMeasure(dimensionality, symbol, kmsConversionFactor);
    }

    public static UnitOfMeasure Create(
        string symbol,
        double scale,
        UnitOfMeasure unitOfMeasure)
    {
        if (scale == 0)
            throw new DivideByZeroException("Unit of measure conversion factor cannot be zero");

        if (scale < 0)
            throw new InvalidOperationException("Unit of measure conversion factor must be positive");

        var dimensionality = (unitOfMeasure ?? throw new ArgumentNullException(nameof(unitOfMeasure))).Dimensionality;
        var kmsConversionFactor = unitOfMeasure.KmsConversionFactor * scale;
        return new UnitOfMeasure(dimensionality, symbol, kmsConversionFactor);
    }

    public static UnitOfMeasure Create(
        string symbol,
        double scale,
        params (UnitOfMeasure unitOfMeasure, int exponent)[] constituentUnits)
    {
        var dimensionality = ReduceDimensions(constituentUnits);
        var kmsConversionFactor = ReduceKmsConversionFactor(constituentUnits) * scale;
        return new UnitOfMeasure(dimensionality, symbol, kmsConversionFactor);
    }

    public static UnitOfMeasure Create(
        string symbol,
        params (UnitOfMeasure unitOfMeasure, int exponent)[] constituentUnits)
    {
        var dimensionality = ReduceDimensions(constituentUnits);
        var kmsConversionFactor = ReduceKmsConversionFactor(constituentUnits);
        return new UnitOfMeasure(dimensionality, symbol, kmsConversionFactor);
    }

    public static UnitOfMeasure Create(
        params (UnitOfMeasure unitOfMeasure, int exponent)[] constituentUnits)
    {
        var symbol = ReduceSymbols(constituentUnits);
        var dimensionality = ReduceDimensions(constituentUnits);
        var kmsConversionFactor = ReduceKmsConversionFactor(constituentUnits);

        return new UnitOfMeasure(dimensionality, symbol, kmsConversionFactor);
    }

    public static OffsetUnitOfMeasure Create(
        string symbol,
        double kmsConversionFactor,
        UnitOfMeasure unitOfMeasure,
        double zeroOffset)
    {
        return new OffsetUnitOfMeasure(
            unitOfMeasure.Dimensionality,
            symbol,
            kmsConversionFactor,
            zeroOffset);
    }

    public static Dimensionality ReduceDimensions((UnitOfMeasure unitOfMeasure, int exponent)[] constituentUnits)
    {
        return constituentUnits.Aggregate(
            Dimensionality.Dimensionless,
            (d, tuple) => d * (tuple.unitOfMeasure.Dimensionality * tuple.exponent));
    }

    public static double ReduceKmsConversionFactor((UnitOfMeasure unitOfMeasure, int exponent)[] constituentUnits)
    {
        return constituentUnits.Aggregate(
            1d,
            (factor, tuple) => factor * Math.Pow(tuple.unitOfMeasure.KmsConversionFactor, tuple.exponent));
    }

    public static string ReduceSymbols((UnitOfMeasure unitOfMeasure, int exponent)[] constituentUnits)
    {
        var numerators = constituentUnits.Where(t => t.exponent > 0)
            .Select(t => t.exponent == 1
                ? t.unitOfMeasure.Symbol
                : $"{t.unitOfMeasure.Symbol}{t.exponent.ToSuperscript()}");

        var numerator = string.Join('·', numerators);
        numerator = numerator.Length == 0 ? "1" : numerator;
        var denominators = constituentUnits.Where(t => t.exponent < 0)
            .Select(t => t.exponent == -1
                ? t.unitOfMeasure.Symbol
                : $"{t.unitOfMeasure.Symbol}{(-t.exponent).ToSuperscript()}");

        var denominator = string.Join('·', denominators);
        return denominator.Length == 0 ? numerator : $"{numerator}/{denominator}";
    }

    public static UnitOfMeasure Create(Metric metric, UnitOfMeasure unitOfMeasure)
    {
        return new UnitOfMeasure(
            unitOfMeasure.Dimensionality,
            metric.Prefix + unitOfMeasure.Symbol,
            metric.Factor * unitOfMeasure.KmsConversionFactor);
    }
}
