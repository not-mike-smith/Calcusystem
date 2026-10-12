using Calcusystem.Authoring.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Authoring.Drafts;

/// <summary>
/// One expression node under construction. Every field is optional and nothing is checked when it is set, so a
/// node can sit in any half-finished state an editor can put it in.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Id"/> is fixed when the node is created, before it is valid or even typed, so a front-end has
/// something to reference a node by from the moment it appears. <b>That id carries through to the built
/// expression</b>, so references held outside the draft stay good across a build.
/// </para>
/// <para>
/// One type covers all nine node kinds, with the variable-only and computed-only fields alongside each other.
/// Changing <see cref="Type"/> therefore leaves whichever fields no longer apply in place, ignored rather than
/// cleared — so flipping a node to another kind and back loses nothing.
/// </para>
/// </remarks>
public sealed class ExpressionDraft
{
    internal ExpressionDraft(string id) => Id = id;

    /// <summary>Identifies this node, and the expression built from it. Fixed at creation.</summary>
    public string Id { get; }

    /// <summary>Which kind of expression to build, or null if the author has not chosen yet.</summary>
    public ExpressionDraftType? Type { get; set; }

    /// <summary>
    /// The ids of this node's operands, in order. A null or blank entry is a slot the author has opened but not
    /// filled.
    /// </summary>
    /// <remarks>
    /// Order matters where the kind cares: a <see cref="ExpressionDraftType.Quotient"/> divides the first by the
    /// second. Ignored entirely for <see cref="ExpressionDraftType.Variable"/>.
    /// </remarks>
    public List<string?> OperandIds { get; } = [];

    /// <summary>What to call a variable. Ignored for every other kind.</summary>
    public string? Symbol { get; set; }

    /// <summary>
    /// A variable's dimensionality. Required to build one, and ignored for every other kind, which derive theirs
    /// from their operands.
    /// </summary>
    public Dimensionality? Dimensionality { get; set; }

    /// <summary>A variable's value, or null for one left unset. Ignored for every other kind.</summary>
    /// <remarks>
    /// A value whose dimensionality disagrees with <see cref="Dimensionality"/> is reported when the draft is
    /// built, not when it is set here.
    /// </remarks>
    public Measurand? Value { get; set; }

    /// <summary>Optional record of where a variable's value came from. Ignored for every other kind.</summary>
    public IProvenance? Provenance { get; set; }

    /// <summary>Whether a computed node's operands are correlated. Ignored for a variable.</summary>
    public UncertaintyCorrelation UncertaintyCorrelation { get; set; } = UncertaintyCorrelation.Uncorrelated;

    public override string ToString() =>
        $"{Type?.ToString() ?? "untyped"} {Symbol ?? Id}";
}
