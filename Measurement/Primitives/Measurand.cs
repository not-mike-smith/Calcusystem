using Calcusystem.Core.Interfaces;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Exceptions;
using Calcusystem.Measurement.Factories;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Snapshots;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Measurement.Primitives;

public class Measurand : ISnapshotting<Measurand, MeasurandSnapshot>
{
    internal readonly Quantity Quantity;
    public readonly IUncertainty Uncertainty;


    public Measurand()
    {
        Quantity = Quantity.One;
        Uncertainty = SymmetricUncertainty.FromRelative(0);
    }

    public Measurand(Quantity quantity, IUncertainty uncertainty)
    {
        Quantity = quantity;
        Uncertainty = uncertainty;
    }

    public Dimensionality Dimensionality => Quantity.Dimensionality;

    public double RelativeUncertainty => Uncertainty.RelativeUncertainty(KmsValue);
    public double UpperRelativeUncertainty => Uncertainty.UpperRelativeUncertainty(KmsValue);
    public double LowerRelativeUncertainty => Uncertainty.LowerRelativeUncertainty(KmsValue);

    public double AbsoluteUncertainty(UnitOfMeasure unitOfMeasure)
    {
        return Math.Abs(Quantity.In(unitOfMeasure)) * RelativeUncertainty;
    }

    public double KmsValue => Quantity.KmsValue;
    public double KmsUpperAbsoluteUncertainty => Uncertainty.UpperAbsoluteUncertainty(KmsValue);
    public double KmsLowerAbsoluteUncertainty => Uncertainty.LowerAbsoluteUncertainty(KmsValue);
    public double KmsAbsoluteUncertainty => Math.Max(KmsUpperAbsoluteUncertainty, KmsLowerAbsoluteUncertainty);

    public double this[Landmark lm] => lm switch
    {
        Landmark.LowerBound => KmsValue - KmsLowerAbsoluteUncertainty,
        Landmark.Nominal => KmsValue,
        Landmark.UpperBound => KmsValue + KmsUpperAbsoluteUncertainty,
        _ => throw new IndexOutOfRangeException()
    };

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
        return ! IsNaN() && IsFinite();
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

    public double AbsoluteUncertaintyIn(UnitOfMeasure unit)
    {
        return In(unit) * RelativeUncertainty;
    }

    public double TryAbsoluteUncertaintyIn(UnitOfMeasure unit)
    {
        return TryIn(unit) * RelativeUncertainty;
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
    /// on the operation as an <see cref="UncertaintyCorrelation"/>. The propagator is the numerical method for
    /// combining uncertainties at all, and is a property of the calculation. Swapping it therefore does not
    /// discard what the model says about correlation; both are passed through together.
    /// </remarks>
    private static IUncertaintyPropagator ResolveUncertaintyPropagator(IUncertaintyPropagator? supplied) =>
        supplied ?? ConservativeGaussianPropagator.Instance;

    public static Measurand Sum(
        UncertaintyCorrelation method,
        IUncertaintyPropagator? propagator,
        IEnumerable<Measurand> measurands) => Sum(method, propagator, measurands.ToArray());

    public static Measurand Sum(UncertaintyCorrelation method, IUncertaintyPropagator? propagator, params Measurand[] measurands)
    {
        if (measurands.Length == 0) return new Measurand();

        if (measurands.Any(q => q.Quantity.Dimensionality != measurands[0].Quantity.Dimensionality))
            throw new IncompatibleDimensionsException("Measurand summation of incompatibly dimensioned units");

        var kmsValue = measurands.Sum(q => q.Quantity.KmsValue);
        var quantity = new Quantity(kmsValue, measurands[0].Quantity.Dimensionality);
        return new Measurand(quantity, ResolveUncertaintyPropagator(propagator).PropagateThroughSum(method, measurands));
    }

    public static Measurand Product(UncertaintyCorrelation method, IUncertaintyPropagator? propagator, params Measurand[] quantities)
    {
        if (quantities.Length == 0) return new Measurand();

        var product = quantities.Select(q => q.Quantity).Aggregate(
            Quantity.One,
            (prod, q) => prod * q);

        return new Measurand(product, ResolveUncertaintyPropagator(propagator).PropagateThroughProduct(method, quantities));
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
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        var quantity = Quantity.TryAdd(other.Quantity);
        var uncertainty = quantity.IsNaN()
            ? SymmetricUncertainty.FromRelative(0)
            : ResolveUncertaintyPropagator(propagator).PropagateThroughSum(method, [this, other]);

        return new Measurand(quantity, uncertainty);
    }

    public Measurand TrySubtract(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        var quantity = Quantity.TrySubtract(other.Quantity);
        var uncertainty = quantity.IsNaN()
            ? SymmetricUncertainty.FromRelative(0)
            : ResolveUncertaintyPropagator(propagator).PropagateThroughSum(method, [this, -other]);

        return new Measurand(quantity, uncertainty);
    }

    public Measurand Plus(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        return Sum(method, propagator, this, other);
    }

    public Measurand Minus(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        return Sum(method, propagator, this, -other);
    }

    public Measurand Times(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        return Product(method, propagator, this, other);
    }

    public Measurand DividedBy(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        return Product(method, propagator, this, other.Reciprocal());
    }

    /// <inheritdoc/>
    public MeasurandSnapshot GetSnapshot() => new(Quantity.GetSnapshot(), Uncertainty.GetSnapshot());

    /// <inheritdoc/>
    public static Measurand FromSnapshot(MeasurandSnapshot state) =>
        new(Quantity.FromSnapshot(state.Quantity), UncertaintyFactory.FromSnapshot(state.Uncertainty));
}