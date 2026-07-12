using Measurement.Exceptions;
using Measurement.Interfaces;
using Measurement.Models;
using Measurement.Uncertainty;

namespace Measurement;

public class Measurand
{
    internal readonly Quantity Quantity;
    internal readonly IUncertainty Uncertainty;


    public Measurand()
    {
        Quantity = Quantity.One;
        Uncertainty = GaussianUncertainty.FromRelErr(0);
    }

    public Measurand(Quantity quantity, IUncertainty uncertainty)
    {
        Quantity = quantity;
        Uncertainty = uncertainty;
    }

    public Dimensionality Dimensionality => Quantity.Dimensionality;

    public double RelativeError => Uncertainty.RelativeError(KmsValue);

    public double AbsoluteError(UnitOfMeasure unitOfMeasure)
    {
        return Math.Abs(Quantity.In(unitOfMeasure)) * RelativeError;
    }

    public double KmsValue => Quantity.KmsValue;
    public double KmsUpperAbsoluteError => Uncertainty.UpperAbsoluteError(KmsValue);
    public double KmsLowerAbsoluteError => Uncertainty.LowerAbsoluteError(KmsValue);
    public double KmsAbsoluteError => Math.Max(KmsUpperAbsoluteError, KmsLowerAbsoluteError);

    public double In(UnitOfMeasure unitOfMeasure)
    {
        return Quantity.In(unitOfMeasure);
    }

    public double TryIn(UnitOfMeasure unitOfMeasure)
    {
        return Quantity.TryIn(unitOfMeasure);
    }

    public bool IsValid()
    {
        return IsNaN() is false && IsFinite();
    }

    public bool IsNegative()
    {
        return Quantity.IsNegative();
    }

    public bool IsNaN()
    {
        return Quantity.IsNaN();
    }

    public bool IsInfinity()
    {
        return Quantity.IsInfinity();
    }

    public bool IsPositiveInfinity()
    {
        return Quantity.IsPositiveInfinity();
    }

    public bool IsNegativeInfinity()
    {
        return Quantity.IsNegativeInfinity();
    }

    public bool IsFinite()
    {
        return Quantity.IsFinite();
    }

    public bool IsNormal()
    {
        return Quantity.IsNormal();
    }

    public bool IsSubnormal()
    {
        return Quantity.IsSubnormal();
    }

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
        return $"{Quantity} {Uncertainty}";
    }

    private IErrorPropagator ResolveErrorPropagator()
    {
        return ConservativeGaussianPropagator.Instance;
    }

    private Measurand Sum(ErrorPropagationMethod method, params Measurand[] measurands)
    {
        if (measurands.Length == 0) return new Measurand();

        if (measurands.Any(q => q.Quantity.Dimensionality != measurands[0].Quantity.Dimensionality))
            throw new IncompatibleDimensionsException("Measurand summation of incompatibly dimensioned units");

        var kmsValue = measurands.Sum(q => q.Quantity.KmsValue);
        var quantity = new Quantity(kmsValue, measurands[0].Quantity.Dimensionality);
        return new Measurand(quantity, ResolveErrorPropagator().PropagateErrorThroughSum(method, measurands));
    }

    private Measurand Product(ErrorPropagationMethod method, params Measurand[] quantities)
    {
        if (quantities.Length == 0) return new Measurand();

        var product = quantities.Select(q => q.Quantity).Aggregate(
            Measurement.Quantity.One,
            (prod, q) => prod * q);

        return new Measurand(product, ResolveErrorPropagator().PropagateErrorThroughProduct(method, quantities));
    }

    public static Measurand operator -(Measurand quantity)
    {
        return new Measurand(-quantity.Quantity, quantity.Uncertainty.Negated(quantity.KmsValue));
    }

    public Measurand Reciprocal()
    {
        return new Measurand(Quantity.One / Quantity, Uncertainty.Reciprocal(KmsValue));
    }

    public Measurand ToPower(int exponent)
    {
        return new Measurand(
            Quantity.ToPower(exponent), 
            ResolveErrorPropagator().PropagateErrorThroughExponentiation(this, exponent, 1));
    }

    public Measurand ToRoot(int root)
    {
        return new Measurand(
            Quantity.ToRoot(root),
            ResolveErrorPropagator().PropagateErrorThroughExponentiation(this, 1, root));
    }

    public Measurand TryAdd(Measurand other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Measurand(
            Quantity.TryAdd(other.Quantity),
            Sum(method, this, other).Uncertainty);
    }

    public Measurand TrySubtract(Measurand other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Measurand(
            Quantity.TrySubtract(other.Quantity),
            Sum(method, this, -other).Uncertainty);
    }

    public Measurand Plus(Measurand other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Sum(method, this, other);
    }

    public Measurand Minus(Measurand other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Sum(method, this, -other);
    }

    public Measurand Times(Measurand other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Product(method, this, other);
    }

    public Measurand DividedBy(Measurand other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Product(method, this, other.Reciprocal());
    }
}