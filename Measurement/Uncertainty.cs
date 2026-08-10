namespace Calcusystem.Measurement;

/// <summary>
/// The construction vocabulary for uncertainty. Every uncertainty a caller creates comes from here.
/// </summary>
/// <remarks>
/// <para>
/// Rebuilding a persisted uncertainty is a separate concern with its own door, <see cref="UncertaintyFactory"/>,
/// deliberately kept apart so that nobody describing a measurement is offered a storage-form flag that only
/// makes sense to a deserializer.
/// </para>
/// <para>
/// Relative error is taken as a <see cref="RelativeError"/> rather than a bare <see langword="double"/>. A number
/// on its own cannot say whether it means a fraction of the value or an amount of it — the ambiguity this
/// library exists to eliminate everywhere else.
/// </para>
/// </remarks>
public static class Uncertainty
{
    /// <summary>No uncertainty at all: the value is exact.</summary>
    public static SymmetricUncertainty Exact() => SymmetricUncertainty.Exact();

    /// <summary>Equal error above and below, as a fraction of the value.</summary>
    public static SymmetricUncertainty Relative(RelativeError relativeError) =>
        SymmetricUncertainty.FromRelErr(relativeError.Value);

    /// <summary>Equal error above and below, as a dimensioned amount.</summary>
    public static SymmetricUncertainty Absolute(Quantity absoluteError) =>
        SymmetricUncertainty.FromAbsErr(absoluteError);

    /// <summary>Independent errors above and below, each a fraction of the value.</summary>
    public static AsymmetricUncertainty Relative(RelativeError upper, RelativeError lower) =>
        AsymmetricUncertainty.FromRelErr(upper.Value, lower.Value);

    /// <summary>Independent errors above and below, each a dimensioned amount.</summary>
    public static AsymmetricUncertainty Absolute(Quantity upper, Quantity lower) =>
        AsymmetricUncertainty.FromAbsErr(upper, lower);
}
