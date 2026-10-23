namespace Calcusystem.Api.Reports;

/// <summary>What a calculation produced, and what it could not.</summary>
/// <param name="IsComplete">Whether every expression the system references produced a value.</param>
/// <param name="AllRelationshipsHold">Whether no relationship was judged unsatisfied.</param>
/// <param name="Values">Every node that resolved, by id.</param>
/// <param name="UnresolvedIds">Nodes that produced no value, because something beneath them is unset.</param>
/// <param name="MissingValueIds">The unset variables responsible. Supply these and more will resolve.</param>
/// <param name="Outcomes">One entry per relationship, including the ones that could not be judged.</param>
public sealed record CalculationReport(
    bool IsComplete,
    bool AllRelationshipsHold,
    IReadOnlyList<ValueReport> Values,
    IReadOnlyList<string> UnresolvedIds,
    IReadOnlyList<string> MissingValueIds,
    IReadOnlyList<OutcomeReport> Outcomes);

/// <summary>The value computed for one node.</summary>
public sealed record ValueReport(string Id, MeasurandReport Value);

/// <summary>What one relationship did.</summary>
/// <param name="RelationshipId">Which relationship.</param>
/// <param name="Symbol">Its operator symbol, as the model shows it.</param>
/// <param name="IsSatisfied">True, false, or null where it could not be judged.</param>
/// <param name="IsViolation">Unsatisfied, and it has a criterion — a requirement the model breaks.</param>
/// <param name="IsInconsistency">Unsatisfied with no criterion — two routes to one quantity disagreeing.</param>
/// <param name="Lhs">The left operand's value, or null if it did not resolve.</param>
/// <param name="Rhs">The right operand's value, or null if it did not resolve.</param>
public sealed record OutcomeReport(
    string RelationshipId,
    string Symbol,
    bool? IsSatisfied,
    bool IsViolation,
    bool IsInconsistency,
    MeasurandReport? Lhs,
    MeasurandReport? Rhs);
