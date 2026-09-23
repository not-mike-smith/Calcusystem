using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies below the entire Rhs uncertainty interval — i.e.
/// Lhs.Upper &lt; Rhs.Lower. No overlap between the two intervals is permitted.
/// <br/><br/>
/// Symbol: <b>⌜&lt;⌟</b>
/// </summary>
public class DefinitelyLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.DefinitelyLessThan;

    public override string Symbol => "⌜<⌟";

    /// <summary><inheritdoc/></summary>
    /// <remarks>The ordering ladder's <c>Below</c>/<c>Certain</c> rung, though it is stated here as itself.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.UpperBound, MustBe.LessThan, Landmark.LowerBound)];
}

/// <summary>
/// Satisfied when the upper bound of Lhs is less than the upper bound of Rhs — i.e. Lhs.Upper &lt; Rhs.Upper.
/// The intervals may overlap; this is a weaker check than <see cref="DefinitelyLessThanOperator"/>.
/// <br/><br/>
/// Symbol: <b>⌜&lt;⌝</b>
/// </summary>
/// <remarks>
/// <b>Not part of the confidence ladder.</b> This compares a derived <i>statistic</i> of each side —
/// (i.e. ceiling against ceiling),
/// so it is not a tier of <see cref="OrderingLadder"/> and cannot be reached by strengthening or weakening one.
/// </remarks>
public class UpperBoundsLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.UpperBoundsLessThan;

    public override string Symbol => "⌜<⌝";

    /// <inheritdoc/>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.UpperBound, MustBe.LessThan, Landmark.UpperBound)];
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is less than the nominal Rhs value. Uncertainty is not part of
/// the ordering, though it still sets the scale at which the two values count as agreeing.
/// <br/><br/>
/// Symbol: <b>·&lt;·</b>
/// </summary>
public class NominallyLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.NominallyLessThan;

    public override string Symbol => "·<·";

    /// <summary><inheritdoc/></summary>
    /// <remarks>The ordering ladder's <c>Below</c>/<c>Nominal</c> rung.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.Nominal, MustBe.LessThan, Landmark.Nominal)];
}

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies above the entire Rhs uncertainty interval — i.e.
/// Lhs.Lower &gt; Rhs.Upper. No overlap between the two intervals is permitted.
/// <br/><br/>
/// Symbol: <b>⌞&gt;⌝</b>
/// </summary>
public class DefinitelyGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.DefinitelyGreaterThan;

    public override string Symbol => "⌞>⌝";

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// The ordering ladder's <c>Above</c>/<c>Certain</c> rung, and written out rather than derived from the
    /// less-than rule: "my floor is above your ceiling" is what this operator checks, and saying so directly
    /// beats making a reader apply a mirroring convention to find out.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.LowerBound, MustBe.GreaterThan, Landmark.UpperBound)];
}

/// <summary>
/// Satisfied when the lower bound of Lhs is greater than the lower bound of Rhs — i.e. Lhs.Lower &gt; Rhs.Lower.
/// The intervals may overlap; this is a weaker check than <see cref="DefinitelyGreaterThanOperator"/>.
/// <br/><br/>
/// Symbol: <b>⌞&gt;⌟</b>
/// </summary>
/// <remarks>
/// <b>Not part of the confidence ladder.</b> This compares a derived <i>statistic</i> of each side —
/// (i.e. floor against floor),
/// so it is not a tier of <see cref="OrderingLadder"/> and cannot be reached by strengthening or weakening one.
/// </remarks>
public class LowerBoundsGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.LowerBoundsGreaterThan;

    public override string Symbol => "⌞>⌟";

    /// <inheritdoc/>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.LowerBound, MustBe.GreaterThan, Landmark.LowerBound)];
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is greater than the nominal Rhs value. Uncertainty is not part
/// of the ordering, though it still sets the scale at which the two values count as agreeing.
/// <br/><br/>
/// Symbol: <b>·&gt;·</b>
/// </summary>
public class NominallyGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.NominallyGreaterThan;

    public override string Symbol => "·>·";

    /// <summary><inheritdoc/></summary>
    /// <remarks>The ordering ladder's <c>Above</c>/<c>Nominal</c> rung.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
        [new(Landmark.Nominal, MustBe.GreaterThan, Landmark.Nominal)];
}
