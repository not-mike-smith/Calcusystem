using Measurement.Interfaces;

namespace Measurement.Uncertainty;

/// <summary>
/// A deferred uncertainty factory: given the nominal <see cref="Quantity"/> a value will carry, it produces the
/// <see cref="IUncertainty"/> that describes it.
/// </summary>
/// <remarks>
/// This indirection exists because an <b>absolute</b> error cannot be converted into the <b>relative</b> error
/// that the uncertainty types store until the nominal value is known. The <c>FromAbsErr</c> factories on
/// <c>GaussianUncertainty</c> and <c>AsymmetricUncertainty</c> capture the absolute error and hand back one of
/// these delegates instead of a finished <see cref="IUncertainty"/>; the overload
/// <see cref="Quantity.Measurand(UncertaintyFromNominalValue)"/> then invokes it with the actual value to
/// complete the <see cref="Measurand"/>. When uncertainty is given as a relative error there is nothing to
/// defer, and the plain <see cref="Quantity.Measurand(IUncertainty)"/> overload is used directly.
/// </remarks>
/// <param name="value">The nominal quantity the resulting uncertainty will describe.</param>
/// <returns>The uncertainty resolved against <paramref name="value"/>.</returns>
public delegate IUncertainty UncertaintyFromNominalValue(Quantity value);
