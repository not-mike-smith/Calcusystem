using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Binary quotient of a <see cref="Numerator"/> over a <see cref="Denominator"/> (both required); the result
/// dimensionality is the numerator's divided by the denominator's.
/// <br/>
/// A computed node: uncertainty is propagated through <see cref="Measurand"/> division using the
/// <see cref="ComputedExpressionBase.UncertaintyCorrelation"/> method.
/// </summary>
public class QuotientExpression : ComputedExpressionBase, IComputedExpression, ISnapshottingNode<QuotientExpression, BinaryExpressionSnapshot>
{
    public required IExpression Numerator { get; init; }

    public required IExpression Denominator { get; init; }

    public override bool IsFullyDescribed => Numerator.IsFullyDescribed && Denominator.IsFullyDescribed;
    public override Dimensionality Dimensionality => Numerator.Dimensionality / Denominator.Dimensionality;

    /// <inheritdoc/>
    /// <remarks>
    /// The case the keyed lookup exists for: numerator and denominator are told apart by identity, not by
    /// which slot a caller happened to put them in.
    /// </remarks>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null) =>
        known.TryGetValue(Numerator, out var numerator) && known.TryGetValue(Denominator, out var denominator)
            ? numerator.DividedBy(denominator, UncertaintyCorrelation, propagator)
            : null;

    public override string ToString()
    {
        return $"{Numerator} / {Denominator}";
    }

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [Numerator, Denominator];

    /// <inheritdoc/>
    public BinaryExpressionSnapshot GetSnapshot() =>
        new(BinaryExpressionType.Quotient, Id, Numerator.Id, Denominator.Id, UncertaintyCorrelation);

    /// <inheritdoc/>
    public static QuotientExpression FromSnapshot(BinaryExpressionSnapshot state, INodeResolver resolve) =>
        new()
        {
            Id = state.Id,
            Numerator = resolve.Resolve<IExpression>(state.InnerId1),
            Denominator = resolve.Resolve<IExpression>(state.InnerId2),
            UncertaintyCorrelation = state.UncertaintyCorrelation,
        };
}
