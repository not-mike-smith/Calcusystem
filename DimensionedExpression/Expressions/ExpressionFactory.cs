using Calcusystem.Core;
using DimensionedExpression.Interfaces;
using DimensionedExpression.State;

namespace DimensionedExpression.Expressions;

/// <summary>
/// Rebuilds expressions from captured state. The counterpart to each expression's <c>GetState</c>.
/// </summary>
/// <remarks>
/// State records are grouped by arity and carry a kind discriminator, so reconstruction picks a concrete type by
/// inspecting the state — the same situation as uncertainty and provenance, and handled the same way: a static
/// gateway over the closed set. Each overload delegates to the concrete type's own <c>FromState</c>, which is
/// where the per-type construction actually lives.
/// </remarks>
public static class ExpressionFactory
{
    /// <summary>Rebuilds a single-argument expression.</summary>
    public static IExpression FromState(UnaryExpressionState state, INodeResolver resolve) => state.Kind switch
    {
        UnaryExpressionKind.Reciprocal => ReciprocalExpression.FromState(state, resolve),
        UnaryExpressionKind.Negated => NegatedExpression.FromState(state, resolve),
        UnaryExpressionKind.Sqrt => SqrtExpression.FromState(state, resolve),
        UnaryExpressionKind.Exponential => ExponentialExpression.FromState(state, resolve),
        UnaryExpressionKind.NaturalLog => NaturalLogExpression.FromState(state, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state.Kind, "Unknown unary expression kind."),
    };

    /// <summary>Rebuilds an n-ary expression.</summary>
    public static IExpression FromState(NaryExpressionState state, INodeResolver resolve) => state.Kind switch
    {
        NaryExpressionKind.Product => ProductExpression.FromState(state, resolve),
        NaryExpressionKind.Sum => SumExpression.FromState(state, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state.Kind, "Unknown n-ary expression kind."),
    };

    /// <summary>Rebuilds a two-argument expression.</summary>
    public static IExpression FromState(BinaryExpressionState state, INodeResolver resolve) => state.Kind switch
    {
        BinaryExpressionKind.Quotient => QuotientExpression.FromState(state, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state.Kind, "Unknown binary expression kind."),
    };
}
