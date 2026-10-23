using Calcusystem.Analysis.Enums;

namespace Calcusystem.Api.Reports;

/// <summary>The parts of a system that can be solved, and the count over the whole of it.</summary>
/// <param name="DegreesOfFreedom">Unknowns less the equations that reach one, over the whole system.</param>
/// <param name="Determination">What that count says.</param>
/// <param name="IsWellPosed">
/// Whether every unknown has an equation available and every equation an unknown to determine. Stronger than an
/// exactly-determined count, which two opposite failures can cancel into.
/// </param>
/// <param name="Underdetermined">Unknowns with too few equations reaching them.</param>
/// <param name="WellDetermined">The part that can be solved.</param>
/// <param name="Overdetermined">Unknowns with surplus equations, which are checks rather than a shortfall.</param>
/// <param name="RedundantEquationIds">Equations reaching no unknown. They check a result; they determine nothing.</param>
/// <param name="UnknownsWithNoEquationIds">Unknowns no equation reaches at all.</param>
public sealed record DecompositionReport(
    int DegreesOfFreedom,
    Determination Determination,
    bool IsWellPosed,
    BlockReport Underdetermined,
    BlockReport WellDetermined,
    BlockReport Overdetermined,
    IReadOnlyList<string> RedundantEquationIds,
    IReadOnlyList<string> UnknownsWithNoEquationIds);

/// <summary>One part of a decomposition. Parts share no unknown and no equation.</summary>
/// <remarks>
/// <c>DegreesOfFreedom</c> is unknowns less equations within this part alone. The whole system's count is the
/// sum over the parts.
/// </remarks>
public sealed record BlockReport(
    int DegreesOfFreedom,
    Determination Determination,
    IReadOnlyList<UnknownReport> Unknowns,
    IReadOnlyList<EquationReport> Equations);

/// <summary>An unknown: a variable the system reaches and nobody has given a value.</summary>
public sealed record UnknownReport(string Id, string Symbol, string Dimensionality);

/// <summary>A determining relationship, and which unknowns it reaches.</summary>
public sealed record EquationReport(string RelationshipId, IReadOnlyList<string> UnknownIds);
