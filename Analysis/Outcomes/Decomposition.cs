using Calcusystem.DimensionedExpression.Expressions;

namespace Calcusystem.Analysis.Outcomes;

/// <summary>
/// A flat system split into the parts that can be solved and the part that cannot, by matching equations to
/// unknowns rather than counting them.
/// </summary>
/// <remarks>
/// <para>
/// Reach for this where <see cref="FlatSystem.Determination"/> is not enough. A system it calls
/// <c>ExactlyDetermined</c> can still hold an unknown nothing reaches, offset by a surplus equation elsewhere;
/// this names both.
/// </para>
/// <para>
/// Equations reaching no unknown take no part, so they appear in none of the three parts. They stay reported as
/// <see cref="FlatSystem.RedundantEquations"/>.
/// </para>
/// </remarks>
/// <param name="Underdetermined">
/// Unknowns with no equation available to them, and the equations reaching only those — more unknowns than
/// equations. Nothing here is solvable until more values are supplied.
/// </param>
/// <param name="WellDetermined">
/// Unknowns exactly matched by equations — as many of one as of the other, with every unknown reachable.
/// </param>
/// <param name="Overdetermined">
/// Unknowns with more equations reaching them than there are unknowns to determine. Every unknown here has an
/// equation available; the surplus equations are checks on the result rather than a shortfall.
/// </param>
public sealed record Decomposition(
    SystemBlock Underdetermined,
    SystemBlock WellDetermined,
    SystemBlock Overdetermined)
{
    /// <summary>
    /// True when every unknown has an equation available and every equation an unknown to determine.
    /// </summary>
    /// <remarks>
    /// Stronger than <c>Determination.ExactlyDetermined</c>: that says the two counts agree, this says the
    /// agreement is not two opposite failures cancelling.
    /// </remarks>
    public bool IsWellPosed => Underdetermined.IsEmpty && Overdetermined.IsEmpty;

    /// <summary>
    /// Every unknown an equation is available for — the whole system's unknowns apart from the underdetermined
    /// part's.
    /// </summary>
    /// <remarks>
    /// Structural only: an equation has been assigned to each of these, which is not a claim that it can be
    /// inverted for it or that doing so is numerically sound.
    /// </remarks>
    public IEnumerable<Variable> DeterminableUnknowns =>
        Overdetermined.Unknowns.Concat(WellDetermined.Unknowns);

    public override string ToString() =>
        $"under: [{Underdetermined}], well: [{WellDetermined}], over: [{Overdetermined}]";
}
