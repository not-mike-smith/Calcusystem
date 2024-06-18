using Measurement.BaseClasses;
using Measurement.Exceptions;
using Measurement.Models;

namespace Measurement;

public class Magnitude : PrecisionQuantity
{
    protected override string ToStringSuffix { get; } = "(+)";

    public Magnitude(double value, UnitOfMeasure unitOfMeasure, double relativeError = 0)
        : base(value, unitOfMeasure, relativeError)
    {
        if (double.IsNegative(value)) throw new NegativeMagnitudeException("Magnitude cannot be negative");
    }

    public Magnitude(Quantity quantity, double relativeError = 0)
        : base(quantity, relativeError)
    {
        if (quantity.IsNegative()) throw new NegativeMagnitudeException("Magnitude cannot be negative");
    }

    public Magnitude ToPower(int exponent)
    {
        return new Magnitude(Quantity.ToPower(exponent), RelativeError * exponent);
    }

    public Magnitude ToRoot(int root)
    {
        return new Magnitude(Quantity.ToRoot(root), RelativeError / root);
    }

    public Magnitude TryAdd(Magnitude other)
    {
        return new Magnitude(Quantity.TryAdd(other.Quantity));
    }

    public Delta TryAdd(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Delta(
            Quantity.TryAdd(other.Quantity),
            PropagateRelativeErrorThroughSum(method, this, other));
    }

    public Delta TrySubtract(Magnitude other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Delta(
            Quantity.TrySubtract(other.Quantity),
            PropagateRelativeErrorThroughSum(method, this, other));
    }

    public Magnitude TrySubtract(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        var quantity = Quantity.TrySubtract(other.Quantity);
        if (quantity.IsNegative())
        {
            quantity = new Quantity(0, quantity.Dimensionality);
        }

        return new Magnitude(quantity, PropagateRelativeErrorThroughSum(method, this, other));
    }

    public Magnitude Plus(Magnitude other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Magnitude(
            Quantity + other.Quantity,
            PropagateRelativeErrorThroughSum(method, this, other));
    }

    public Delta Plus(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Sum(method, this, other);
    }

    public Delta Minus(Magnitude other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Sum(method, this, -other);
    }

    public Magnitude Minus(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Magnitude(
            Quantity - other.Quantity,
            PropagateRelativeErrorThroughSum(method, this, other));
    }

    public Magnitude Times(Magnitude other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Magnitude(
            Quantity * other.Quantity,
            PropagateRelativeErrorThroughProduct(method, this, other));
    }

    public Delta Times(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Product(method, this, other);
    }

    public Magnitude DividedBy(Magnitude other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return new Magnitude(
            Quantity / other.Quantity,
            PropagateRelativeErrorThroughProduct(method, this, other));
    }

    public Delta DividedBy(Delta other, ErrorPropagationMethod method = ErrorPropagationMethod.Uncorrelated)
    {
        return Quotient(method, this, other);
    }

    public static implicit operator Delta(Magnitude x)
    {
        return new Delta(x.Quantity);
    }

    public static explicit operator Magnitude(Delta x)
    {
        return new Magnitude(x.Quantity);
    }

    public static Delta operator -(Magnitude x)
    {
        return new Delta(-x.Quantity);
    }
}
