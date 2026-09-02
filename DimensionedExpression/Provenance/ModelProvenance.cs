using Calcusystem.Core.Identity;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.Provenance;

/// <summary>
/// Provenance for an empirically fitted constant within a constitutive relationship (model-specific, not a
/// physical property). Construct via <see cref="ProvenanceFactory.Model"/>.
/// </summary>
public sealed class ModelProvenance : IdBase, IProvenance
{
    internal ModelProvenance(string modelName, string? fittingReference, string id)
        : base(id)
    {
        ModelName = modelName;
        FittingReference = fittingReference;
    }

    internal string ModelName { get; }
    internal string? FittingReference { get; }

    public string Summary() =>
        $"Model parameter: {ModelName}{(FittingReference is null ? "" : $" (fit {FittingReference})")}";

    ProvenanceSnapshot IProvenance.GetSnapshot() =>
        ProvenanceSnapshot.Model(Id, ModelName, FittingReference);
}
