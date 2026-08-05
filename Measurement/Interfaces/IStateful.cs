namespace Measurement.Interfaces;

/// <summary>
/// A type that can hand out the complete state defining an instance, and rebuild an instance from that state.
/// This is the seam persistence layers use instead of reaching for the type's internals.
/// </summary>
/// <remarks>
/// <para>
/// <typeparamref name="TState"/> is a plain state record — a <i>memento</i>. It deliberately carries no format
/// concerns: no type discriminator, no schema version, no encoding. Measurement answers "what state defines this
/// object"; the persistence layer answers "how is that state encoded, versioned, and migrated". Keeping those two
/// questions apart is the whole point of this interface — a DTO here would drag the file format into this assembly.
/// </para>
/// <para>
/// Only closed, single-implementation types use this. A polymorphic hierarchy cannot: the concrete type is chosen
/// by inspecting the state, so reconstruction has to be a static gateway over the closed set rather than a
/// per-type <c>static abstract</c>. <see cref="IUncertainty"/> is that case — it declares <c>GetState</c> itself
/// and rebuilds through <see cref="UncertaintyFactory"/>.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TState">The state record that fully describes an instance.</typeparam>
public interface IStateful<TSelf, TState> where TSelf : IStateful<TSelf, TState>
{
    /// <summary>Returns the complete state defining this instance.</summary>
    TState GetState();

    /// <summary>Rebuilds an instance from previously captured state. Not part of the normal construction API.</summary>
    static abstract TSelf FromState(TState state);
}
