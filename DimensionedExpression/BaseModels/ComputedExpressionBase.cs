using Calcusystem.Core;
using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.BaseModels;

public abstract class ComputedExpressionBase : IdBase
{
    public bool IsDirectlyMutable => false;

    public ErrorPropagationMethod ErrorPropagation { get; set; }

    protected ComputedExpressionBase(string id = Constants.CREATE_NEW_ID) : base(id)
    { }
}
