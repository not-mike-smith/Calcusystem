namespace Calcusystem.Core;

/// <summary>
/// Anything carrying a stable string identity that survives persistence.
/// </summary>
/// <remarks>
/// <para>
/// The id is what lets a flattened graph rebuild its references: parents name their children by id rather than
/// containing them, so identity has to outlive a round trip. <see cref="IdBase"/> is the usual implementation.
/// </para>
/// <para>
/// Being identified does not imply being <i>referenceable</i>. Provenance has an id that round-trips for
/// fidelity, but it is owned inline by a single node and never named by another.
/// </para>
/// </remarks>
public interface IIdentified
{
    /// <summary>Stable identity, preserved across serialization.</summary>
    string Id { get; }
}
