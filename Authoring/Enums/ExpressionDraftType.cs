namespace Calcusystem.Authoring.Enums;

/// <summary>
/// Which kind of expression a draft node is to become.
/// </summary>
/// <remarks>
/// One flat set over all nine node kinds, where the snapshots group them by arity across three enums. An author
/// picks from a single list — the arity grouping exists so reconstruction can dispatch on it, which is not a
/// question anyone building a model asks. <c>DraftTypeCoverageTests</c> holds the two in step.
/// </remarks>
public enum ExpressionDraftType
{
    /// <summary>A leaf whose value is supplied directly.</summary>
    Variable = 1,

    /// <summary>Sum of every operand. All must share a dimensionality.</summary>
    Sum,

    /// <summary>Product of every operand.</summary>
    Product,

    /// <summary>First operand divided by the second.</summary>
    Quotient,

    /// <summary>Negation of its one operand.</summary>
    Negated,

    /// <summary>One divided by its one operand.</summary>
    Reciprocal,

    /// <summary>Square root of its one operand. Every dimensional exponent must be even.</summary>
    Sqrt,

    /// <summary><c>e</c> raised to its one operand, which must be dimensionless.</summary>
    Exponential,

    /// <summary>Natural log of its one operand, which must be dimensionless and positive.</summary>
    NaturalLog,
}
