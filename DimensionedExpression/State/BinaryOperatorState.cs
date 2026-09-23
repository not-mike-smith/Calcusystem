using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;

namespace Calcusystem.DimensionedExpression.State;

/// <summary>
/// The complete stored state of any binary operator. Every operator has the same shape — two operand references
/// plus annotations — so one record with a <see cref="Kind"/> discriminator covers all thirteen.
/// </summary>
/// <param name="Kind">Which operator this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="LhsId">Id of the left-hand expression.</param>
/// <param name="RhsId">Id of the right-hand expression.</param>
/// <param name="SolvingRole">
/// What this relationship does to the problem. Stored as the role rather than as the derived
/// <c>IsDetermining</c> boolean, because that flattens <c>Equation</c> and <c>Coherence</c> together and they
/// cannot be told apart again on load. Only the equality kind can store anything but
/// <see cref="DimensionedExpression.SolvingRole.Requirement"/>; for every other kind reconstruction ignores it,
/// because those types have no way to represent it.
/// </param>
/// <param name="Agreement">
/// How strictly an equality reads "equal", and null for every other kind. Stored rather than left to the
/// reader: without it the wire says a relationship is an equality and nothing about what equality means, so two
/// readers can reach opposite verdicts from identical bytes.
/// </param>
/// <param name="Rule">
/// The comparison a <see cref="BinaryOperatorKind.SimpleComparison"/> asserts, and null for every other kind,
/// whose rules are fixed by their type.
/// </param>
/// <param name="Name">Optional human-readable name.</param>
/// <param name="Description">Optional human-readable description.</param>
/// <param name="Provenance">Where the relationship came from (e.g. a citation), or null when untracked.</param>
public readonly record struct BinaryOperatorState(
    BinaryOperatorKind Kind,
    string Id,
    string LhsId,
    string RhsId,
    SolvingRole SolvingRole,
    AgreementRule? Agreement,
    ComparisonRule? Rule,
    string? Name,
    string? Description,
    ProvenanceState? Provenance);
