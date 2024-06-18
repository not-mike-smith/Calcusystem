using Measurement;
using Measurement.Models;

namespace DimensionedExpression.BaseModels;

public abstract class CalculatedExpressionBase : IdBase
{
    public bool IsDirectlyMutable => false;

    public ErrorPropagationMethod ErrorPropagation { get; set; }

    protected CalculatedExpressionBase(string id = Constants.CREATE_NEW) : base(id)
    { }
}
