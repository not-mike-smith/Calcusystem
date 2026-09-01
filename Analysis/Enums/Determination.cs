namespace Calcusystem.Analysis.Enums;

/// <summary>
/// How a system's equation count stands against its unknown count — the gate for what can be done with it.
/// </summary>
public enum Determination
{
    /// <summary>
    /// More unknowns than equations. Values are missing; the system cannot be solved until more are supplied.
    /// <c>FlatSystem.Unknowns</c> names what is outstanding.
    /// </summary>
    Underdetermined,

    /// <summary>
    /// As many equations as unknowns. A necessary condition for solvability, and the gate for evaluation and
    /// solving — but <b>not sufficient</b>, since the equations are counted, not checked for independence. See
    /// the remarks on <see cref="FlatSystem.DegreesOfFreedom"/>.
    /// </summary>
    ExactlyDetermined,

    /// <summary>
    /// More equations than unknowns. Redundancy, which is a finding rather than an error: the extra equations
    /// may agree, in which case they corroborate, or disagree, in which case the model or the measurements are
    /// inconsistent and that is worth knowing. Pin different subsets through the bindings argument and compare.
    /// </summary>
    Overdetermined,
}
