namespace DimensionedExpression.State;

/// <summary>Which operator a <see cref="BinaryOperatorState"/> rebuilds into.</summary>
/// <remarks>
/// The full taxonomy — symbol, commutativity, and the exact interval condition each one tests — lives in
/// <c>BinaryOperators/OPERATORS.md</c>.
/// </remarks>
public enum BinaryOperatorKind
{
    /// <summary>Values are equal, per an injected equality strategy.</summary>
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
}

/// <summary>
/// The complete stored state of any binary operator. Every operator has the same shape — two operand references
/// plus annotations — so one record with a <see cref="Kind"/> discriminator covers all thirteen.
/// </summary>
/// <param name="Kind">Which operator this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="LhsId">Id of the left-hand expression.</param>
/// <param name="RhsId">Id of the right-hand expression.</param>
/// <param name="Name">Optional human-readable name.</param>
/// <param name="Description">Optional human-readable description.</param>
/// <param name="Provenance">Where the relationship came from (e.g. a citation), or null when untracked.</param>
public readonly record struct BinaryOperatorState(
    BinaryOperatorKind Kind,
    string Id,
    string LhsId,
    string RhsId,
    string? Name,
    string? Description,
    ProvenanceState? Provenance);
