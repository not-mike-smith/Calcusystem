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

    /// <summary>A variable, a constant or a reference value has no dimensionality declared.</summary>
    DimensionalityMissing,

    /// <summary>
    /// A relationship kind needs data it was not given — an equality's agreement rule or solving role, or a
    /// simple comparison's rule.
    /// </summary>
    RelationshipRuleMissing,

    /// <summary>
    /// The domain refused to build the node or to accept it. The message is the exception's, so the rule is
    /// stated wherever the domain states it rather than restated here.
    /// </summary>
    DomainRule,

    /// <summary>
    /// A relationship other than an equality states a solving role other than
    /// <see cref="Calcusystem.DimensionedExpression.Enums.SolvingRole.Requirement"/>. Only an equality can determine
    /// a value.
    /// </summary>
    RoleNotAllowed,

    /// <summary>A variable, a constant or a reference value has no name.</summary>
    NameMissing,

    /// <summary>A node's name breaks the rule for names. The message says how.</summary>
    NameInvalid,

    /// <summary>Another node in the draft has the same name. Reported on each node that has it.</summary>
    DuplicateName,

    /// <summary>A constant, a reference value or a scalar has no value. It cannot be left unset.</summary>
    ValueMissing,

    /// <summary>
    /// A constant, a reference value or a scalar has a value its kind does not accept, or one whose
    /// dimensionality is not the one declared. The message says how.
    /// </summary>
    FixedValueInvalid,
}
