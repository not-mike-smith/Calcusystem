using System;
using Measurement.Exceptions;
using Measurement.Interfaces;
using Measurement.Models;
using Measurement.Uncertainty;

namespace Measurement;

public readonly struct Quantity
{
    private readonly double Value;
    public readonly Dimensionality Dimensionality;
    internal double KmsValue => Value;

    internal static Quantity One => new Quantity(1, Dimensionality.Dimensionless);

    public Quantity()
    {
        Value = double.NaN;
        Dimensionality = Dimensionality.Dimensionless;
    }

    public Quantity(double value, UnitOfMeasure unitOfMeasure)
    {
        Value = unitOfMeasure.ConvertToKmsValue(value);
        Dimensionality = unitOfMeasure.Dimensionality;
    }

    internal Quantity(double value, Dimensionality dimensionality)
    {
        Value = value;
        Dimensionality = dimensionality;
    }

    public Measurand Measurand(IUncertainty uncertainty)
    {
        return new Measurand(this, uncertainty);
    }

    public Measurand Measurand(UncertaintyFromNominalValue absoluteUncertainty)
    {
        return new Measurand(this, absoluteUncertainty(this));
    }

    public double In(UnitOfMeasure unitOfMeasure)
    {
        if (Dimensionality != unitOfMeasure.Dimensionality)
        {
            throw new IncompatibleDimensionsException(
                $"Cannot express {Dimensionality} value in {unitOfMeasure.Symbol}");
        }

        return unitOfMeasure.ConvertFromKmsValue(Value);
    }

    public double TryIn(UnitOfMeasure unitOfMeasure)
    {
        return Dimensionality == unitOfMeasure.Dimensionality
            ? unitOfMeasure.ConvertFromKmsValue(Value)
            : double.NaN;
    }

    public bool IsNegative()
    {
        return double.IsNegative(Value);
    }

    public bool IsNaN()
    {
        return double.IsNaN(Value);
    }

    public bool IsInfinity()
    {
        return double.IsInfinity(Value);
    }

    public bool IsPositiveInfinity()
    {
        return double.IsPositiveInfinity(Value);
    }

    public bool IsNegativeInfinity()
    {
        return double.IsNegativeInfinity(Value);
    }

    public bool IsFinite()
    {
        return double.IsFinite(Value);
    }

    public bool IsNormal()
    {
        return double.IsNormal(Value);
    }

    public bool IsSubnormal()
    {
        return double.IsSubnormal(Value);
    }

    public override string ToString()
    {
        return $"{Value:E4} {Dimensionality}"; // try to get fundamental unit later
    }

    public static Quantity operator +(Quantity lhs, Quantity rhs)
    {
        if (lhs.Dimensionality != rhs.Dimensionality)
        {
            throw new IncompatibleDimensionsException(
                $"Cannot add {lhs.Dimensionality} quantity and {rhs.Dimensionality} quantity");
        }

        return new Quantity(lhs.Value + rhs.Value, lhs.Dimensionality);
    }

    public static Quantity operator -(Quantity q)
    {
        return new Quantity(-q.Value, q.Dimensionality);
    }

    public static Quantity operator -(Quantity lhs, Quantity rhs)
    {
        if (lhs.Dimensionality != rhs.Dimensionality)
        {
            throw new IncompatibleDimensionsException(
                $"Cannot subtract {rhs.Dimensionality} quantity from {lhs.Dimensionality} quantity");
        }

        return new Quantity(lhs.Value - rhs.Value, lhs.Dimensionality);
    }

    public static Quantity operator *(Quantity lhs, Quantity rhs)
    {
        return new Quantity(lhs.Value * rhs.Value, lhs.Dimensionality * rhs.Dimensionality);
    }

    public static Quantity operator /(Quantity lhs, Quantity rhs)
    {
        return new Quantity(lhs.Value / rhs.Value, lhs.Dimensionality / rhs.Dimensionality);
    }

    public static explicit operator Quantity(double d)
    {
        return new Quantity(d, Dimensionality.Dimensionless);
    }

    public Quantity ToPower(int exponent)
    {
        return new Quantity(Math.Pow(Value, exponent), Dimensionality * exponent);
    }

    public Quantity ToRoot(int root)
    {
        return new Quantity(Math.Pow(Value, 1d / root), Dimensionality / root);
    }

    public Quantity TryAdd(Quantity other)
    {
        var value = Dimensionality == other.Dimensionality
            ? Value + other.Value
            : double.NaN;

        return new Quantity(value, Dimensionality);
    }

    public Quantity TrySubtract(Quantity other)
    {
        var value = Dimensionality == other.Dimensionality
            ? Value - other.Value
            : double.NaN;

        return new Quantity(value, Dimensionality);
    }
}
