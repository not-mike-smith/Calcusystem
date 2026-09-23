using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.Enums;

namespace Calcusystem.Serialization.Mappers;

/// <summary>
/// Translates between the domain's state discriminators and the <c>Type</c> strings written to the wire.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of the coupling between the two vocabularies, and it lives here because the strings are a
/// storage format decision. The state records use enums, which say nothing about how they are persisted; the
/// payload uses concrete type names, which is what previously-written data already contains.
/// </para>
/// <para>
/// The names are deliberately the concrete class names rather than the enum member names, so this change did not
/// invalidate existing payloads. If a domain class is ever renamed, the corresponding string here should
/// <i>not</i> follow it — the mapping is a migration point, not a mirror.
/// </para>
/// </remarks>
internal static class WireNames
{
    internal static string Of(UnaryExpressionKind kind) => kind switch
    {
        UnaryExpressionKind.Reciprocal => nameof(ReciprocalExpression),
        UnaryExpressionKind.Negated => nameof(NegatedExpression),
        UnaryExpressionKind.Sqrt => nameof(SqrtExpression),
        UnaryExpressionKind.Exponential => nameof(ExponentialExpression),
        UnaryExpressionKind.NaturalLog => nameof(NaturalLogExpression),
        _ => throw new NotImplementedException($"No wire name for unary expression kind {kind}"),
    };

    internal static UnaryExpressionKind UnaryKind(string type) => type switch
    {
        nameof(ReciprocalExpression) => UnaryExpressionKind.Reciprocal,
        nameof(NegatedExpression) => UnaryExpressionKind.Negated,
        nameof(SqrtExpression) => UnaryExpressionKind.Sqrt,
        nameof(ExponentialExpression) => UnaryExpressionKind.Exponential,
        nameof(NaturalLogExpression) => UnaryExpressionKind.NaturalLog,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for SingleDerivedVariable object with saved type, {type}"),
    };

    internal static string Of(NaryExpressionKind kind) => kind switch
    {
        NaryExpressionKind.Product => nameof(ProductExpression),
        NaryExpressionKind.Sum => nameof(SumExpression),
        _ => throw new NotImplementedException($"No wire name for n-ary expression kind {kind}"),
    };

    internal static NaryExpressionKind NaryKind(string type) => type switch
    {
        nameof(ProductExpression) => NaryExpressionKind.Product,
        nameof(SumExpression) => NaryExpressionKind.Sum,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for ListDerivedVariable object with saved type, {type}"),
    };

    internal static string Of(BinaryExpressionKind kind) => kind switch
    {
        BinaryExpressionKind.Quotient => nameof(QuotientExpression),
        _ => throw new NotImplementedException($"No wire name for binary expression kind {kind}"),
    };

    internal static BinaryExpressionKind BinaryKind(string type) => type switch
    {
        nameof(QuotientExpression) => BinaryExpressionKind.Quotient,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for PairDerivedVariable object with saved type, {type}"),
    };

    internal static string Of(BinaryOperatorKind kind) => kind switch
    {
        BinaryOperatorKind.Equality => nameof(EqualityOperator),
        BinaryOperatorKind.AnyToleranceOverlap => nameof(AnyToleranceOverlapOperator),
        BinaryOperatorKind.MutuallyWithinTolerance => nameof(MutuallyWithinToleranceOperator),
        BinaryOperatorKind.WhollyWithinTolerance => nameof(WhollyWithinToleranceOperator),
        BinaryOperatorKind.WithinBindingTolerance => nameof(WithinBindingToleranceOperator),
        BinaryOperatorKind.PointAndUpperBoundWithinTolerance => nameof(PointAndUpperBoundWithinToleranceOperator),
        BinaryOperatorKind.PointAndLowerBoundWithinTolerance => nameof(PointAndLowerBoundWithinToleranceOperator),
        BinaryOperatorKind.DefinitelyLessThan => nameof(DefinitelyLessThanOperator),
        BinaryOperatorKind.UpperBoundsLessThan => nameof(UpperBoundsLessThanOperator),
        BinaryOperatorKind.NominallyLessThan => nameof(NominallyLessThanOperator),
        BinaryOperatorKind.DefinitelyGreaterThan => nameof(DefinitelyGreaterThanOperator),
        BinaryOperatorKind.LowerBoundsGreaterThan => nameof(LowerBoundsGreaterThanOperator),
        BinaryOperatorKind.NominallyGreaterThan => nameof(NominallyGreaterThanOperator),
        BinaryOperatorKind.SimpleComparison => nameof(SimpleComparison),
        _ => throw new NotImplementedException($"No wire name for operator kind {kind}"),
    };

    internal static BinaryOperatorKind OperatorKind(string type) => type switch
    {
        nameof(EqualityOperator) => BinaryOperatorKind.Equality,
        nameof(AnyToleranceOverlapOperator) => BinaryOperatorKind.AnyToleranceOverlap,
        nameof(MutuallyWithinToleranceOperator) => BinaryOperatorKind.MutuallyWithinTolerance,
        nameof(WhollyWithinToleranceOperator) => BinaryOperatorKind.WhollyWithinTolerance,
        nameof(WithinBindingToleranceOperator) => BinaryOperatorKind.WithinBindingTolerance,
        nameof(PointAndUpperBoundWithinToleranceOperator) => BinaryOperatorKind.PointAndUpperBoundWithinTolerance,
        nameof(PointAndLowerBoundWithinToleranceOperator) => BinaryOperatorKind.PointAndLowerBoundWithinTolerance,
        nameof(DefinitelyLessThanOperator) => BinaryOperatorKind.DefinitelyLessThan,
        nameof(UpperBoundsLessThanOperator) => BinaryOperatorKind.UpperBoundsLessThan,
        nameof(NominallyLessThanOperator) => BinaryOperatorKind.NominallyLessThan,
        nameof(DefinitelyGreaterThanOperator) => BinaryOperatorKind.DefinitelyGreaterThan,
        nameof(LowerBoundsGreaterThanOperator) => BinaryOperatorKind.LowerBoundsGreaterThan,
        nameof(NominallyGreaterThanOperator) => BinaryOperatorKind.NominallyGreaterThan,
        nameof(SimpleComparison) => BinaryOperatorKind.SimpleComparison,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for BinaryOperator object with saved type, {type}"),
    };

    internal static string Of(ProvenanceKind kind) => kind switch
    {
        ProvenanceKind.Measured => nameof(MeasuredProvenance),
        ProvenanceKind.Reference => nameof(ReferenceProvenance),
        ProvenanceKind.Design => nameof(DesignProvenance),
        ProvenanceKind.Model => nameof(ModelProvenance),
        _ => throw new NotImplementedException($"No wire name for provenance kind {kind}"),
    };

    internal static ProvenanceKind ProvenanceKindOf(string type) => type switch
    {
        nameof(MeasuredProvenance) => ProvenanceKind.Measured,
        nameof(ReferenceProvenance) => ProvenanceKind.Reference,
        nameof(DesignProvenance) => ProvenanceKind.Design,
        nameof(ModelProvenance) => ProvenanceKind.Model,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for provenance type {type}"),
    };
}
