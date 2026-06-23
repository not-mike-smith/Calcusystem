using DimensionedExpression.BaseModels;

namespace DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies strictly below the entire Rhs uncertainty
/// interval — i.e. Lhs.Upper &lt; Rhs.Lower. No overlap between the two intervals is permitted.
/// Symbol: &lt;&lt;
/// Use for definitive less-than checks where even worst-case Lhs must remain below best-case Rhs.
/// </summary>
public class DefinitelyLessThanOperator : NonCommutativeOperatorBase
{
    public override string Symbol => "<<";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        var lhs = Lhs.Value!;
        var rhs = Rhs.Value!;
        return lhs.KmsValue + lhs.KmsUpperAbsoluteError < rhs.KmsValue - rhs.KmsLowerAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the upper bound of Lhs is strictly less than the upper bound of Rhs — i.e.
/// Lhs.Upper &lt; Rhs.Upper. The intervals may overlap; this is a weaker check than
/// <see cref="DefinitelyLessThanOperator"/>.
/// Symbol: &lt;^
/// Use when you need to know that Lhs's worst-case high value is bounded by Rhs's worst-case high value.
/// </summary>
public class UpperBoundsLessThanOperator : NonCommutativeOperatorBase
{
    public override string Symbol => "<^";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        var lhs = Lhs.Value!;
        var rhs = Rhs.Value!;
        return lhs.KmsValue + lhs.KmsUpperAbsoluteError < rhs.KmsValue + rhs.KmsUpperAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is strictly less than the nominal Rhs value.
/// Uncertainty is ignored entirely.
/// Symbol: &lt;~
/// Use when only the reported values matter and measurement uncertainty is not part of the check.
/// </summary>
public class NominallyLessThanOperator : NonCommutativeOperatorBase
{
    public override string Symbol => "<~";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        return Lhs.Value!.KmsValue < Rhs.Value!.KmsValue;
    }
}

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval lies strictly above the entire Rhs uncertainty
/// interval — i.e. Lhs.Lower &gt; Rhs.Upper. No overlap between the two intervals is permitted.
/// Symbol: &gt;&gt;
/// Use for definitive greater-than checks where even worst-case Lhs must remain above best-case Rhs.
/// </summary>
public class DefinitelyGreaterThanOperator : NonCommutativeOperatorBase
{
    public override string Symbol => ">>";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        var lhs = Lhs.Value!;
        var rhs = Rhs.Value!;
        return lhs.KmsValue - lhs.KmsLowerAbsoluteError > rhs.KmsValue + rhs.KmsUpperAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the lower bound of Lhs is strictly greater than the lower bound of Rhs — i.e.
/// Lhs.Lower &gt; Rhs.Lower. The intervals may overlap; this is a weaker check than
/// <see cref="DefinitelyGreaterThanOperator"/>.
/// Symbol: &gt;v
/// Use when you need to know that Lhs's worst-case low value is above Rhs's worst-case low value.
/// </summary>
public class LowerBoundsGreaterThanOperator : NonCommutativeOperatorBase
{
    public override string Symbol => ">v";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        var lhs = Lhs.Value!;
        var rhs = Rhs.Value!;
        return lhs.KmsValue - lhs.KmsLowerAbsoluteError > rhs.KmsValue - rhs.KmsLowerAbsoluteError;
    }
}

/// <summary>
/// Satisfied when the nominal (center) Lhs value is strictly greater than the nominal Rhs value.
/// Uncertainty is ignored entirely.
/// Symbol: &gt;~
/// Use when only the reported values matter and measurement uncertainty is not part of the check.
/// </summary>
public class NominallyGreaterThanOperator : NonCommutativeOperatorBase
{
    public override string Symbol => ">~";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        return Lhs.Value!.KmsValue > Rhs.Value!.KmsValue;
    }
}
