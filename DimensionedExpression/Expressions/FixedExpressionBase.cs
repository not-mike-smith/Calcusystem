using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>Base for the fixed values: a leaf whose value is given when it is built and never changes.</summary>
public abstract class FixedExpressionBase : ExpressionBase, IFixedExpression
{
    /// <exception cref="InvalidFixedValueException"><paramref name="value"/> breaks the rule for <paramref name="kind"/>.</exception>
    private protected FixedExpressionBase(FixedExpressionType kind, Measurand value, string id) : base(id) =>
        throw new NotImplementedException();

    /// <inheritdoc/>
    public Measurand Value => throw new NotImplementedException();

    /// <inheritdoc/>
    public IProvenance? Provenance { get; private protected init; }

    /// <inheritdoc/>
    public override bool IsDirectlyMutable => false;

    /// <summary><inheritdoc/></summary>
    /// <remarks>Always true: a fixed value is never unset.</remarks>
    public override bool IsFullyDescribed => true;

    /// <summary><inheritdoc/></summary>
    /// <remarks>The value's own.</remarks>
    public override Dimensionality Dimensionality => throw new NotImplementedException();

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [];

    /// <summary><inheritdoc/></summary>
    /// <remarks>Its own value, whatever <paramref name="known"/> holds: a fixed value cannot be overridden.</remarks>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null) => throw new NotImplementedException();

    private protected FixedExpressionSnapshot Snapshot(FixedExpressionType kind) => throw new NotImplementedException();

    /// <summary>The checked parts of a snapshot that names <paramref name="kind"/>.</summary>
    /// <exception cref="ArgumentException">The snapshot is of another kind.</exception>
    private protected static (Measurand Value, IProvenance? Provenance) Read(
        FixedExpressionSnapshot snapshot,
        FixedExpressionType kind) => throw new NotImplementedException();
}
