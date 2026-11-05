using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Examples.Presentation;

/// <summary>
/// One line of a worked example's report: a node, the unit to show it in, and what to call it.
/// </summary>
/// <remarks>
/// A <c>Measurand</c> does not remember the unit it was authored in — storage is always KMS — so the
/// unit a reader should see is carried here rather than recovered from the value.
/// </remarks>
/// <param name="Label">The name this quantity goes by in the problem statement.</param>
/// <param name="Expression">The node to read. A <c>Variable</c> for a given, a computed node for a result.</param>
/// <param name="Unit">The unit to convert into. Must match the node's dimensionality.</param>
/// <param name="Note">Where the number came from, or what it is for. Empty when the label says enough.</param>
public sealed record Readout(
    string Label,
    IExpression Expression,
    UnitOfMeasure Unit,
    string Note = "");
