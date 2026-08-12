using Calcusystem.Core;
using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.BaseModels;

public abstract class ComputedExpressionBase : IdBase
{
    public bool IsDirectlyMutable => false;

    // TODO: rename to `ErrorCorrelation`, with `ErrorPropagationMethod`. This says whether this node's operands
    // are correlated, not how error is propagated — the propagator is the `IErrorPropagator` a calculation
    // supplies. See the note on `ErrorPropagationMethod` for the full set of places a rename touches.
    public ErrorPropagationMethod ErrorPropagation { get; set; }

    protected ComputedExpressionBase(string id = Constants.CREATE_NEW_ID) : base(id)
    { }
}
