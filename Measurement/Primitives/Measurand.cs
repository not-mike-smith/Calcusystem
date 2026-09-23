using Calcusystem.Core.Interfaces;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Exceptions;
using Calcusystem.Measurement.Factories;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Snapshots;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Measurement.Primitives;

/// <summary>
/// A physical value and the uncertainty around it: the pairing the rest of the library computes with.
/// </summary>
/// <remarks>
/// Immutable. Every operation returns a new instance, so a measurand held by one caller cannot be changed by
/// another. Values are stored KMS-normalized; units apply only when supplying or reading a value.
/// </remarks>
public class Measurand : ISnapshotting<Measurand, MeasurandSnapshot>
{
    internal readonly Quantity Quantity;

    /// <summary>The uncertainty around the value, preserved through negation and <see cref="Reciprocal"/>.</summary>
    public readonly IUncertainty Uncertainty;

    /// <summary>The multiplicative identity: exactly one, dimensionless, with no uncertainty.</summary>
    /// <remarks>One rather than zero, so that an empty <see cref="Product"/> returns something usable.</remarks>
    public Measurand()
    {
        Quantity = Quantity.One;
        Uncertainty = SymmetricUncertainty.FromRelative(0);
    }

    /// <summary>Pairs a quantity with an uncertainty.</summary>
    public Measurand(Quantity quantity, IUncertainty uncertainty)
    {
        Quantity = quantity;
        Uncertainty = uncertainty;
    }

    /// <summary>The physical dimension of the value.</summary>
    public Dimensionality Dimensionality => Quantity.Dimensionality;

    /// <summary>The larger of the two directional relative uncertainties — conservative, for propagation.</summary>
    public double RelativeUncertainty => Uncertainty.RelativeUncertainty(KmsValue);

    /// <summary>The relative uncertainty above the nominal value.</summary>
    public double UpperRelativeUncertainty => Uncertainty.UpperRelativeUncertainty(KmsValue);

    /// <summary>The relative uncertainty below the nominal value.</summary>
    public double LowerRelativeUncertainty => Uncertainty.LowerRelativeUncertainty(KmsValue);

    /// <summary>
    /// The absolute uncertainty expressed in <paramref name="unitOfMeasure"/>, always non-negative.
    /// </summary>
    /// <remarks>
    /// Differs from <see cref="AbsoluteUncertaintyIn"/> only in taking the magnitude of the value first, so
    /// this stays positive where that one follows the sign of a negative value.
    /// </remarks>
    public double AbsoluteUncertainty(UnitOfMeasure unitOfMeasure)
    {
        return Math.Abs(Quantity.In(unitOfMeasure)) * RelativeUncertainty;
    }

    /// <summary>The nominal value in SI base (kg-m-s) units.</summary>
    public double KmsValue => Quantity.KmsValue;

    /// <summary>The absolute uncertainty above the nominal value, in KMS units.</summary>
    public double KmsUpperAbsoluteUncertainty => Uncertainty.UpperAbsoluteUncertainty(KmsValue);

    /// <summary>The absolute uncertainty below the nominal value, in KMS units.</summary>
    public double KmsLowerAbsoluteUncertainty => Uncertainty.LowerAbsoluteUncertainty(KmsValue);

    /// <summary>The larger of the two directional uncertainties — conservative, for propagation.</summary>
    public double KmsAbsoluteUncertainty => Math.Max(KmsUpperAbsoluteUncertainty, KmsLowerAbsoluteUncertainty);

    /// <summary>The KMS value at one <see cref="Landmark"/> of the uncertainty band.</summary>
    /// <exception cref="IndexOutOfRangeException"><paramref name="lm"/> is not a declared landmark.</exception>
    public double this[Landmark lm] => lm switch
    {
        Landmark.LowerBound => KmsValue - KmsLowerAbsoluteUncertainty,
        Landmark.Nominal => KmsValue,
        Landmark.UpperBound => KmsValue + KmsUpperAbsoluteUncertainty,
        _ => throw new IndexOutOfRangeException()
    };

    /// <summary>The nominal value read in <paramref name="unitOfMeasure"/>.</summary>
    /// <exception cref="IncompatibleDimensionsException">The unit's dimensionality is not this value's.</exception>
    public double In(UnitOfMeasure unitOfMeasure)
    {
        return Quantity.In(unitOfMeasure);
    }

    /// <summary>
    /// As <see cref="In"/>, but returns <see cref="double.NaN"/> on a dimension mismatch instead of throwing.
    /// </summary>
    public double TryIn(UnitOfMeasure unitOfMeasure)
    {
        return Quantity.TryIn(unitOfMeasure);
    }

    /// <summary>Whether the value is neither NaN nor infinite.</summary>
    /// <remarks>
    /// Does <b>not</b> reject negatives. Whether a quantity may be negative is a modelling question, so it
    /// belongs to the caller or the expression layer rather than to the value.
    /// </remarks>
    public bool IsValid()
    {
        return ! IsNaN() && IsFinite();
    }

    /// <summary>Whether the nominal value is less than zero.</summary>
    public bool IsNegative()
    {
        return Quantity.IsNegative();
    }

    /// <summary>Whether the nominal value is NaN.</summary>
    public bool IsNaN()
    {
        return Quantity.IsNaN();
    }

    /// <summary>Whether the nominal value is infinite, in either direction.</summary>
    public bool IsInfinity()
    {
        return Quantity.IsInfinity();
    }

    /// <summary>Whether the nominal value is positive infinity.</summary>
    public bool IsPositiveInfinity()
    {
        return Quantity.IsPositiveInfinity();
    }

    /// <summary>Whether the nominal value is negative infinity.</summary>
    public bool IsNegativeInfinity()
    {
        return Quantity.IsNegativeInfinity();
    }

    /// <summary>Whether the nominal value is neither infinite nor NaN.</summary>
    public bool IsFinite()
    {
        return Quantity.IsFinite();
    }

    /// <summary>Whether the nominal value is a normal floating-point number.</summary>
    public bool IsNormal()
    {
        return Quantity.IsNormal();
    }

    /// <summary>Whether the nominal value is subnormal — non-zero but too small for full precision.</summary>
    public bool IsSubnormal()
    {
        return Quantity.IsSubnormal();
    }

    /// <summary>
    /// The absolute uncertainty expressed in <paramref name="unit"/>, following the sign of the value.
    /// </summary>
    /// <remarks>Use <see cref="AbsoluteUncertainty"/> for a magnitude that is always non-negative.</remarks>
    /// <exception cref="IncompatibleDimensionsException">The unit's dimensionality is not this value's.</exception>
    public double AbsoluteUncertaintyIn(UnitOfMeasure unit)
    {
        return In(unit) * RelativeUncertainty;
    }

    /// <summary>
    /// As <see cref="AbsoluteUncertaintyIn"/>, but returns <see cref="double.NaN"/> on a dimension mismatch.
    /// </summary>
    public double TryAbsoluteUncertaintyIn(UnitOfMeasure unit)
    {
        return TryIn(unit) * RelativeUncertainty;
    }

    /// <summary>The quantity and its uncertainty, e.g. <c>2 kg ± 1%</c>.</summary>
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

    /// <inheritdoc cref="Sum(UncertaintyCorrelation, IUncertaintyPropagator?, Measurand[])"/>
    public static Measurand Sum(
        UncertaintyCorrelation method,
        IUncertaintyPropagator? propagator,
        IEnumerable<Measurand> measurands) => Sum(method, propagator, measurands.ToArray());

    /// <summary>Adds every measurand, propagating uncertainty through the sum.</summary>
    /// <remarks>An empty set sums to the dimensionless identity rather than throwing.</remarks>
    /// <exception cref="IncompatibleDimensionsException">The addends do not share a dimensionality.</exception>
    public static Measurand Sum(UncertaintyCorrelation method, IUncertaintyPropagator? propagator, params Measurand[] measurands)
    {
        if (measurands.Length == 0) return new Measurand();

        if (measurands.Any(q => q.Quantity.Dimensionality != measurands[0].Quantity.Dimensionality))
            throw new IncompatibleDimensionsException("Measurand summation of incompatibly dimensioned units");

        var kmsValue = measurands.Sum(q => q.Quantity.KmsValue);
        var quantity = new Quantity(kmsValue, measurands[0].Quantity.Dimensionality);
        return new Measurand(quantity, ResolveUncertaintyPropagator(propagator).PropagateThroughSum(method, measurands));
    }

    /// <summary>Multiplies every measurand, combining dimensions and propagating uncertainty.</summary>
    /// <remarks>Dimensions need not match; they combine. An empty set returns the identity.</remarks>
    public static Measurand Product(UncertaintyCorrelation method, IUncertaintyPropagator? propagator, params Measurand[] quantities)
    {
        if (quantities.Length == 0) return new Measurand();

        var product = quantities.Select(q => q.Quantity).Aggregate(
            Quantity.One,
            (prod, q) => prod * q);

        return new Measurand(product, ResolveUncertaintyPropagator(propagator).PropagateThroughProduct(method, quantities));
    }

    /// <summary>Negates the value, preserving the uncertainty band about it.</summary>
    public static Measurand operator -(Measurand quantity)
    {
        return new Measurand(-quantity.Quantity, quantity.Uncertainty.Negated(quantity.KmsValue));
    }

    /// <summary>One divided by this value, inverting the dimensionality.</summary>
    public Measurand Reciprocal()
    {
        return new Measurand(Quantity.One / Quantity, Uncertainty.Reciprocal(KmsValue));
    }

    /// <summary>Raises the value to an integer power, multiplying each dimension exponent by it.</summary>
    public Measurand ToPower(int exponent)
    {
        return new Measurand(
            Quantity.ToPower(exponent),
            Uncertainty.Exponentiated(KmsValue, exponent, 1));
    }

    /// <summary>Takes an integer root of the value.</summary>
    /// <exception cref="NondiscreteDimensionalityException">
    /// A dimension exponent does not divide evenly by <paramref name="root"/>.
    /// </exception>
    public Measurand ToRoot(int root)
    {
        return new Measurand(
            Quantity.ToRoot(root),
            Uncertainty.Exponentiated(KmsValue, 1, root));
    }

    /// <summary>
    /// As <see cref="Plus"/>, but returns a NaN-valued measurand with zero uncertainty on a dimension
    /// mismatch instead of throwing.
    /// </summary>
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

    /// <summary>
    /// As <see cref="Minus"/>, but returns a NaN-valued measurand with zero uncertainty on a dimension
    /// mismatch instead of throwing.
    /// </summary>
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

    /// <summary>Adds <paramref name="other"/>, propagating uncertainty through the sum.</summary>
    /// <exception cref="IncompatibleDimensionsException">The two do not share a dimensionality.</exception>
    public Measurand Plus(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        return Sum(method, propagator, this, other);
    }

    /// <summary>Subtracts <paramref name="other"/>, propagating uncertainty through the difference.</summary>
    /// <exception cref="IncompatibleDimensionsException">The two do not share a dimensionality.</exception>
    public Measurand Minus(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        return Sum(method, propagator, this, -other);
    }

    /// <summary>Multiplies by <paramref name="other"/>, combining dimensions.</summary>
    public Measurand Times(
        Measurand other,
        UncertaintyCorrelation method = UncertaintyCorrelation.Uncorrelated,
        IUncertaintyPropagator? propagator = null)
    {
        return Product(method, propagator, this, other);
    }

    /// <summary>Divides by <paramref name="other"/>, combining dimensions.</summary>
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
    public static Measurand FromSnapshot(MeasurandSnapshot snapshot) =>
        new(Quantity.FromSnapshot(snapshot.Quantity), UncertaintyFactory.FromSnapshot(snapshot.Uncertainty));
}