using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Rebuilds expressions from a captured snapshot. The counterpart to each expression's <c>GetSnapshot</c>.
/// </summary>
/// <remarks>
/// Snapshots are grouped by what they store and carry a kind discriminator, so reconstruction picks a concrete type by
/// inspecting the snapshot — the same situation as uncertainty and provenance, and handled the same way: a static
/// gateway over the closed set. Each overload delegates to the concrete type's own <c>FromSnapshot</c>, which is
/// where the per-type construction actually lives.
/// </remarks>
public static class ExpressionFactory
{
    /// <summary>Rebuilds a fixed value. It names no other node, so it needs no resolver.</summary>
    /// <exception cref="Exceptions.InvalidNameException">The snapshot's name is not one its kind accepts.</exception>
    /// <exception cref="Exceptions.InvalidFixedValueException">The snapshot's value is not one its kind accepts.</exception>
    public static IFixedExpression FromSnapshot(FixedExpressionSnapshot snapshot) => snapshot.Type switch
    {
        FixedExpressionType.Constant => Constant.FromSnapshot(snapshot),
        FixedExpressionType.ReferenceValue => ReferenceValue.FromSnapshot(snapshot),
        FixedExpressionType.Scalar => Scalar.FromSnapshot(snapshot),
        _ => throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.Type, "Unknown kind of fixed value."),
    };

    /// <summary>Rebuilds a single-argument expression.</summary>
    public static IExpression FromSnapshot(UnaryExpressionSnapshot snapshot, INodeResolver resolve) => snapshot.Type switch
    {
        UnaryExpressionType.Reciprocal => ReciprocalExpression.FromSnapshot(snapshot, resolve),
        UnaryExpressionType.Negated => NegatedExpression.FromSnapshot(snapshot, resolve),
        UnaryExpressionType.Sqrt => SqrtExpression.FromSnapshot(snapshot, resolve),
        UnaryExpressionType.Exponential => ExponentialExpression.FromSnapshot(snapshot, resolve),
        UnaryExpressionType.NaturalLog => NaturalLogExpression.FromSnapshot(snapshot, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.Type, "Unknown unary expression kind."),
    };

    /// <summary>Rebuilds an n-ary expression.</summary>
    public static IExpression FromSnapshot(NaryExpressionSnapshot snapshot, INodeResolver resolve) => snapshot.Type switch
    {
        NaryExpressionType.Product => ProductExpression.FromSnapshot(snapshot, resolve),
        NaryExpressionType.Sum => SumExpression.FromSnapshot(snapshot, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.Type, "Unknown n-ary expression kind."),
    };

    /// <summary>Rebuilds a two-argument expression.</summary>
    public static IExpression FromSnapshot(BinaryExpressionSnapshot snapshot, INodeResolver resolve) => snapshot.Type switch
    {
        BinaryExpressionType.Quotient => QuotientExpression.FromSnapshot(snapshot, resolve),
        _ => throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.Type, "Unknown binary expression kind."),
    };
}
