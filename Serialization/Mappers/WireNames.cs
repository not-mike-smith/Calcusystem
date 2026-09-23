using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Provenance;

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
    internal static string Of(UnaryExpressionType kind) => kind switch
    {
        UnaryExpressionType.Reciprocal => nameof(ReciprocalExpression),
        UnaryExpressionType.Negated => nameof(NegatedExpression),
        UnaryExpressionType.Sqrt => nameof(SqrtExpression),
        UnaryExpressionType.Exponential => nameof(ExponentialExpression),
        UnaryExpressionType.NaturalLog => nameof(NaturalLogExpression),
        _ => throw new NotImplementedException($"No wire name for unary expression kind {kind}"),
    };

    internal static UnaryExpressionType UnaryType(string type) => type switch
    {
        nameof(ReciprocalExpression) => UnaryExpressionType.Reciprocal,
        nameof(NegatedExpression) => UnaryExpressionType.Negated,
        nameof(SqrtExpression) => UnaryExpressionType.Sqrt,
        nameof(ExponentialExpression) => UnaryExpressionType.Exponential,
        nameof(NaturalLogExpression) => UnaryExpressionType.NaturalLog,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for SingleDerivedVariable object with saved type, {type}"),
    };

    internal static string Of(NaryExpressionType kind) => kind switch
    {
        NaryExpressionType.Product => nameof(ProductExpression),
        NaryExpressionType.Sum => nameof(SumExpression),
        _ => throw new NotImplementedException($"No wire name for n-ary expression kind {kind}"),
    };

    internal static NaryExpressionType NaryType(string type) => type switch
    {
        nameof(ProductExpression) => NaryExpressionType.Product,
        nameof(SumExpression) => NaryExpressionType.Sum,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for ListDerivedVariable object with saved type, {type}"),
    };

    internal static string Of(BinaryExpressionType kind) => kind switch
    {
        BinaryExpressionType.Quotient => nameof(QuotientExpression),
        _ => throw new NotImplementedException($"No wire name for binary expression kind {kind}"),
    };

    internal static BinaryExpressionType BinaryType(string type) => type switch
    {
        nameof(QuotientExpression) => BinaryExpressionType.Quotient,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for PairDerivedVariable object with saved type, {type}"),
    };

    internal static string Of(BinaryOperatorType kind) => kind switch
    {
        BinaryOperatorType.Equality => nameof(EqualityOperator),
        BinaryOperatorType.AnyToleranceOverlap => nameof(AnyToleranceOverlapOperator),
        BinaryOperatorType.MutuallyWithinTolerance => nameof(MutuallyWithinToleranceOperator),
        BinaryOperatorType.WhollyWithinTolerance => nameof(WhollyWithinToleranceOperator),
        BinaryOperatorType.WithinBindingTolerance => nameof(WithinBindingToleranceOperator),
        BinaryOperatorType.PointAndUpperBoundWithinTolerance => nameof(PointAndUpperBoundWithinToleranceOperator),
        BinaryOperatorType.PointAndLowerBoundWithinTolerance => nameof(PointAndLowerBoundWithinToleranceOperator),
        BinaryOperatorType.DefinitelyLessThan => nameof(DefinitelyLessThanOperator),
        BinaryOperatorType.UpperBoundsLessThan => nameof(UpperBoundsLessThanOperator),
        BinaryOperatorType.NominallyLessThan => nameof(NominallyLessThanOperator),
        BinaryOperatorType.DefinitelyGreaterThan => nameof(DefinitelyGreaterThanOperator),
        BinaryOperatorType.LowerBoundsGreaterThan => nameof(LowerBoundsGreaterThanOperator),
        BinaryOperatorType.NominallyGreaterThan => nameof(NominallyGreaterThanOperator),
        BinaryOperatorType.SimpleComparison => nameof(SimpleComparison),
        _ => throw new NotImplementedException($"No wire name for operator kind {kind}"),
    };

    internal static BinaryOperatorType OperatorType(string type) => type switch
    {
        nameof(EqualityOperator) => BinaryOperatorType.Equality,
        nameof(AnyToleranceOverlapOperator) => BinaryOperatorType.AnyToleranceOverlap,
        nameof(MutuallyWithinToleranceOperator) => BinaryOperatorType.MutuallyWithinTolerance,
        nameof(WhollyWithinToleranceOperator) => BinaryOperatorType.WhollyWithinTolerance,
        nameof(WithinBindingToleranceOperator) => BinaryOperatorType.WithinBindingTolerance,
        nameof(PointAndUpperBoundWithinToleranceOperator) => BinaryOperatorType.PointAndUpperBoundWithinTolerance,
        nameof(PointAndLowerBoundWithinToleranceOperator) => BinaryOperatorType.PointAndLowerBoundWithinTolerance,
        nameof(DefinitelyLessThanOperator) => BinaryOperatorType.DefinitelyLessThan,
        nameof(UpperBoundsLessThanOperator) => BinaryOperatorType.UpperBoundsLessThan,
        nameof(NominallyLessThanOperator) => BinaryOperatorType.NominallyLessThan,
        nameof(DefinitelyGreaterThanOperator) => BinaryOperatorType.DefinitelyGreaterThan,
        nameof(LowerBoundsGreaterThanOperator) => BinaryOperatorType.LowerBoundsGreaterThan,
        nameof(NominallyGreaterThanOperator) => BinaryOperatorType.NominallyGreaterThan,
        nameof(SimpleComparison) => BinaryOperatorType.SimpleComparison,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for BinaryOperator object with saved type, {type}"),
    };

    internal static string Of(ProvenanceType kind) => kind switch
    {
        ProvenanceType.Measured => nameof(MeasuredProvenance),
        ProvenanceType.Reference => nameof(ReferenceProvenance),
        ProvenanceType.Design => nameof(DesignProvenance),
        ProvenanceType.Model => nameof(ModelProvenance),
        _ => throw new NotImplementedException($"No wire name for provenance kind {kind}"),
    };

    internal static ProvenanceType ProvenanceTypeOf(string type) => type switch
    {
        nameof(MeasuredProvenance) => ProvenanceType.Measured,
        nameof(ReferenceProvenance) => ProvenanceType.Reference,
        nameof(DesignProvenance) => ProvenanceType.Design,
        nameof(ModelProvenance) => ProvenanceType.Model,
        _ => throw new NotImplementedException(
            $"No deserialization method defined for provenance type {type}"),
    };
}
