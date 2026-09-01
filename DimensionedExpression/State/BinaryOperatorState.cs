using Calcusystem.DimensionedExpression.BinaryOperators;

namespace Calcusystem.DimensionedExpression.State;

/// <summary>Which operator a <see cref="BinaryOperatorState"/> rebuilds into.</summary>
/// <remarks>
/// The full taxonomy — symbol, commutativity, and the exact interval condition each one tests — lives in
/// <c>BinaryOperators/OPERATORS.md</c>.
/// </remarks>
public enum BinaryOperatorKind
{
    /// <summary>Values agree, to the strictness the state's agreement rule names.</summary>
    Equality,

    /// <summary>The uncertainty intervals overlap at all.</summary>
    AnyToleranceOverlap,

    /// <summary>Each value lies within the other's interval.</summary>
    MutuallyWithinTolerance,

    /// <summary>One interval lies wholly within the other.</summary>
    WhollyWithinTolerance,

    /// <summary>Within the tighter of the two tolerances.</summary>
    WithinBindingTolerance,

    /// <summary>Point and upper bound within tolerance.</summary>
    PointAndUpperBoundWithinTolerance,

    /// <summary>Point and lower bound within tolerance.</summary>
    PointAndLowerBoundWithinTolerance,

    /// <summary>Entire left interval strictly below the entire right one.</summary>
    DefinitelyLessThan,

    /// <summary>Left ceiling below right ceiling.</summary>
    UpperBoundsLessThan,

    /// <summary>Nominal values compared, uncertainty ignored.</summary>
    NominallyLessThan,

    /// <summary>Entire left interval strictly above the entire right one.</summary>
    DefinitelyGreaterThan,

    /// <summary>Left floor above right floor.</summary>
    LowerBoundsGreaterThan,

    /// <summary>Nominal values compared, uncertainty ignored.</summary>
    NominallyGreaterThan,

    /// <summary>One comparison between named landmarks — the general form, carrying its own rule.</summary>
    SimpleComparison,
}

/// <summary>
/// The complete stored state of any binary operator. Every operator has the same shape — two operand references
/// plus annotations — so one record with a <see cref="Kind"/> discriminator covers all thirteen.
/// </summary>
/// <param name="Kind">Which operator this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="LhsId">Id of the left-hand expression.</param>
/// <param name="RhsId">Id of the right-hand expression.</param>
/// <param name="SolvingRole">
/// What this relationship does to the problem. Stored as the role rather than as the derived
/// <c>IsDetermining</c> boolean, because that flattens <c>Equation</c> and <c>Coherence</c> together and they
/// cannot be told apart again on load. Only the equality kind can store anything but
/// <see cref="DimensionedExpression.SolvingRole.Requirement"/>; for every other kind reconstruction ignores it,
/// because those types have no way to represent it.
/// </param>
/// <param name="Agreement">
/// How strictly an equality reads "equal", and null for every other kind. Stored rather than left to the
/// reader: without it the wire says a relationship is an equality and nothing about what equality means, so two
/// readers can reach opposite verdicts from identical bytes.
/// </param>
/// <param name="Rule">
/// The comparison a <see cref="BinaryOperatorKind.SimpleComparison"/> asserts, and null for every other kind,
/// whose rules are fixed by their type.
/// </param>
/// <param name="Name">Optional human-readable name.</param>
/// <param name="Description">Optional human-readable description.</param>
/// <param name="Provenance">Where the relationship came from (e.g. a citation), or null when untracked.</param>
public readonly record struct BinaryOperatorState(
    BinaryOperatorKind Kind,
    string Id,
    string LhsId,
    string RhsId,
    SolvingRole SolvingRole,
    AgreementRule? Agreement,
    ComparisonRule? Rule,
    string? Name,
    string? Description,
    ProvenanceState? Provenance);
