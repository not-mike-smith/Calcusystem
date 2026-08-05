using Calcusystem.Core;
using DimensionedExpression.Interfaces;
using DimensionedExpression.State;

namespace DimensionedExpression.Provenance;

/// <summary>
/// Provenance for a literature or tabulated value (physical constant, material/thermodynamic property).
/// Construct via <see cref="ProvenanceFactory.Reference"/>.
/// </summary>
public sealed class ReferenceProvenance : IdBase, IProvenance
{
    internal ReferenceProvenance(string citation, string? url, int? year, string id)
        : base(id)
    {
        Citation = citation;
        Url = url;
        Year = year;
    }

    internal string Citation { get; }
    internal string? Url { get; }
    internal int? Year { get; }

    public string Summary() =>
        $"Reference: {Citation}{(Year is null ? "" : $" ({Year})")}";

    ProvenanceState IProvenance.GetState() =>
        ProvenanceState.Reference(Id, Citation, Url, Year);
}
