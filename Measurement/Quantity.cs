using System;
using Measurement.Exceptions;
using Measurement.Interfaces;
using Measurement.State;

namespace Measurement;

/// <summary>
/// A dimensioned scalar without uncertainty: a KMS-normalized value paired with its
/// <see cref="Dimensionality"/>. This is the internal "currency" of the library — the raw numeric
/// carrier that <see cref="Measurand"/> wraps once uncertainty is attached. Arithmetic operators enforce or
/// combine dimensions; the value is always stored in SI base (kg-m-s) units.
/// </summary>
/// <remarks>
/// A <c>readonly</c> value type whose <c>default</c> is a dimensionless <see cref="double.NaN"/>. Construct
/// either from a user value plus a <see cref="UnitOfMeasure"/> (which converts to KMS), or directly from a
/// raw KMS value plus a <see cref="Dimensionality"/>.
/// </remarks>
public readonly struct Quantity : IStateful<Quantity, QuantityState>
{
    private readonly double? _value;
    private double Value => _value ?? double.NaN;

    /// <summary>The physical dimension of this quantity.</summary>
    public readonly Dimensionality Dimensionality;
    internal double KmsValue => Value;

    internal static Quantity One => new Quantity(1, Dimensionality.Dimensionless);

    /// <summary>
    /// Creates a quantity from a value expressed in <paramref name="unitOfMeasure"/>, converting it to KMS and
    /// adopting the unit's dimensionality.
    /// </summary>
    public Quantity(double value, UnitOfMeasure unitOfMeasure)
    {
        _value = unitOfMeasure.ConvertToKmsValue(value);
        Dimensionality = unitOfMeasure.Dimensionality;
    }

    /// <summary>
    /// Creates a quantity directly from an already-KMS-normalized value and an explicit dimensionality.
    /// </summary>
    public Quantity(double kmsValue, Dimensionality dimensionality)
    {
        _value = kmsValue;
        Dimensionality = dimensionality;
    }

    /// <summary>Attaches the given uncertainty to this value, producing a <see cref="Measurand"/>.</summary>
    public Measurand Measurand(IUncertainty uncertainty)
    {
        return new Measurand(this, uncertainty);
    }

    /// <summary>
    /// Returns this quantity's value expressed in <paramref name="unitOfMeasure"/>.
    /// </summary>
    /// <exception cref="IncompatibleDimensionsException">The unit's dimensionality does not match this quantity's.</exception>
    public double In(UnitOfMeasure unitOfMeasure)
    {
        if (Dimensionality != unitOfMeasure.Dimensionality)
        {
            throw new IncompatibleDimensionsException(
                $"Cannot express {Dimensionality} value in {unitOfMeasure.Symbol}");
        }

        return unitOfMeasure.ConvertFromKmsValue(Value);
    }

    /// <summary>
    /// Like <see cref="In"/>, but returns <see cref="double.NaN"/> instead of throwing when the unit's
    /// dimensionality does not match.
    /// </summary>
    public double TryIn(UnitOfMeasure unitOfMeasure)
    {
        return Dimensionality == unitOfMeasure.Dimensionality
            ? unitOfMeasure.ConvertFromKmsValue(Value)
            : double.NaN;
    }

    /// <summary>Whether the KMS value is negative (see <see cref="double.IsNegative"/>).</summary>
    public bool IsNegative()
    {
        return double.IsNegative(Value);
    }

    /// <summary>Whether the KMS value is NaN — also the state of a <c>default</c> quantity.</summary>
    public bool IsNaN()
    {
        return double.IsNaN(Value);
    }

    /// <summary>Whether the KMS value is positive or negative infinity.</summary>
    public bool IsInfinity()
    {
        return double.IsInfinity(Value);
    }

    /// <summary>Whether the KMS value is positive infinity.</summary>
    public bool IsPositiveInfinity()
    {
        return double.IsPositiveInfinity(Value);
    }

    /// <summary>Whether the KMS value is negative infinity.</summary>
    public bool IsNegativeInfinity()
    {
        return double.IsNegativeInfinity(Value);
    }

    /// <summary>Whether the KMS value is finite (not NaN or infinite).</summary>
    public bool IsFinite()
    {
        return double.IsFinite(Value);
    }

    /// <summary>Whether the KMS value is a normal floating-point number (see <see cref="double.IsNormal"/>).</summary>
    public bool IsNormal()
    {
        return double.IsNormal(Value);
    }

    /// <summary>Whether the KMS value is subnormal (see <see cref="double.IsSubnormal"/>).</summary>
    public bool IsSubnormal()
    {
        return double.IsSubnormal(Value);
    }

    /// <summary>Debug-oriented rendering: the KMS value in scientific notation followed by its dimensionality.</summary>
    public override string ToString()
    {
        return $"{Value:E4} {Dimensionality}"; // try to get fundamental unit later
    }

    /// <summary>Adds two quantities of matching dimensionality.</summary>
    /// <exception cref="IncompatibleDimensionsException">The dimensionalities differ.</exception>
    public static Quantity operator +(Quantity lhs, Quantity rhs)
    {
        if (lhs.Dimensionality != rhs.Dimensionality)
        {
            throw new IncompatibleDimensionsException(
                $"Cannot add {lhs.Dimensionality} quantity and {rhs.Dimensionality} quantity");
        }

        return new Quantity(lhs.Value + rhs.Value, lhs.Dimensionality);
    }

    /// <summary>Negates the value, preserving dimensionality.</summary>
    public static Quantity operator -(Quantity q)
    {
        return new Quantity(-q.Value, q.Dimensionality);
    }

    /// <summary>Subtracts <paramref name="rhs"/> from <paramref name="lhs"/>; both must share a dimensionality.</summary>
    /// <exception cref="IncompatibleDimensionsException">The dimensionalities differ.</exception>
    public static Quantity operator -(Quantity lhs, Quantity rhs)
    {
        if (lhs.Dimensionality != rhs.Dimensionality)
        {
            throw new IncompatibleDimensionsException(
                $"Cannot subtract {rhs.Dimensionality} quantity from {lhs.Dimensionality} quantity");
        }

        return new Quantity(lhs.Value - rhs.Value, lhs.Dimensionality);
    }

    /// <summary>Multiplies two quantities, multiplying both their values and their dimensionalities.</summary>
    public static Quantity operator *(Quantity lhs, Quantity rhs)
    {
        return new Quantity(lhs.Value * rhs.Value, lhs.Dimensionality * rhs.Dimensionality);
    }

    /// <summary>Divides two quantities, dividing both their values and their dimensionalities.</summary>
    public static Quantity operator /(Quantity lhs, Quantity rhs)
    {
        return new Quantity(lhs.Value / rhs.Value, lhs.Dimensionality / rhs.Dimensionality);
    }

    /// <summary>Explicitly treats a bare <see cref="double"/> as a dimensionless quantity.</summary>
    public static explicit operator Quantity(double d)
    {
        return new Quantity(d, Dimensionality.Dimensionless);
    }

    /// <summary>Raises the quantity to an integer power, scaling both the value and the dimensionality.</summary>
    public Quantity ToPower(int exponent)
    {
        return new Quantity(Math.Pow(Value, exponent), Dimensionality * exponent);
    }

    /// <summary>
    /// Takes the integer <paramref name="root"/> of the quantity, taking the root of the value and dividing the
    /// dimensionality's exponents.
    /// </summary>
    /// <exception cref="NondiscreteDimensionalityException">
    /// A dimensionality exponent does not divide evenly by <paramref name="root"/>. See
    /// <see cref="Dimensionality.op_Division(Dimensionality, int)"/>.
    /// </exception>
    public Quantity ToRoot(int root)
    {
        return new Quantity(Math.Pow(Value, 1d / root), Dimensionality / root);
    }

    /// <summary>
    /// Adds <paramref name="other"/> to this quantity, returning a NaN-valued quantity (rather than throwing)
    /// when the dimensionalities do not match.
    /// </summary>
    public Quantity TryAdd(Quantity other)
    {
        var value = Dimensionality == other.Dimensionality
            ? Value + other.Value
            : double.NaN;

        return new Quantity(value, Dimensionality);
    }

    /// <summary>
    /// Subtracts <paramref name="other"/> from this quantity, returning a NaN-valued quantity (rather than
    /// throwing) when the dimensionalities do not match.
    /// </summary>
    public Quantity TrySubtract(Quantity other)
    {
        var value = Dimensionality == other.Dimensionality
            ? Value - other.Value
            : double.NaN;

        return new Quantity(value, Dimensionality);
    }

    /// <inheritdoc/>
    /// <remarks>Implemented publicly, unlike the <see cref="IUncertainty"/> seam: a quantity's state is its value
    /// and its dimension, both of which are already public concepts here. Nothing is being hidden to protect.</remarks>
    public QuantityState GetState() => new(KmsValue, Dimensionality);

    /// <inheritdoc/>
    public static Quantity FromState(QuantityState state) => new(state.KmsValue, state.Dimensionality);
}
