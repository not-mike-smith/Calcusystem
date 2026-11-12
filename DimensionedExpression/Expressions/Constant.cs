using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// A named value with no uncertainty, fixed when it is built: standard gravity, 9.80665 m/s², exact by
/// definition.
/// </summary>
/// <remarks>
/// Listed in <c>ExpressionSystem.Givens</c>, never in <c>Variables</c>. The same value with uncertainty is a
/// <see cref="ReferenceValue"/>.
/// </remarks>
public sealed class Constant : FixedExpressionBase, ISnapshotting<Constant, FixedExpressionSnapshot>
{
    /// <exception cref="InvalidNameException"><paramref name="name"/> is not a valid name.</exception>
    /// <exception cref="InvalidFixedValueException"><paramref name="value"/> has uncertainty.</exception>
    internal Constant(string name, Measurand value, IProvenance? provenance, string id)
        : base(Enums.FixedExpressionType.Constant, value, id) => throw new NotImplementedException();

    /// <summary>What the model calls this constant. Never null.</summary>
    public new string Name => base.Name!;

    /// <inheritdoc/>
    public FixedExpressionSnapshot GetSnapshot() => throw new NotImplementedException();

    /// <summary><inheritdoc/></summary>
    /// <exception cref="ArgumentException">The snapshot is not a constant's.</exception>
    /// <exception cref="InvalidNameException">The snapshot's name is missing or not a valid name.</exception>
    /// <exception cref="InvalidFixedValueException">The snapshot's value has uncertainty.</exception>
    public static Constant FromSnapshot(FixedExpressionSnapshot snapshot) => throw new NotImplementedException();
}
