using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;

namespace Calcusystem.Analysis;

/// <summary>
/// One determining relationship, paired with the unknowns it is incident on.
/// </summary>
/// <remarks>
/// The incident set comes from walking both sides of the relationship, so a computed node between the operator
/// and a leaf contributes nothing of its own — it is the path by which the equation reaches that leaf. This is
/// the row of the incidence matrix the structural analysis in Milestone 4 will match over.
/// </remarks>
/// <param name="Relationship">The determining operator this row stands for.</param>
/// <param name="Unknowns">The distinct unknowns reachable from either side.</param>
public sealed record Equation(IBinaryOperator Relationship, IReadOnlyList<Variable> Unknowns);

/// <summary>
/// A system reduced to the only two things degrees of freedom depends on: the unknowns to solve for and the
/// equations available to solve them with.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately a <i>flat</i> form rather than a walk over the system's object structure, because the
/// two stop agreeing as soon as systems compose. Connecting sub-systems maps their variables onto each other, so
/// an identity connection merges two unknowns while adding no equation — meaning composed degrees of freedom is
/// not the sum of its parts. Flattening first and analysing once keeps a forty-stage column identical in
/// treatment to a single stage.
/// </para>
/// <para>
/// Only a <see cref="Variable"/> is ever an unknown. A computed node is determined the moment its leaves are, so
/// admitting one would add a column and force a compensating row, changing nothing but the size of the problem.
/// </para>
/// </remarks>
/// <param name="Unknowns">The distinct unbound variables the system must resolve.</param>
/// <param name="Equations">The determining relationships available to resolve them.</param>
public sealed record FlatSystem(IReadOnlyList<Variable> Unknowns, IReadOnlyList<Equation> Equations)
{
    /// <summary>
    /// Unknowns minus equations: positive when values are missing, zero when the system is square, negative when
    /// it carries redundancy.
    /// </summary>
    /// <remarks>
    /// <b>This counts equations; it does not check that they are independent.</b> Zero is therefore a necessary
    /// but not sufficient condition for solvability — two equations asserting the same thing alongside a
    /// genuinely free variable also lands on zero, and no count can tell the difference. Distinguishing them
    /// needs a matching over <see cref="Equation.Unknowns"/>, which is the Milestone 4 structural analysis.
    /// Treat this as a gate that can reject, not as a promise that solving will succeed.
    /// </remarks>
    public int DegreesOfFreedom => Unknowns.Count - Equations.Count;

    /// <summary>How <see cref="DegreesOfFreedom"/> classifies this system.</summary>
    public Determination Determination => DegreesOfFreedom switch
    {
        > 0 => Determination.Underdetermined,
        0 => Determination.ExactlyDetermined,
        _ => Determination.Overdetermined,
    };

    /// <summary>
    /// Unknowns that no equation is incident on — referenced by the system, but with nothing in it able to
    /// determine them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not the same as "unconstrained": a variable may well carry tolerance or ordering constraints and still
    /// appear here, because a constraint bounds a value rather than producing one. <c>l &lt; 3 m</c> tells a
    /// solver nothing about what <c>l</c> is.
    /// </para>
    /// <para>
    /// A cheap slice of what a full structural analysis would report. These are guaranteed unsolvable however
    /// the rest of the system is arranged, so they are worth surfacing separately from the aggregate count: a
    /// system can be square overall and still contain one of these, paired with a redundancy elsewhere.
    /// </para>
    /// </remarks>
    public IEnumerable<Variable> UnknownsWithNoEquation
    {
        get
        {
            var incident = Equations.SelectMany(e => e.Unknowns).ToHashSet();
            return Unknowns.Where(u => ! incident.Contains(u));
        }
    }
}
