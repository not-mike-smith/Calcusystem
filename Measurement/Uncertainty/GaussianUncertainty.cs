using Measurement.Interfaces;

namespace Measurement.Uncertainty;

/// <summary>
/// Symmetric relative uncertainty, equivalent to the original single-value model.
/// Propagates via RSS (uncorrelated) or direct sum (correlated) through arithmetic operations.
/// </summary>
public sealed class GaussianUncertainty : ISymmetricUncertainty
{
    private readonly double _relativeError;

    private GaussianUncertainty(double relativeError)
    {
        if (double.IsNegative(relativeError) || double.IsInfinity(relativeError) || double.IsNaN(relativeError))
            throw new ArgumentException("Relative error cannot be negative, infinite, or NaN.", nameof(relativeError));

        _relativeError = relativeError;
    }

    public double RelativeError(double nominalKmsValue) => _relativeError;

    public double AbsoluteError(double nominalKmsValue) => _relativeError * Math.Abs(nominalKmsValue);

    private static GaussianUncertainty FromAbsoluteError(Quantity value, Quantity absoluteError)
    {
        if (value.Dimensionality != absoluteError.Dimensionality)
            throw new ArgumentException("Value and absolute error must have the same dimensionality.");

        double relativeError = Math.Abs(absoluteError.KmsValue) / Math.Abs(value.KmsValue);
        return new GaussianUncertainty(relativeError);
    }

    public static GaussianUncertainty FromRelErr(double relativeError)
    {
        return new GaussianUncertainty(relativeError);
    }

    public static UncertaintyFromNominalValue FromAbsErr(Quantity absoluteError)
    {
        GaussianUncertainty func(Quantity value)
        {
            return FromAbsoluteError(value, absoluteError);
        }

        return func;
    }

    public IUncertainty Reciprocal(double nominalKmsValue)
    {
        return this;
    }

    public IUncertainty Negated(double nominalKmsValue)
    {
        return this;
    }
}
