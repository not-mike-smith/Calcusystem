namespace Calcusystem.Core.Interfaces;

/// <summary>
/// A type that can hand out the complete state defining an instance, and rebuild an instance from that state.
/// This is the seam persistence layers use instead of reaching for the type's internals.
/// </summary>
/// <remarks>
/// <para>
/// <typeparamref name="TSnapshot"/> is a plain state record — a <i>memento</i>. It deliberately carries no format
/// concerns: no type discriminator, no schema version, no encoding. The owning assembly answers "what state
/// defines this object"; the persistence layer answers "how is that state encoded, versioned, and migrated".
/// Keeping those two questions apart is the whole point of this interface — a DTO here would drag the file
/// format into the domain.
/// </para>
/// <para>
/// Use this when an instance can be rebuilt from its own state alone. When rebuilding requires resolving
/// references to <i>other</i> objects, use <see cref="ISnapshottingNode{TSelf,TSnapshot}"/> instead. The axis is
/// whether reconstruction needs outside help — not where the type sits in any tree. A leaf variable holding only
/// its own value is stateful in this sense; a product referring to its factors by id is not.
/// </para>
/// <para>
/// Only closed, single-implementation types use this. A polymorphic hierarchy cannot: the concrete type is
/// chosen by inspecting the state, so reconstruction has to be a static gateway over the closed set rather than
/// a per-type <c>static abstract</c>. Uncertainty and provenance are both handled that way, by a factory.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TSnapshot">The state record that fully describes an instance.</typeparam>
public interface ISnapshotting<TSelf, TSnapshot> where TSelf : ISnapshotting<TSelf, TSnapshot>
{
    /// <summary>Returns the complete state defining this instance.</summary>
    TSnapshot GetSnapshot();

    /// <summary>Rebuilds an instance from previously captured state. Not part of the normal construction API.</summary>
    static abstract TSelf FromSnapshot(TSnapshot state);
}
