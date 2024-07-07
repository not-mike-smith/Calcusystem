using Measurement.BaseClasses;
using Measurement.Models;

namespace Measurement;

public class Delta : PrecisionQuantity
{
    protected override string ToStringSuffix { get; } = "(Δ)";

    public Delta(double value, UnitOfMeasure unitOfMeasure, double relativeError = 0)
        : base(value, unitOfMeasure, relativeError)
    { }

    public Delta(Quantity quantity, double relativeError = 0)
        : base(quantity, relativeError)
    { }

    public override double In(UnitOfMeasure unitOfMeasure)
    {
        if (unitOfMeasure is OffsetUnitOfMeasure offsetUnit)
        {
            return base.In(offsetUnit.DeltaUnit);
        }

        return base.In(unitOfMeasure);
    }

    public override double TryIn(UnitOfMeasure unitOfMeasure)
    {
        if (unitOfMeasure is OffsetUnitOfMeasure offsetUnit)
        {
            return base.TryIn(offsetUnit.DeltaUnit);
        }

        return base.TryIn(unitOfMeasure);
    }

    public Delta ToPower(int exponent)
    {
        return new Delta(Quantity.ToPower(exponent), exponent * RelativeError);
    }

    public Delta ToRoot(int root)
    {
        return new Delta(Quantity.ToRoot(root), RelativeError / root);
    }

    public Delta TryAdd(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Delta(
            Quantity.TryAdd(other.Quantity),
            PropagateRelativeErrorThroughSum(method, this, other));
    }

    public Delta TrySubtract(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Delta(
            Quantity.TrySubtract(other.Quantity),
            PropagateRelativeErrorThroughSum(method, this, other));
    }

    public Delta Plus(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Sum(method, this, other);
    }

    public Delta Minus(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Sum(method, this, -other);
    }

    public Delta Times(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Product(method, this, other);
    }

    public Delta DividedBy(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Quotient(method, this, other);
    }

    public static Delta operator -(Delta x)
    {
        return new Delta(-x.Quantity);
    }

    internal static Delta? Deserialize(Dimensionality dimensionality, double? kmsValue, double? relativeError)
    {
        if (kmsValue.HasValue is false)
        {
            return null;
        }

        var quantity = new Quantity(kmsValue.Value, dimensionality);
        return relativeError.HasValue
            ? new Delta(quantity, relativeError.Value)
            : new Delta(quantity);
    }
}
