namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>
/// How strictly an <see cref="EqualityOperator"/> reads "equal" — which of the nested agreements between two
/// uncertain values it demands.
/// </summary>
/// <remarks>
/// <para>
/// A value, not a strategy, and that is the whole point. Equality used to take an injected
/// <c>IEqualityEstimating</c>, which meant the wire format carried "this is an equality" and nothing about what
/// equality <i>meant</i>: the reader supplied the semantics, so two readers could reach opposite verdicts from
/// identical bytes. A strategy cannot be serialized; an enum can.
/// </para>
/// <para>
/// Ordered from strictest to loosest, and each implies the one after it. <see cref="Nominal"/> is the reading
/// most equations want — the two quantities are the same number, as far as the measurements can tell. The looser
/// two coincide with <c>MutuallyWithinToleranceOperator</c> and <c>AnyToleranceOverlapOperator</c>, which is
/// deliberate: those state the same condition as a requirement, while an equality can additionally be an
/// <see cref="SolvingRole.Equation"/> or a <see cref="SolvingRole.Coherence"/> check and so is the only place
/// the condition can carry a solver's weight.
/// </para>
/// <para>
/// Zero is left unassigned so a default-constructed value is not silently one of the readings.
/// </para>
/// </remarks>
public enum AgreementRule : byte
{
    /// <summary>
    /// The reported values agree, with each side's uncertainty interval set aside. Uncertainty still supplies
    /// the scale at which two nearby values count as the same number — see <c>MeasurandComparer</c>.
    /// </summary>
    Nominal = 1,

    /// <summary>
    /// Each side's reported value falls inside the other's uncertainty band. The usual reading of "these two
    /// independent measurements agree", and symmetric by construction.
    /// </summary>
    Mutual = 2,

    /// <summary>
    /// The two uncertainty bands share at least one point — there is some value consistent with both. The
    /// weakest agreement worth asserting.
    /// </summary>
    Overlapping = 3,
}
