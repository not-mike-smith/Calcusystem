using Calcusystem.Analysis.Extensions;
using Calcusystem.Api.Reports;
using Calcusystem.Authoring.Drafts;
using Calcusystem.Core.Either;
using Calcusystem.Serialization.Mappers;
using Dtos = Calcusystem.Serialization.Dtos;

namespace Calcusystem.Api.Workflows;

/// <summary>
/// What the endpoints do with a posted draft. The only place in this assembly that names
/// <c>Calcusystem.Analysis</c>.
/// </summary>
/// <remarks>
/// Every method takes a draft and nothing else, because the client owns the draft and the server keeps nothing
/// between requests.
/// </remarks>
public sealed class DraftWorkflow
{
    /// <summary>What stands between <paramref name="draft"/> and a built system. Empty if nothing does.</summary>
    public IReadOnlyList<DraftProblem> Problems(SystemDraft draft) => draft.Validate();

    /// <summary>The built system in its saved form, or the problems that stopped it.</summary>
    public Either<Dtos.ExpressionSystem> Build(SystemDraft draft) =>
        draft.Build().Map(system => new SerializingMapper().Map(system));

    /// <summary>Which parts of the built system can be solved, or the problems that stopped the build.</summary>
    /// <remarks>Flattened once and then decomposed, so the count and the parts come from one pass.</remarks>
    public Either<DecompositionReport> Decompose(SystemDraft draft) =>
        draft.Build().Map(system =>
        {
            var flat = system.Flatten();
            return Reporting.Report(flat, flat.Decompose());
        });

    /// <summary>What the built system computes, or the problems that stopped the build.</summary>
    /// <remarks>
    /// Values come from the draft's own nodes. There is no per-request override, which a draft does not need —
    /// an editor sets a value on the node and posts the draft again.
    /// </remarks>
    public Either<CalculationReport> Calculate(SystemDraft draft) =>
        draft.Build().Map(system => Reporting.Report(system.Calculate()));
}
