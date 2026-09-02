using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Rebuilds expressions from captured state. The counterpart to each expression's <c>GetSnapshot</c>.
/// </summary>
/// <remarks>
/// State records are grouped by arity and carry a kind discriminator, so reconstruction picks a concrete type by
/// inspecting the state — the same situation as uncertainty and provenance, and handled the same way: a static
/// gateway over the closed set. Each overload delegates to the concrete type's own <c>FromSnapshot</c>, which is
/// where the per-type construction actually lives.
/// </remarks>
public static class ExpressionFactory
{
    /// <summary>Rebuilds a single-argument expression.</summary>
    public static IExpression FromSnapshot(UnaryExpressionSnapshot state, INodeResolver resolve) => state.Type switch
    {
        UnaryExpressionType.Reciprocal => ReciprocalExpression.FromSnapshot(state, resolve),
        UnaryExpressionType.Negated => NegatedExpression.FromSnapshot(state, resolve),
        UnaryExpressionType.Sqrt => SqrtExpression.FromSnapshot(state, resolve),
        UnaryExpressionType.Exponential => ExponentialExpression.FromSnapshot(state, resolve),
        UnaryExpressionType.NaturalLog => NaturalLogExpression.FromSnapshot(state, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Unknown unary expression kind."),
    };

    /// <summary>Rebuilds an n-ary expression.</summary>
    public static IExpression FromSnapshot(NaryExpressionSnapshot state, INodeResolver resolve) => state.Type switch
    {
        NaryExpressionType.Product => ProductExpression.FromSnapshot(state, resolve),
        NaryExpressionType.Sum => SumExpression.FromSnapshot(state, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Unknown n-ary expression kind."),
    };

    /// <summary>Rebuilds a two-argument expression.</summary>
    public static IExpression FromSnapshot(BinaryExpressionSnapshot state, INodeResolver resolve) => state.Type switch
    {
        BinaryExpressionType.Quotient => QuotientExpression.FromSnapshot(state, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Unknown binary expression kind."),
    };
}
