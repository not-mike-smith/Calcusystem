using Calcusystem.DimensionedExpression.Snapshots;


namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>Which operator a <see cref="BinaryOperatorSnapshot"/> rebuilds into.</summary>
/// <remarks>
/// The full taxonomy — symbol, commutativity, and the exact interval condition each one tests — lives in
/// <c>BinaryOperators/OPERATORS.md</c>.
/// </remarks>
public enum BinaryOperatorType
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
