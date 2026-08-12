namespace Calcusystem.Measurement;

// TODO: rename to `ErrorCorrelation`. This enum does not name a method of propagating error — it states
// whether two operands' errors are correlated, which is a fact about the quantities being combined. The method
// is `IErrorPropagator`, and the two now sit side by side in every arithmetic signature, where the current name
// reads as though one were a variant of the other.
//
// Rename touches: this type, `IComputedExpression.ErrorPropagation` and its `ComputedExpressionBase` backing,
// the `ErrorPropagation` fields on `NaryExpressionState`/`BinaryExpressionState`, the matching DTO properties
// in `Serialization/Dtos/Expression.cs`, and the `method` parameters on `Measurand`/`IErrorPropagator`. The DTO
// property name is on the wire, so this breaks stored payloads — acceptable while there is no corpus, but it is
// the part to notice.
/// <summary>
/// Whether two operands' errors are treated as moving together or independently when their uncertainties are
/// combined.
/// </summary>
/// <remarks>
/// A statement about the model, not about arithmetic: it records something known about where the quantities came
/// from — two readings off one instrument share its calibration error, two independent instruments do not.
/// Distinct from <see cref="Interfaces.IErrorPropagator"/>, which is the numerical method for combining
/// uncertainties and belongs to a calculation rather than to the model.
/// </remarks>
public enum ErrorPropagationMethod : byte
{
    /// <summary>Errors are independent; they combine in quadrature.</summary>
    Uncorrelated = 0,

    /// <summary>Errors move together; they combine directly, giving the more conservative result.</summary>
    Correlated = 1
}
