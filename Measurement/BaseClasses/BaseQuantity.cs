using Measurement.Models;

namespace Measurement.BaseClasses;

public abstract class BaseQuantity
{
    protected internal readonly Quantity Quantity;
    protected abstract string ToStringSuffix { get; }

    protected BaseQuantity(double value, UnitOfMeasure unitOfMeasure)
    {
        Quantity = new Quantity(value, unitOfMeasure);
    }

    protected BaseQuantity(Quantity quantity)
    {
        Quantity = quantity;
    }

    public Dimensionality Dimensionality => Quantity.Dimensionality;

    public virtual double In(UnitOfMeasure unitOfMeasure)
    {
        return Quantity.In(unitOfMeasure);
    }

    public virtual double TryIn(UnitOfMeasure unitOfMeasure)
    {
        return Quantity.TryIn(unitOfMeasure);
    }

    public override string ToString()
    {
        return $"{Quantity}{ToStringSuffix}";
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
        return Quantity.IsNormal();
    }
}
