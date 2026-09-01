
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies below the entire Rhs uncertainty interval — i.e.
/// Lhs.Upper &lt; Rhs.Lower. No overlap between the two intervals is permitted.
/// <br/>
/// Symbol: <b>⌜&lt;⌟</b>
/// <br/>
/// Use for definitive less-than checks where even worst-case Lhs must remain below best-case Rhs.
/// </summary>
public class DefinitelyLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.DefinitelyLessThan;

    public override string Symbol => "⌜<⌟";

    /// <inheritdoc/>
    /// <remarks>The ordering ladder's <c>Below</c>/<c>Certain</c> rung, though it is stated here as itself.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.UpperBound, ComparisonType.LessThan, Landmark.LowerBound)];
}

/// <summary>
/// Satisfied when the upper bound of Lhs is less than the upper bound of Rhs — i.e. Lhs.Upper &lt; Rhs.Upper.
/// The intervals may overlap; this is a weaker check than <see cref="DefinitelyLessThanOperator"/>.
/// <br/>
/// Symbol: <b>⌜&lt;⌝</b>
/// <br/>
/// Use when you need to know that Lhs's worst-case high value is bounded by Rhs's worst-case high value.
/// <br/>
/// <b>Off the confidence ladder, deliberately.</b> This compares a derived <i>statistic</i> of each side —
/// ceiling against ceiling — rather than asking how the two quantities stand to one another, so it is not a
/// tier of <see cref="OrderingLadder"/> and cannot be reached by strengthening or weakening one.
/// </summary>
public class UpperBoundsLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.UpperBoundsLessThan;

    public override string Symbol => "⌜<⌝";

    /// <inheritdoc/>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.UpperBound, ComparisonType.LessThan, Landmark.UpperBound)];
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is less than the nominal Rhs value. Uncertainty is not part of
/// the ordering, though it still sets the scale at which the two values count as agreeing.
/// <br/>
/// Symbol: <b>·&lt;·</b>
/// <br/>
/// Use when only the reported values matter.
/// </summary>
public class NominallyLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.NominallyLessThan;

    public override string Symbol => "·<·";

    /// <inheritdoc/>
    /// <remarks>The ordering ladder's <c>Below</c>/<c>Nominal</c> rung.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.Nominal, ComparisonType.LessThan, Landmark.Nominal)];
}

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies above the entire Rhs uncertainty interval — i.e.
/// Lhs.Lower &gt; Rhs.Upper. No overlap between the two intervals is permitted.
/// <br/>
/// Symbol: <b>⌞&gt;⌝</b>
/// <br/>
/// Use for definitive greater-than checks where even worst-case Lhs must remain above best-case Rhs.
/// </summary>
public class DefinitelyGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.DefinitelyGreaterThan;

    public override string Symbol => "⌞>⌝";

    /// <inheritdoc/>
    /// <remarks>
    /// The ordering ladder's <c>Above</c>/<c>Certain</c> rung, and written out rather than derived from the
    /// less-than rule: "my floor is above your ceiling" is what this operator checks, and saying so directly
    /// beats making a reader apply a mirroring convention to find out.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.LowerBound, ComparisonType.GreaterThan, Landmark.UpperBound)];
}

/// <summary>
/// Satisfied when the lower bound of Lhs is greater than the lower bound of Rhs — i.e. Lhs.Lower &gt; Rhs.Lower.
/// The intervals may overlap; this is a weaker check than <see cref="DefinitelyGreaterThanOperator"/>.
/// <br/>
/// Symbol: <b>⌞&gt;⌟</b>
/// <br/>
/// Use when you need to know that Lhs's worst-case low value is above Rhs's worst-case low value.
/// <br/>
/// <b>Off the confidence ladder, deliberately</b>, for the same reason as
/// <see cref="UpperBoundsLessThanOperator"/>: it compares floors, which is a statistic of each side rather than
/// a claim about their ordering. Note it is not that operator's mirror image — one compares ceilings and the
/// other floors, so neither is the other read with the operands swapped.
/// </summary>
public class LowerBoundsGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.LowerBoundsGreaterThan;

    public override string Symbol => "⌞>⌟";

    /// <inheritdoc/>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.LowerBound, ComparisonType.GreaterThan, Landmark.LowerBound)];
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is greater than the nominal Rhs value. Uncertainty is not part
/// of the ordering, though it still sets the scale at which the two values count as agreeing.
/// <br/>
/// Symbol: <b>·&gt;·</b>
/// <br/>
/// Use when only the reported values matter.
/// </summary>
public class NominallyGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.NominallyGreaterThan;

    public override string Symbol => "·>·";

    /// <inheritdoc/>
    /// <remarks>The ordering ladder's <c>Above</c>/<c>Nominal</c> rung.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.Nominal, ComparisonType.GreaterThan, Landmark.Nominal)];
}
