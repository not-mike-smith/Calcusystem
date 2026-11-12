using System.Globalization;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>Base for the fixed values: a leaf whose value is given when it is built and never changes.</summary>
public abstract class FixedExpressionBase : ExpressionBase, IFixedExpression
{
    /// <param name="kind">Which fixed value this is.</param>
    /// <param name="name">The name. Required for every kind but a scalar, which has none.</param>
    /// <param name="value">The value, which must be one <paramref name="kind"/> accepts.</param>
    /// <param name="provenance">Where the value came from, or null. Not kept for a scalar.</param>
    /// <param name="id">Stable identity.</param>
    /// <exception cref="InvalidFixedValueException"><paramref name="value"/> breaks the rule for <paramref name="kind"/>.</exception>
    /// <exception cref="InvalidNameException">A kind that must have a name was given none, or an invalid one.</exception>
    private protected FixedExpressionBase(
        FixedExpressionType kind,
        string? name,
        Measurand value,
        IProvenance? provenance,
        string id)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (FixedValueRule.Problem(kind, value) is { } problem) throw new InvalidFixedValueException(problem);

        _kind = kind;
        Value = value;

        if (kind is FixedExpressionType.Scalar) return;

        var noun = kind is FixedExpressionType.Constant ? "constant" : "reference value";
        Name = name ?? throw new InvalidNameException($"A {noun} needs a name.");
        Provenance = provenance;
    }

    private readonly FixedExpressionType _kind;

    /// <inheritdoc/>
    public Measurand Value { get; }

    /// <inheritdoc/>
    public IProvenance? Provenance { get; }

    /// <inheritdoc/>
    public override bool IsDirectlyMutable => false;

    /// <summary><inheritdoc/></summary>
    /// <remarks>Always true: a fixed value is never unset.</remarks>
    public override bool IsFullyDescribed => true;

    /// <summary><inheritdoc/></summary>
    /// <remarks>The value's own.</remarks>
    public override Dimensionality Dimensionality => Value.Dimensionality;

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [];

    /// <summary><inheritdoc/></summary>
    /// <remarks>Its own value, whatever <paramref name="known"/> holds: a fixed value cannot be overridden.</remarks>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null) => Value;

    /// <summary>The name, or the number for a scalar.</summary>
    public override string ToString() => Name ?? Value.KmsValue.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public FixedExpressionSnapshot GetSnapshot() =>
        new(_kind, Id, Name, Value.GetSnapshot(), Provenance?.GetSnapshot());

    /// <summary>The checked parts of a snapshot that names <paramref name="kind"/>.</summary>
    /// <exception cref="ArgumentException">The snapshot is of another kind.</exception>
    private protected static (Measurand Value, IProvenance? Provenance) Read(
        FixedExpressionSnapshot snapshot,
        FixedExpressionType kind)
    {
        if (snapshot.Type != kind)
        {
            throw new ArgumentException($"A snapshot of a {snapshot.Type} does not rebuild a {kind}.", nameof(snapshot));
        }

        return (
            Measurand.FromSnapshot(snapshot.Value),
            snapshot.Provenance is { } provenance ? ProvenanceFactory.FromSnapshot(provenance) : null);
    }
}
