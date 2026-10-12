using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;

namespace Calcusystem.Authoring.Drafts;

/// <summary>
/// One relationship under construction. Every field is optional and nothing is checked when it is set.
/// </summary>
/// <remarks>
/// Separate from <see cref="ExpressionDraft"/> because the domain separates them: a relationship has two named
/// sides rather than a list of operands, and carries a solving role no expression has.
/// </remarks>
public sealed class RelationshipDraft
{
    internal RelationshipDraft(string id) => Id = id;

    /// <summary>Identifies this relationship, and the operator built from it. Fixed at creation.</summary>
    public string Id { get; }

    /// <summary>Which of the operators to build, or null if the author has not chosen yet.</summary>
    public BinaryOperatorType? Type { get; set; }

    /// <summary>The id of the left operand — the value under test, where the relationship distinguishes sides.</summary>
    public string? LhsId { get; set; }

    /// <summary>The id of the right operand — what it is tested against, where the relationship distinguishes sides.</summary>
    public string? RhsId { get; set; }

    /// <summary>
    /// What this relationship does to the problem. Only an equality can be anything but
    /// <see cref="SolvingRole.Requirement"/>, and an equality must say which it is.
    /// </summary>
    public SolvingRole? SolvingRole { get; set; }

    /// <summary>How strictly an equality reads "equal". Required for an equality, ignored otherwise.</summary>
    public AgreementRule? Agreement { get; set; }

    /// <summary>
    /// The single comparison a <see cref="BinaryOperatorType.SimpleComparison"/> asserts. Required for one,
    /// ignored otherwise.
    /// </summary>
    public ComparisonRule? Rule { get; set; }

    /// <summary>Optional label for this relationship.</summary>
    public string? Name { get; set; }

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional record of where this relationship came from — a citation for a constitutive equation.</summary>
    public IProvenance? Provenance { get; set; }

    public override string ToString() =>
        $"{Type?.ToString() ?? "untyped"} {Name ?? Id}";
}
