using Calcusystem.Authoring.Drafts;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Snapshots;

namespace Calcusystem.Serialization.Mappers;

/// <summary>
/// Maps a <see cref="SystemDraft"/> to its wire shape and back.
/// </summary>
/// <remarks>
/// <para>
/// Static and stateless in both directions, unlike the system mappers: a draft is already flat and already
/// references by id, so there is no graph to rebuild, no ordering to choose, and no resolver to thread.
/// </para>
/// <para>
/// A draft is freely invalid, and so is its wire shape — an untyped node, an empty operand slot and a reference
/// to nothing all survive a round trip. What is refused is a payload that cannot be <i>held</i> as a draft at
/// all; see <see cref="FromWire"/>.
/// </para>
/// </remarks>
public static class DraftMapper
{
    /// <summary>The wire shape of <paramref name="draft"/>.</summary>
    public static Dtos.SystemDraft ToWire(SystemDraft draft) => new()
    {
        Id = draft.Id,
        Name = draft.Name,
        Description = draft.Description,
        Expressions = draft.Expressions.Select(ToWire).ToList(),
        Relationships = draft.Relationships.Select(ToWire).ToList(),
    };

    /// <summary>The draft <paramref name="wire"/> describes.</summary>
    /// <exception cref="FormatException">
    /// Two nodes in one list claim the same id, which a draft cannot hold — one would displace the other. An id
    /// shared by an expression and a relationship is held, and reported as a <c>DuplicateId</c> problem.
    /// </exception>
    /// <exception cref="InvalidOperationException">An uncertainty is missing a field its shape requires.</exception>
    /// <exception cref="NotImplementedException">A provenance or uncertainty <c>Type</c> names no known kind.</exception>
    public static SystemDraft FromWire(Dtos.SystemDraft wire)
    {
        RefuseRepeatedIds(wire.Expressions.Select(e => e.Id), nameof(wire.Expressions));
        RefuseRepeatedIds(wire.Relationships.Select(r => r.Id), nameof(wire.Relationships));

        var draft = SystemDraft.Create(wire.Name ?? "", wire.Description ?? "", wire.Id);

        foreach (var expression in wire.Expressions) Read(draft, expression);
        foreach (var relationship in wire.Relationships) Read(draft, relationship);

        return draft;
    }

    private static Dtos.ExpressionDraft ToWire(ExpressionDraft draft) => new()
    {
        Id = draft.Id,
        Type = draft.Type,
        OperandIds = [.. draft.OperandIds],
        Symbol = draft.Symbol,
        Dimensionality = Encode(draft.Dimensionality),
        KmsValue = draft.Value?.KmsValue,
        ValueDimensionality = DivergentValueDimensionality(draft),
        Uncertainty = draft.Value is { } value ? UncertaintyCodec.ToWire(value.Uncertainty.GetSnapshot()) : null,
        ValueUnit = draft.ValueUnit,
        Provenance = draft.Provenance is { } provenance ? ProvenanceCodec.ToWire(provenance.GetSnapshot()) : null,
        UncertaintyCorrelation = draft.UncertaintyCorrelation,
    };

    private static Dtos.RelationshipDraft ToWire(RelationshipDraft draft) => new()
    {
        Id = draft.Id,
        Type = draft.Type,
        LhsId = draft.LhsId,
        RhsId = draft.RhsId,
        SolvingRole = draft.SolvingRole,
        Agreement = draft.Agreement,
        RuleLhs = draft.Rule?.Lhs,
        RuleMustBe = draft.Rule?.MustBe,
        RuleRhs = draft.Rule?.Rhs,
        Name = draft.Name,
        Description = draft.Description,
        Provenance = draft.Provenance is { } provenance ? ProvenanceCodec.ToWire(provenance.GetSnapshot()) : null,
    };

    private static void Read(SystemDraft draft, Dtos.ExpressionDraft wire)
    {
        var node = draft.AddExpression(wire.Type, wire.Id);

        node.Symbol = wire.Symbol;
        node.Dimensionality = Decode(wire.Dimensionality);
        node.UncertaintyCorrelation = wire.UncertaintyCorrelation ?? UncertaintyCorrelation.Uncorrelated;
        node.Provenance = ProvenanceCodec.FromWire(wire.Provenance) is { } snapshot
            ? ProvenanceFactory.FromSnapshot(snapshot)
            : null;
        node.Value = ValueOf(wire);
        node.ValueUnit = wire.ValueUnit;

        foreach (var operandId in wire.OperandIds) node.OperandIds.Add(operandId);
    }

    private static void Read(SystemDraft draft, Dtos.RelationshipDraft wire)
    {
        var node = draft.AddRelationship(wire.Type, wire.Id);

        node.LhsId = wire.LhsId;
        node.RhsId = wire.RhsId;
        node.SolvingRole = wire.SolvingRole;
        node.Agreement = wire.Agreement;
        node.Rule = wire is { RuleLhs: { } lhs, RuleMustBe: { } mustBe, RuleRhs: { } rhs }
            ? new ComparisonRule(lhs, mustBe, rhs)
            : null;
        node.Name = wire.Name;
        node.Description = wire.Description;
        node.Provenance = ProvenanceCodec.FromWire(wire.Provenance) is { } snapshot
            ? ProvenanceFactory.FromSnapshot(snapshot)
            : null;
    }

    /// <remarks>
    /// A value with no dimensionality written anywhere is read as dimensionless, which is what the draft then
    /// reports as a missing dimensionality rather than building.
    /// </remarks>
    private static Measurand? ValueOf(Dtos.ExpressionDraft wire)
    {
        if (wire.KmsValue is not { } kms) return null;

        var dimensionality = DimensionalityCodec.Decode(wire.ValueDimensionality ?? wire.Dimensionality);

        return Measurand.FromSnapshot(
            new MeasurandSnapshot(
                new QuantitySnapshot(kms, dimensionality),
                UncertaintyCodec.FromWire(wire.Uncertainty)));
    }

    /// <summary>The value's own dimensionality, where reading the node would not arrive at it anyway.</summary>
    /// <remarks>
    /// A draft can hold a value whose dimensionality disagrees with the one declared on the node — setting one
    /// and then changing the other reaches it, and the build reports it. Writing only the declared one would
    /// mend that on the way through, so the draft that came back would build where the original did not.
    /// </remarks>
    private static string? DivergentValueDimensionality(ExpressionDraft draft)
    {
        if (draft.Value is not { } value) return null;

        var declared = draft.Dimensionality ?? Dimensionality.Dimensionless;

        return value.Dimensionality == declared ? null : Encode(value.Dimensionality);
    }

    private static string? Encode(Dimensionality? dimensionality) =>
        dimensionality is { } present ? DimensionalityCodec.Encode(present.GetSnapshot()) : null;

    private static Dimensionality? Decode(string? encoded) =>
        encoded is null ? null : Dimensionality.FromSnapshot(DimensionalityCodec.Decode(encoded));

    /// <remarks>
    /// <c>AddExpression</c> and <c>AddRelationship</c> key by id, so a repeated one would overwrite the node
    /// already held and the draft that came back would be missing a node nobody deleted.
    /// </remarks>
    private static void RefuseRepeatedIds(IEnumerable<string?> ids, string list)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in ids)
        {
            if (id is not null && ! seen.Add(id))
                throw new FormatException($"Two nodes in {list} claim the id '{id}'.");
        }
    }
}
