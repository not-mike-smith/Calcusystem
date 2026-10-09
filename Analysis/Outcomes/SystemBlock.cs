using Calcusystem.Analysis.Enums;
using Calcusystem.DimensionedExpression.Expressions;

namespace Calcusystem.Analysis.Outcomes;

/// <summary>
/// One part of a decomposed system: a set of unknowns together with the equations that reach them, and nothing
/// outside it.
/// </summary>
/// <param name="Unknowns">The unknowns in this part.</param>
/// <param name="Equations">The equations incident only on unknowns in this part.</param>
public sealed record SystemBlock(IReadOnlyList<Variable> Unknowns, IReadOnlyList<Equation> Equations)
{
    /// <summary>A block with nothing in it — what a part that does not arise is reported as, never null.</summary>
    public static readonly SystemBlock Empty = new([], []);

    /// <summary>Unknowns minus equations, within this part alone.</summary>
    /// <remarks>
    /// A plain subtraction, unlike <see cref="FlatSystem.DegreesOfFreedom"/>, which first excludes the equations
    /// that reach no unknown. Those never enter a block, so there is nothing here to exclude.
    /// </remarks>
    public int DegreesOfFreedom => Unknowns.Count - Equations.Count;

    /// <summary>How this part's counts stand — the same classification as for a whole system, read locally.</summary>
    public Determination Determination => DegreesOfFreedom switch
    {
        > 0 => Determination.Underdetermined,
        0 => Determination.ExactlyDetermined,
        _ => Determination.Overdetermined,
    };

    /// <summary>Whether this part holds nothing.</summary>
    public bool IsEmpty => Unknowns.Count == 0 && Equations.Count == 0;

    public override string ToString() =>
        $"{Unknowns.Count} unknown(s), {Equations.Count} equation(s): {Determination}";
}
