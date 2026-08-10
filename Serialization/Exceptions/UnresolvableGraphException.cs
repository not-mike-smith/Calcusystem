namespace Calcusystem.Serialization.Exceptions;

/// <summary>
/// Deserialization could not finish because the remaining expressions can never be built: something they
/// reference is absent from the payload, or they depend on each other in a cycle.
/// </summary>
/// <remarks>
/// <para>
/// The rebuild loop defers an expression whose children are not loaded yet and retries it later. That terminates
/// only if every deferral eventually becomes buildable. When a full pass over the queue produces no progress,
/// nothing further can change, and the loop would otherwise spin forever — not even overflowing the stack, since
/// the retry is iterative.
/// </para>
/// <para>
/// <see cref="MissingIds"/> and <see cref="CyclicIds"/> separate the two causes: an id referenced but absent from
/// the payload is a dangling reference, whereas one that is present but itself still unbuilt is part of a cycle.
/// Expression trees are acyclic by construction, so a cycle means the payload was produced by something other
/// than <c>SerializingMapper</c>, or was edited afterwards.
/// </para>
/// </remarks>
public class UnresolvableGraphException : Exception
{
    /// <summary>Ids of the expressions that could not be built.</summary>
    public readonly IReadOnlyList<string> UnbuiltIds;

    /// <summary>Referenced ids that appear nowhere in the payload.</summary>
    public readonly IReadOnlyList<string> MissingIds;

    /// <summary>Referenced ids that are present but themselves unbuilt — the cycle.</summary>
    public readonly IReadOnlyList<string> CyclicIds;

    public UnresolvableGraphException(
        IReadOnlyList<string> unbuiltIds,
        IReadOnlyList<string> missingIds,
        IReadOnlyList<string> cyclicIds)
        : base(BuildMessage(unbuiltIds, missingIds, cyclicIds))
    {
        UnbuiltIds = unbuiltIds;
        MissingIds = missingIds;
        CyclicIds = cyclicIds;
    }

    private static string BuildMessage(
        IReadOnlyList<string> unbuiltIds,
        IReadOnlyList<string> missingIds,
        IReadOnlyList<string> cyclicIds)
    {
        var reasons = new List<string>();
        if (missingIds.Count > 0)
            reasons.Add($"referenced ids absent from the payload: {string.Join(", ", missingIds)}");
        if (cyclicIds.Count > 0)
            reasons.Add($"ids in a reference cycle: {string.Join(", ", cyclicIds)}");
        if (reasons.Count == 0)
            reasons.Add("no referenced id could be resolved");

        return $"Could not deserialize {unbuiltIds.Count} expression(s) ({string.Join(", ", unbuiltIds)}) — "
               + string.Join("; ", reasons)
               + ".";
    }
}
