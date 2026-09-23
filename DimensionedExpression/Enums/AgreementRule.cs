using Calcusystem.DimensionedExpression.BinaryOperators;

namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>
/// How strictly an <see cref="EqualityOperator"/> reads "equal" — which of the nested agreements between two
/// uncertain values it demands.
/// </summary>
/// <remarks>
/// Ordered from strictest to loosest, each implying the one after it. <see cref="Nominal"/> is the reading most
/// equations want. Zero is left unassigned, so a default-constructed value is not silently one of them.
/// </remarks>
public enum AgreementRule : byte
{
    /// <summary>
    /// The reported values agree, with each side's uncertainty interval set aside. Uncertainty still supplies
    /// the scale at which two nearby values count as the same number — see <c>MeasurandComparer</c>.
    /// <br/><br/>
    /// Symbol: <b>·==·</b>
    /// </summary>
    Nominal = 1,

    /// <summary>
    /// Each side's reported value falls inside the other's uncertainty band. The usual reading of "these two
    /// independent measurements agree", and symmetric by construction.
    /// <br/><br/>
    /// Symbol:  <b>{·==·}</b>
    /// </summary>
    Mutual = 2,

    /// <summary>
    /// The two uncertainty bands share at least one point — there is some value consistent with both. The
    /// weakest agreement worth asserting.
    /// <br/><br/>
    /// Symbol: <b>{&gt;=&lt;}</b>
    /// </summary>
    Overlapping = 3,
}
