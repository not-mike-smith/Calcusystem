using Calcusystem.Authoring.Enums;

namespace Calcusystem.Authoring.Drafts;

/// <summary>
/// One reason a draft cannot be built, carrying the id of the node it is about so an editor can point at it.
/// </summary>
/// <param name="Kind">What sort of problem this is.</param>
/// <param name="NodeId">The draft node at fault, or null for a problem about the draft as a whole.</param>
/// <param name="Message">What is wrong, in a form fit to show someone.</param>
public sealed record DraftProblem(DraftProblemKind Kind, string? NodeId, string Message)
{
    public override string ToString() =>
        NodeId is null ? $"{Kind}: {Message}" : $"{Kind} [{NodeId}]: {Message}";
}
