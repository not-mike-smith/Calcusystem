namespace Calcusystem.Core.Interfaces;

/// <summary>
/// A type that can hand out the complete data defining an instance, and rebuild an instance from it.
/// This is the seam persistence layers use instead of reaching for the type's internals.
/// </summary>
/// <remarks>
/// <para>
/// <typeparamref name="TSnapshot"/> is a plain record carrying no format concerns: no type discriminator, no
/// schema version, no encoding. The owning assembly answers "what data defines this object"; the persistence
/// layer answers "how is that data encoded, versioned, and migrated". A DTO here would drag the file format
/// into the domain.
/// </para>
/// <para>
/// Use this when an instance can be rebuilt from its own snapshot alone. When rebuilding requires resolving
/// references to <i>other</i> objects, use <see cref="ISnapshottingNode{TSelf,TSnapshot}"/> instead. The axis is
/// whether reconstruction needs outside help — not where the type sits in any tree. A leaf variable holding only
/// its own value qualifies; a product referring to its factors by id does not.
/// </para>
/// <para>
/// Only closed, single-implementation types use this. A polymorphic hierarchy cannot: the concrete type is
/// chosen by inspecting the snapshot, so reconstruction has to be a static gateway over the closed set rather
/// than a per-type <c>static abstract</c>. Uncertainty and provenance are both handled that way, by a factory.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TSnapshot">The snapshot that fully describes an instance.</typeparam>
public interface ISnapshotting<TSelf, TSnapshot> where TSelf : ISnapshotting<TSelf, TSnapshot>
{
    /// <summary>Returns the complete snapshot defining this instance.</summary>
    TSnapshot GetSnapshot();

    /// <summary>Rebuilds an instance from a previously captured snapshot. Not part of the normal construction API.</summary>
    static abstract TSelf FromSnapshot(TSnapshot snapshot);
}
