namespace Measurement.Interfaces;

public interface IErrorPropagator
{
    IUncertainty PropagateErrorThroughExponentiation(
        Measurand baseQuantity,
        int exponentNumerator,
        int exponentDenominator);

    IUncertainty PropagateErrorThroughProduct(ErrorPropagationMethod method, params Measurand[] measurands);
    IUncertainty PropagateErrorThroughSum(ErrorPropagationMethod method, params Measurand[] quantities);
}