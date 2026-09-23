using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Core;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Rebuilds binary operators from captured state. The counterpart to <c>BinaryOperatorBase.GetState</c>.
/// </summary>
/// <remarks>
/// A gateway rather than a per-type <c>FromState</c>: construction is identical across all thirteen operators
/// apart from which type is instantiated, so per-type implementations would be pure duplication.
/// </remarks>
public static class BinaryOperatorFactory
{
    /// <summary>Rebuilds the operator described by <paramref name="state"/>.</summary>
    /// <param name="state">The captured state.</param>
    /// <param name="resolve">Resolves the operand ids.</param>
    /// <exception cref="ArgumentException">
    /// An equality whose state names no agreement rule. Equality semantics come from the document, so a
    /// document that omits them describes no particular relationship and is not guessed at.
    /// </exception>
    public static IBinaryOperator FromState(BinaryOperatorState state, INodeResolver resolve)
    {
        var lhs = resolve.Resolve<IExpression>(state.LhsId);
        var rhs = resolve.Resolve<IExpression>(state.RhsId);

        BinaryOperatorBase op = state.Kind switch
        {
            BinaryOperatorKind.Equality =>
                new EqualityOperator(AgreementOf(state), state.SolvingRole)
                    { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.AnyToleranceOverlap =>
                new AnyToleranceOverlapOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.MutuallyWithinTolerance =>
                new MutuallyWithinToleranceOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.WhollyWithinTolerance =>
                new WhollyWithinToleranceOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.WithinBindingTolerance =>
                new WithinBindingToleranceOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.PointAndUpperBoundWithinTolerance =>
                new PointAndUpperBoundWithinToleranceOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.PointAndLowerBoundWithinTolerance =>
                new PointAndLowerBoundWithinToleranceOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.DefinitelyLessThan =>
                new DefinitelyLessThanOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.UpperBoundsLessThan =>
                new UpperBoundsLessThanOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.NominallyLessThan =>
                new NominallyLessThanOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.DefinitelyGreaterThan =>
                new DefinitelyGreaterThanOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.LowerBoundsGreaterThan =>
                new LowerBoundsGreaterThanOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.NominallyGreaterThan =>
                new NominallyGreaterThanOperator { Id = state.Id, Lhs = lhs, Rhs = rhs },
            BinaryOperatorKind.SimpleComparison =>
                new SimpleComparison(RuleOf(state)) { Id = state.Id, Lhs = lhs, Rhs = rhs },
            _ => throw new ArgumentOutOfRangeException(nameof(state), state.Kind, "Unknown operator kind."),
        };

        op.Name = state.Name;
        op.Description = state.Description;
        op.Provenance = state.Provenance is { } p ? ProvenanceFactory.FromState(p) : null;
        return op;
    }

    private static ComparisonRule RuleOf(BinaryOperatorState state) =>
        state.Rule
        ?? throw new ArgumentException(
            $"Simple comparison '{state.Id}' has no rule.", nameof(state));

    private static AgreementRule AgreementOf(BinaryOperatorState state) =>
        state.Agreement
        ?? throw new ArgumentException(
            $"Equality operator '{state.Id}' has no agreement rule.", nameof(state));
}
