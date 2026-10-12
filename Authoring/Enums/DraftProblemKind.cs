namespace Calcusystem.Authoring.Enums;

/// <summary>
/// Why a draft cannot be built, in a form a front-end can branch on without reading the message.
/// </summary>
/// <remarks>
/// Everything but <see cref="DomainRule"/> is a shape the draft layer checks itself, because the domain types
/// cannot be called at all until the shape is right. <see cref="DomainRule"/> is the domain's own refusal,
/// relayed.
/// </remarks>
public enum DraftProblemKind
{
    /// <summary>No kind of expression has been chosen for the node yet.</summary>
    TypeNotChosen = 1,

    /// <summary>An operand slot is empty.</summary>
    OperandMissing,

    /// <summary>The node has the wrong number of operands for its kind.</summary>
    OperandCountWrong,

    /// <summary>An operand names an id the draft does not hold.</summary>
    ReferenceNotFound,

    /// <summary>The node is its own operand, directly or through others.</summary>
    CircularReference,

    /// <summary>Two nodes in the draft claim the same id.</summary>
    DuplicateId,

    /// <summary>A variable has no dimensionality, which it cannot be constructed without.</summary>
    DimensionalityMissing,

    /// <summary>A relationship kind needs data it was not given — an agreement rule, or a comparison rule.</summary>
    RelationshipRuleMissing,

    /// <summary>
    /// The domain refused to build the node or to accept it. The message is the exception's, so the rule is
    /// stated wherever the domain states it rather than restated here.
    /// </summary>
    DomainRule,
}
