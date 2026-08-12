
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies strictly below the entire Rhs uncertainty
/// interval — i.e. Lhs.Upper &lt; Rhs.Lower. No overlap between the two intervals is permitted.
/// <br/>
/// Symbol: <b>&lt;&lt;</b>
/// <br/>
/// Use for definitive less-than checks where even worst-case Lhs must remain below best-case Rhs.
/// </summary>
public class DefinitelyLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.DefinitelyLessThan;

    public override string Symbol => "<<";

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        return lhs.KmsValue + lhs.KmsUpperAbsoluteError < rhs.KmsValue - rhs.KmsLowerAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the upper bound of Lhs is strictly less than the upper bound of Rhs — i.e.
/// Lhs.Upper &lt; Rhs.Upper. The intervals may overlap; this is a weaker check than
/// <see cref="DefinitelyLessThanOperator"/>.
/// <br/>
/// Symbol: <b>&lt;^</b>
/// <br/>
/// Use when you need to know that Lhs's worst-case high value is bounded by Rhs's worst-case high value.
/// </summary>
public class UpperBoundsLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.UpperBoundsLessThan;

    public override string Symbol => "<^";

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        return lhs.KmsValue + lhs.KmsUpperAbsoluteError < rhs.KmsValue + rhs.KmsUpperAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is strictly less than the nominal Rhs value.
/// Uncertainty is ignored entirely.
/// <br/>
/// Symbol: <b>&lt;~</b>
/// <br/>
/// Use when only the reported values matter and measurement uncertainty is not part of the check.
/// </summary>
public class NominallyLessThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.NominallyLessThan;

    public override string Symbol => "<~";

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        return lhs.KmsValue < rhs.KmsValue;
    }
}

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies strictly above the entire Rhs uncertainty
/// interval — i.e. Lhs.Lower &gt; Rhs.Upper. No overlap between the two intervals is permitted.
/// <br/>
/// Symbol: <b>&gt;&gt;</b>
/// <br/>
/// Use for definitive greater-than checks where even worst-case Lhs must remain above best-case Rhs.
/// </summary>
public class DefinitelyGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.DefinitelyGreaterThan;

    public override string Symbol => ">>";

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        return lhs.KmsValue - lhs.KmsLowerAbsoluteError > rhs.KmsValue + rhs.KmsUpperAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the lower bound of Lhs is strictly greater than the lower bound of Rhs — i.e.
/// Lhs.Lower &gt; Rhs.Lower. The intervals may overlap; this is a weaker check than
/// <see cref="DefinitelyGreaterThanOperator"/>.
/// <br/>
/// Symbol: <b>&gt;v</b>
/// <br/>
/// Use when you need to know that Lhs's worst-case low value is above Rhs's worst-case low value.
/// </summary>
public class LowerBoundsGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.LowerBoundsGreaterThan;

    public override string Symbol => ">v";

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        return lhs.KmsValue - lhs.KmsLowerAbsoluteError > rhs.KmsValue - rhs.KmsLowerAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is strictly greater than the nominal Rhs value.
/// Uncertainty is ignored entirely.
/// <br/>
/// Symbol: <b>&gt;~</b>
/// <br/>
/// Use when only the reported values matter and measurement uncertainty is not part of the check.
/// </summary>
public class NominallyGreaterThanOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.NominallyGreaterThan;

    public override string Symbol => ">~";

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        return lhs.KmsValue > rhs.KmsValue;
    }
}
