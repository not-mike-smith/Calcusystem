using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// An unnamed, dimensionless coefficient with no uncertainty: the <c>2</c> and the <c>π/4</c> in
/// <c>π/4·D²</c>.
/// </summary>
/// <remarks>
/// Part of the formula it appears in, not a quantity of the model: it has no name and no provenance, and a
/// system lists it in neither <c>Variables</c> nor <c>Givens</c>.
/// </remarks>
public sealed class Scalar : FixedExpressionBase, ISnapshotting<Scalar, FixedExpressionSnapshot>
{
    /// <exception cref="InvalidFixedValueException"><paramref name="value"/> has uncertainty or a dimension.</exception>
    internal Scalar(Measurand value, string id)
        : base(FixedExpressionType.Scalar, value, id) { }

    /// <summary><inheritdoc/></summary>
    /// <exception cref="ArgumentException">The snapshot is not a scalar's, or it has a name or a provenance.</exception>
    /// <exception cref="InvalidFixedValueException">The snapshot's value has uncertainty or a dimension.</exception>
    public static Scalar FromSnapshot(FixedExpressionSnapshot snapshot)
    {
        var (value, provenance) = Read(snapshot, FixedExpressionType.Scalar);

        if (snapshot.Name is not null || provenance is not null)
        {
            throw new ArgumentException("A scalar has no name and no provenance.", nameof(snapshot));
        }

        return new Scalar(value, snapshot.Id);
    }
}
