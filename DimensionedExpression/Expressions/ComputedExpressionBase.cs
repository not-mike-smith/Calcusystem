using Calcusystem.Core.Identity;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.Expressions;

public abstract class ComputedExpressionBase : ExpressionBase
{
    /// <inheritdoc/>
    public override bool IsDirectlyMutable => false;

    public UncertaintyCorrelation UncertaintyCorrelation { get; set; }

    protected ComputedExpressionBase(string id = Constants.CREATE_NEW_ID) : base(id)
    { }
}
