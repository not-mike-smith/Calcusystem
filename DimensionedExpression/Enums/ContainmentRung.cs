
namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>How much of the subject is inside the criterion's tolerance band.</summary>
/// <remarks>
/// <b>A lattice, not a chain.</b> A value's upper and lower bounds are independently checkable, so
/// <see cref="NominalAndUpperWithin"/> and <see cref="NominalAndLowerWithin"/> are incomparable — either can hold
/// without the other. That is why there is no "achieved rung" here as there is for ordering: asking for one would
/// force a precedence between "cannot overshoot" and "cannot undershoot", which are different engineering
/// questions with no general answer.
/// </remarks>
public enum ContainmentRung : byte
{
    /// <summary>
    /// The two intervals share at least one point, so the values are not incompatible. Symmetric — at this rung
    /// the asymmetry between subject and band genuinely vanishes. Non-strict: intervals that merely touch overlap.
    /// </summary>
    Overlaps = 1,

    /// <summary>The subject's reported value falls inside the band, with its own uncertainty set aside.</summary>
    NominalWithin = 2,

    /// <summary>…and it cannot overshoot the band's ceiling even at its worst case. The rung for a maximum rating.</summary>
    NominalAndUpperWithin = 3,

    /// <summary>…and it cannot undershoot the band's floor even at its worst case. The rung for a minimum rating.</summary>
    NominalAndLowerWithin = 4,

    /// <summary>
    /// The whole of the subject's interval lies strictly inside the band, so no part of its uncertainty reaches
    /// the band's edge.
    /// </summary>
    WhollyWithin = 5,
}
