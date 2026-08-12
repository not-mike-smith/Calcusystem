using Calcusystem.Core;
using Calcusystem.Measurement.Exceptions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.State;

namespace Calcusystem.Measurement;

public class Measurand : IStateful<Measurand, MeasurandState>
{
    internal readonly Quantity Quantity;
    public readonly IUncertainty Uncertainty;


    public Measurand()
    {
        Quantity = Quantity.One;
        Uncertainty = SymmetricUncertainty.FromRelErr(0);
    }

    public Measurand(Quantity quantity, IUncertainty uncertainty)
    {
        Quantity = quantity;
        Uncertainty = uncertainty;
    }

    public Dimensionality Dimensionality => Quantity.Dimensionality;

    public double RelativeError => Uncertainty.RelativeError(KmsValue);
    public double UpperRelativeError => Uncertainty.UpperRelativeError(KmsValue);
    public double LowerRelativeError => Uncertainty.LowerRelativeError(KmsValue);

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

    /// <summary>
    /// The propagator to combine uncertainties with: the one supplied, or the conservative Gaussian default.
    /// </summary>
    /// <remarks>
    /// Which propagator is used and whether operands are <i>correlated</i> are different questions on different
    /// axes. Correlation is a statement about the model — whether these two quantities move together — and rides
    /// on the operation as an <see cref="ErrorPropagationMethod"/>. The propagator is the numerical method for
    /// combining uncertainties at all, and is a property of the calculation. Swapping it therefore does not
    /// discard what the model says about correlation; both are passed through together.
    /// </remarks>
    private static IErrorPropagator ResolveErrorPropagator(IErrorPropagator? supplied) =>
        supplied ?? ConservativeGaussianPropagator.Instance;

    private Measurand Sum(ErrorPropagationMethod method, IErrorPropagator? propagator, params Measurand[] measurands)
    {
        if (measurands.Length == 0) return new Measurand();

        if (measurands.Any(q => q.Quantity.Dimensionality != measurands[0].Quantity.Dimensionality))
            throw new IncompatibleDimensionsException("Measurand summation of incompatibly dimensioned units");

        var kmsValue = measurands.Sum(q => q.Quantity.KmsValue);
        var quantity = new Quantity(kmsValue, measurands[0].Quantity.Dimensionality);
        return new Measurand(quantity, ResolveErrorPropagator(propagator).PropagateErrorThroughSum(method, measurands));
    }

    private Measurand Product(ErrorPropagationMethod method, IErrorPropagator? propagator, params Measurand[] quantities)
    {
        if (quantities.Length == 0) return new Measurand();

        var product = quantities.Select(q => q.Quantity).Aggregate(
            Quantity.One,
            (prod, q) => prod * q);

        return new Measurand(product, ResolveErrorPropagator(propagator).PropagateErrorThroughProduct(method, quantities));
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
            Uncertainty.Exponentiated(KmsValue, exponent, 1));
    }

    public Measurand ToRoot(int root)
    {
        return new Measurand(
            Quantity.ToRoot(root),
            Uncertainty.Exponentiated(KmsValue, 1, root));
    }

    public Measurand TryAdd(
        Measurand other,
        ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated,
        IErrorPropagator? propagator = null)
    {
        var quantity = Quantity.TryAdd(other.Quantity);
        var uncertainty = quantity.IsNaN()
            ? SymmetricUncertainty.FromRelErr(0)
            : ResolveErrorPropagator(propagator).PropagateErrorThroughSum(method, [this, other]);

        return new Measurand(quantity, uncertainty);
    }

    public Measurand TrySubtract(
        Measurand other,
        ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated,
        IErrorPropagator? propagator = null)
    {
        var quantity = Quantity.TrySubtract(other.Quantity);
        var uncertainty = quantity.IsNaN()
            ? SymmetricUncertainty.FromRelErr(0)
            : ResolveErrorPropagator(propagator).PropagateErrorThroughSum(method, [this, -other]);

        return new Measurand(quantity, uncertainty);
    }

    public Measurand Plus(
        Measurand other,
        ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated,
        IErrorPropagator? propagator = null)
    {
        return Sum(method, propagator, this, other);
    }

    public Measurand Minus(
        Measurand other,
        ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated,
        IErrorPropagator? propagator = null)
    {
        return Sum(method, propagator, this, -other);
    }

    public Measurand Times(
        Measurand other,
        ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated,
        IErrorPropagator? propagator = null)
    {
        return Product(method, propagator, this, other);
    }

    public Measurand DividedBy(
        Measurand other,
        ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated,
        IErrorPropagator? propagator = null)
    {
        return Product(method, propagator, this, other.Reciprocal());
    }

    /// <inheritdoc/>
    public MeasurandState GetState() => new(Quantity.GetState(), Uncertainty.GetState());

    /// <inheritdoc/>
    public static Measurand FromState(MeasurandState state) =>
        new(Quantity.FromState(state.Quantity), UncertaintyFactory.FromState(state.Uncertainty));
}