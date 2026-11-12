using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// A named value with uncertainty, fixed when it is built: the gravitational constant <i>G</i>.
/// </summary>
/// <remarks>
/// Listed in <c>ExpressionSystem.Givens</c>, never in <c>Variables</c>. The same value with no uncertainty is a
/// <see cref="Constant"/>.
/// </remarks>
public sealed class ReferenceValue : FixedExpressionBase, ISnapshotting<ReferenceValue, FixedExpressionSnapshot>
{
    /// <exception cref="InvalidNameException"><paramref name="name"/> is not a valid name.</exception>
    /// <exception cref="InvalidFixedValueException"><paramref name="value"/> has no uncertainty.</exception>
    internal ReferenceValue(string name, Measurand value, IProvenance? provenance, string id)
        : base(Enums.FixedExpressionType.ReferenceValue, value, id) => throw new NotImplementedException();

    /// <summary>What the model calls this reference value. Never null.</summary>
    public new string Name => base.Name!;

    /// <inheritdoc/>
    public FixedExpressionSnapshot GetSnapshot() => throw new NotImplementedException();

    /// <summary><inheritdoc/></summary>
    /// <exception cref="ArgumentException">The snapshot is not a reference value's.</exception>
    /// <exception cref="InvalidNameException">The snapshot's name is missing or not a valid name.</exception>
    /// <exception cref="InvalidFixedValueException">The snapshot's value has no uncertainty.</exception>
    public static ReferenceValue FromSnapshot(FixedExpressionSnapshot snapshot) => throw new NotImplementedException();
}
