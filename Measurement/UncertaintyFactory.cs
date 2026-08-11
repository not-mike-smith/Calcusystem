using Calcusystem.Core;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.State;

namespace Calcusystem.Measurement;

/// <summary>
/// Rebuilds an <see cref="IUncertainty"/> from previously captured <see cref="UncertaintyState"/>.
/// </summary>
/// <remarks>
/// The counterpart to <see cref="IUncertainty.GetState"/>, and the reason <see cref="IUncertainty"/> does not
/// implement <see cref="IStateful{TSelf,TState}"/>: the concrete type is chosen by inspecting the state, so
/// reconstruction is a static gateway over the closed set of shapes rather than a <c>static abstract</c> on each
/// implementation. This mirrors how provenance rebuilds through <c>ProvenanceFactory</c>.
/// <para>
/// This is a persistence entry point, kept deliberately apart from the construction vocabulary
/// (<c>FromRelErr</c> / <c>FromAbsErr</c>) so that callers building an uncertainty are never offered a
/// <c>(bool, double)</c> overload that only makes sense to a deserializer.
/// </para>
/// </remarks>
public static class UncertaintyFactory
{
    /// <summary>Rebuilds the uncertainty described by <paramref name="state"/>.</summary>
    public static IUncertainty FromState(UncertaintyState state) => state.Shape switch
    {
        UncertaintyShape.Symmetric =>
            SymmetricUncertainty.From(state.IsStoredAsAbs, state.UpperMagnitude),
        UncertaintyShape.Asymmetric =>
            AsymmetricUncertainty.From(state.IsStoredAsAbs, state.UpperMagnitude, state.LowerMagnitude),
        _ => throw new ArgumentOutOfRangeException(
            nameof(state), state.Shape, "Unknown uncertainty shape."),
    };
}
