namespace Measurement.Interfaces;

/// <summary>
/// Strategy for propagating measurement uncertainty through arithmetic on <see cref="Measurand"/>s.
/// Each method takes the operands and returns the uncertainty of the result. Implementations decide
/// how errors combine (e.g. root-sum-of-squares vs. direct sum) and what concrete
/// <see cref="IUncertainty"/> type the result carries.
/// </summary>
/// <remarks>
/// The propagator is the injection seam for alternative uncertainty models (Monte Carlo, correlation-aware,
/// etc.). The only implementation today is <c>ConservativeGaussianPropagator</c>, which always returns a
/// symmetric <c>GaussianUncertainty</c> derived from each operand's conservative error.
/// </remarks>
public interface IErrorPropagator
{
    /// <summary>
    /// Propagates uncertainty through raising <paramref name="measurand"/> to the rational power
    /// <paramref name="exponentNumerator"/>/<paramref name="exponentDenominator"/>. Integer powers pass a
    /// denominator of 1; roots pass a numerator of 1.
    /// </summary>
    /// <param name="measurand">The operand being raised to a power.</param>
    /// <param name="exponentNumerator">Numerator of the rational exponent.</param>
    /// <param name="exponentDenominator">Denominator of the rational exponent.</param>
    /// <returns>The uncertainty of the exponentiated result.</returns>
    IUncertainty PropagateErrorThroughExponentiation(
        Measurand measurand,
        int exponentNumerator,
        int exponentDenominator);

    /// <summary>
    /// Propagates uncertainty through the product (and quotient, via reciprocal operands) of the operands.
    /// </summary>
    /// <param name="method">Whether the operands' errors are treated as correlated or uncorrelated.</param>
    /// <param name="measurands">The factors whose product's uncertainty is being computed.</param>
    /// <returns>The uncertainty of the product.</returns>
    IUncertainty PropagateErrorThroughProduct(ErrorPropagationMethod method, params Measurand[] measurands);

    /// <summary>
    /// Propagates uncertainty through the sum (and difference, via negated operands) of the operands.
    /// </summary>
    /// <param name="method">Whether the operands' errors are treated as correlated or uncorrelated.</param>
    /// <param name="measurands">The addends whose sum's uncertainty is being computed.</param>
    /// <returns>The uncertainty of the sum.</returns>
    IUncertainty PropagateErrorThroughSum(ErrorPropagationMethod method, params Measurand[] measurands);
}
