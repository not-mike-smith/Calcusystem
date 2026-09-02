using Calcusystem.Core.Identity;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.Provenance;

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

    ProvenanceSnapshot IProvenance.GetSnapshot() =>
        ProvenanceSnapshot.Reference(Id, Citation, Url, Year);
}
