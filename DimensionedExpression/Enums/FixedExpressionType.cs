using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>Which fixed value a <see cref="FixedExpressionSnapshot"/> rebuilds into.</summary>
public enum FixedExpressionType
{
    /// <summary>A named value with no uncertainty.</summary>
    Constant,

    /// <summary>A named value with uncertainty.</summary>
    ReferenceValue,

    /// <summary>An unnamed, dimensionless coefficient with no uncertainty.</summary>
    Scalar,
}
