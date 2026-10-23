using Calcusystem.Authoring.Enums;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.Serialization.Dtos;

/// <summary>The wire shape of a draft: a document root holding its nodes, with every field optional.</summary>
public class SystemDraft
{
    /// <summary>The id to rebuild the draft under, or null for a fresh one.</summary>
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    /// <summary>The expression nodes, in no particular order.</summary>
    public List<ExpressionDraft> Expressions { get; init; } = [];

    /// <summary>The relationship nodes, in no particular order.</summary>
    public List<RelationshipDraft> Relationships { get; init; } = [];
}

/// <summary>The wire shape of one expression node under construction.</summary>
public class ExpressionDraft
{
    /// <summary>The id to rebuild the node under, or null for a fresh one.</summary>
    public string? Id { get; init; }

    /// <summary>Which kind of expression, or null if the author has not chosen one.</summary>
    public ExpressionDraftType? Type { get; init; }

    /// <summary>The operands, in order, where a null entry is a slot opened but not filled.</summary>
    public List<string?> OperandIds { get; init; } = [];

    public string? Symbol { get; init; }

    /// <summary>The declared dimensionality, encoded as by <see cref="Mappers.DimensionalityCodec"/>.</summary>
    public string? Dimensionality { get; init; }

    /// <summary>The value, in KMS units, or null if none has been supplied.</summary>
    public double? KmsValue { get; init; }

    /// <summary>
    /// The dimensionality of <see cref="KmsValue"/>, written only where it differs from
    /// <see cref="Dimensionality"/>. Null means the value carries the declared one.
    /// </summary>
    public string? ValueDimensionality { get; init; }

    /// <summary>The uncertainty on <see cref="KmsValue"/>, or null for none.</summary>
    public Uncertainty? Uncertainty { get; init; }

    public Provenance? Provenance { get; init; }

    /// <summary>How this node's operands correlate, or null for uncorrelated.</summary>
    public UncertaintyCorrelation? UncertaintyCorrelation { get; init; }
}

/// <summary>The wire shape of one relationship under construction.</summary>
public class RelationshipDraft
{
    /// <summary>The id to rebuild the node under, or null for a fresh one.</summary>
    public string? Id { get; init; }

    /// <summary>Which operator, or null if the author has not chosen one.</summary>
    public BinaryOperatorType? Type { get; init; }

    public string? LhsId { get; init; }

    public string? RhsId { get; init; }

    public SolvingRole? SolvingRole { get; init; }

    /// <summary>What an equality takes as agreement, required by the equality kinds only.</summary>
    public AgreementRule? Agreement { get; init; }

    /// <summary>The comparison rule's left landmark. All three rule fields or none.</summary>
    public Landmark? RuleLhs { get; init; }

    public MustBe? RuleMustBe { get; init; }

    public Landmark? RuleRhs { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public Provenance? Provenance { get; init; }
}
