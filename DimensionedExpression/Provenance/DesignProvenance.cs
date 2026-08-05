using Calcusystem.Core;
using DimensionedExpression.Interfaces;
using DimensionedExpression.State;

namespace DimensionedExpression.Provenance;

/// <summary>
/// Provenance for an engineer-specified value; any tolerance lives in the variable's uncertainty, not here.
/// Construct via <see cref="ProvenanceFactory.Design"/>.
/// </summary>
public sealed class DesignProvenance : IdBase, IProvenance
{
    internal DesignProvenance(string? specReference, string id)
        : base(id)
    {
        SpecReference = specReference;
    }

    internal string? SpecReference { get; }

    public string Summary() =>
        $"Design parameter{(SpecReference is null ? "" : $" (spec {SpecReference})")}";

    ProvenanceState IProvenance.GetState() =>
        ProvenanceState.Design(Id, SpecReference);
}
