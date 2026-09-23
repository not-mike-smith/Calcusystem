using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Rebuilds binary operators from a captured snapshot. The counterpart to <c>BinaryOperatorBase.GetSnapshot</c>.
/// </summary>
/// <remarks>
/// A gateway rather than a per-type <c>FromSnapshot</c>: construction is identical across every operator
/// apart from which type is instantiated, so per-type implementations would be pure duplication.
/// </remarks>
public static class BinaryOperatorFactory
{
    /// <summary>Rebuilds the operator described by <paramref name="snapshot"/>.</summary>
    /// <param name="snapshot">The captured snapshot.</param>
    /// <param name="resolve">Resolves the operand ids.</param>
    /// <exception cref="ArgumentException">
    /// An equality whose snapshot names no agreement rule. Equality semantics come from the document, so a
    /// document that omits them describes no particular relationship and is not guessed at.
    /// </exception>
    public static IBinaryOperator FromSnapshot(BinaryOperatorSnapshot snapshot, INodeResolver resolve)
    {
        var lhs = resolve.Resolve<IExpression>(snapshot.LhsId);
        var rhs = resolve.Resolve<IExpression>(snapshot.RhsId);

        BinaryOperatorBase op = snapshot.Type switch
        {
            BinaryOperatorType.Equality =>
                new EqualityOperator(AgreementOf(snapshot), snapshot.SolvingRole)
                    { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.AnyToleranceOverlap =>
                new AnyToleranceOverlapOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.MutuallyWithinTolerance =>
                new MutuallyWithinToleranceOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.WhollyWithinTolerance =>
                new WhollyWithinToleranceOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.WithinBindingTolerance =>
                new WithinBindingToleranceOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.PointAndUpperBoundWithinTolerance =>
                new PointAndUpperBoundWithinToleranceOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.PointAndLowerBoundWithinTolerance =>
                new PointAndLowerBoundWithinToleranceOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.DefinitelyLessThan =>
                new DefinitelyLessThanOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.UpperBoundsLessThan =>
                new UpperBoundsLessThanOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.NominallyLessThan =>
                new NominallyLessThanOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.DefinitelyGreaterThan =>
                new DefinitelyGreaterThanOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.LowerBoundsGreaterThan =>
                new LowerBoundsGreaterThanOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.NominallyGreaterThan =>
                new NominallyGreaterThanOperator { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorType.SimpleComparison =>
                new SimpleComparison(RuleOf(snapshot)) { Id = snapshot.Id, Lhs = lhs, Rhs = rhs },
            _ => throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.Type, "Unknown operator kind."),
        };

        op.Name = snapshot.Name;
        op.Description = snapshot.Description;
        op.Provenance = snapshot.Provenance is { } p ? ProvenanceFactory.FromSnapshot(p) : null;
        return op;
    }

    private static ComparisonRule RuleOf(BinaryOperatorSnapshot snapshot) =>
        snapshot.Rule
        ?? throw new ArgumentException(
            $"Simple comparison '{snapshot.Id}' has no rule.", nameof(snapshot));

    private static AgreementRule AgreementOf(BinaryOperatorSnapshot snapshot) =>
        snapshot.Agreement
        ?? throw new ArgumentException(
            $"Equality operator '{snapshot.Id}' has no agreement rule.", nameof(snapshot));
}
