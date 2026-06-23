using System;
using System.Linq;
using Measurement.Exceptions;
using Measurement.Extensions;
using Measurement.Models;
using Measurement.Uncertainty;

namespace Measurement.BaseClasses;

public abstract class PrecisionQuantity : PhysicalQuantity
{
    private readonly IUncertainty _uncertainty;

    protected PrecisionQuantity(double value, UnitOfMeasure unitOfMeasure, double relativeError)
        : base(value, unitOfMeasure)
    {
        _uncertainty = new GaussianUncertainty(relativeError);
    }

    protected PrecisionQuantity(Quantity quantity, double relativeError) : base(quantity)
    {
        _uncertainty = new GaussianUncertainty(relativeError);
    }

    protected PrecisionQuantity(Quantity quantity, IUncertainty uncertainty) : base(quantity)
    {
        _uncertainty = uncertainty;
    }

    public IUncertainty Uncertainty => _uncertainty;

    public double RelativeError => _uncertainty.RelativeError(KmsValue);

    public double AbsoluteError(UnitOfMeasure unitOfMeasure)
    {
        return Math.Abs(Quantity.In(unitOfMeasure)) * RelativeError;
    }

    public double KmsValue => Quantity.KmsValue;
    public double KmsUpperAbsoluteError => _uncertainty.UpperAbsoluteError(KmsValue);
    public double KmsLowerAbsoluteError => _uncertainty.LowerAbsoluteError(KmsValue);
    public double KmsAbsoluteError => Math.Max(KmsUpperAbsoluteError, KmsLowerAbsoluteError);

    public double AbsoluteErrorIn(UnitOfMeasure unit)
    {
        return In(unit) * RelativeError;
    }

    public double TryAbsoluteErrorIn(UnitOfMeasure unit)
    {
        return TryIn(unit) * RelativeError;
    }

    public override string ToString()
    {
        return base.ToString() + $" ±{RelativeError/100:g2}%";
    }

    public static Delta Sum(ErrorPropagationMethod method, params PrecisionQuantity[] quantities)
    {
        if (quantities.Length == 0) return new Delta(new Quantity(), 0);

        if (quantities.Any(q => q.Quantity.Dimensionality != quantities[0].Quantity.Dimensionality))
            throw new IncompatibleDimensionsException("PrecisionQuantity summation of incompatibly dimensioned units");

        var kmsValue = quantities.Sum(q => q.Quantity.KmsValue);
        var quantity = new Quantity(kmsValue, quantities[0].Quantity.Dimensionality);
        return new Delta(quantity, PropagateRelativeErrorThroughSum(method, quantities));
    }

    public static double PropagateRelativeErrorThroughSum(
        ErrorPropagationMethod method,
        params PrecisionQuantity[] quantities)
    {
        double absoluteError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => Math.Sqrt(quantities.SumOfSquares(Absolute)),
            ErrorPropagationMethod.Correlated => quantities.Sum(Absolute),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return absoluteError / quantities.Sum(q => q.Quantity.KmsValue);
    }

    public static Delta Product(ErrorPropagationMethod method, params PrecisionQuantity[] quantities)
    {
        if (quantities.Length == 0) return new Delta(new Quantity(), 0);

        var product = quantities.Select(q => q.Quantity).Aggregate(
            Measurement.Quantity.One,
            (prod, q) => prod * q);

        var relativeError = PropagateRelativeErrorThroughProduct(method, quantities);
        return new Delta(product, relativeError);
    }

    public static double PropagateRelativeErrorThroughProduct(
        ErrorPropagationMethod method,
        params PrecisionQuantity[] quantities)
    {
        return method switch
        {
            ErrorPropagationMethod.Uncorrelated => Math.Sqrt(quantities.SumOfSquares(Relative)),
            ErrorPropagationMethod.Correlated => quantities.Sum(Relative), // TODO double-check this
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }

    public static Delta Quotient(
        ErrorPropagationMethod method,
        PrecisionQuantity numerator,
        PrecisionQuantity denominator)
    {
        return Product(method, numerator, denominator.Reciprocal());
    }

    public static PrecisionQuantity operator -(PrecisionQuantity quantity)
    {
        return new Delta(-quantity.Quantity, quantity.Uncertainty);
    }

    public PrecisionQuantity Reciprocal()
    {
        return this switch
        {
            Delta => new Delta(Quantity.One / Quantity, Uncertainty),
            Magnitude => new Magnitude(Quantity.One / Quantity, Uncertainty),
            _ => throw new InvalidOperationException()
        };
    }

    private static double Absolute(PrecisionQuantity precisionQuantity) => precisionQuantity.KmsAbsoluteError;
    private static double Relative(PrecisionQuantity precisionQuantity) => precisionQuantity.RelativeError;
}
