namespace Calcusystem.Measurement.Enums;

/// <summary>
/// Whether two operands' uncertainties are treated as moving together or independently when they are combined.
/// </summary>
/// <remarks>
/// A statement about the model, not about arithmetic: it records something known about where the quantities came
/// from — two readings off one instrument share its calibration uncertainty, two independent instruments do not.
/// Distinct from <see cref="Interfaces.IUncertaintyPropagator"/>, which is the numerical method for combining
/// uncertainties and belongs to a calculation rather than to the model.
/// </remarks>
public enum UncertaintyCorrelation : byte
{
    /// <summary>Uncertainties are independent; they combine in quadrature.</summary>
    Uncorrelated = 0,

    /// <summary>Uncertainties move together; they combine directly, giving the more conservative result.</summary>
    Correlated = 1
}
