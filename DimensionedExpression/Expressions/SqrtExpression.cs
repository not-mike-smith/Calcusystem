using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary square root of any <see cref="IExpression"/>. The result's dimensionality is the argument's with every
/// exponent halved (e.g. √(m²·s⁻²) → m·s⁻¹), so every exponent must be even — an odd one throws
/// <see cref="Measurement.Exceptions.NondiscreteDimensionalityException"/> on construction.
/// <br/><br/>
/// A negative argument value yields a NaN result.
/// <br/><br/>
/// Uncertainty follows the power rule: RelativeUncertainty(√x) = ½·RelativeUncertainty(x).
/// </summary>
public class SqrtExpression : ExpressionBase, IExpression, ISnapshottingNode<SqrtExpression, UnaryExpressionSnapshot>
{
    private readonly IExpression _argument;

    public SqrtExpression(IExpression argument, string id = IdBase.CREATE_NEW_ID) : base(id)
    {
        // Halving the exponents is the step that can fail, so it happens here — once, where the operand
        // arrives, like every other operand rule in this assembly.
        Dimensionality = argument.Dimensionality / 2;
        _argument = argument;
    }

    public IExpression Argument => _argument;

    public override bool IsDirectlyMutable => false;
    public override bool IsFullyDescribed => Argument.IsFullyDescribed;

    /// <summary><inheritdoc/></summary>
    /// <remarks>Each of the argument's exponents, halved. Settled at construction.</remarks>
    public override Dimensionality Dimensionality { get; }

    /// <inheritdoc/>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null) =>
        known.TryGetValue(Argument, out var operand) ? operand.ToRoot(2) : null;

    public override string ToString()
    {
        return $"√({Argument})";
    }

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [Argument];

    /// <inheritdoc/>
    public UnaryExpressionSnapshot GetSnapshot() =>
        new(UnaryExpressionType.Sqrt, Id, Argument.Id);

    /// <inheritdoc/>
    public static SqrtExpression FromSnapshot(UnaryExpressionSnapshot snapshot, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(snapshot.InnerId), snapshot.Id);
}
