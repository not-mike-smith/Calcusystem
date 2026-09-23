
namespace Calcusystem.Measurement.Enums;

/// <summary>The directional shape an <see cref="UncertaintyState"/> describes — which concrete
/// <see cref="IUncertainty"/> it rebuilds into.</summary>
public enum UncertaintyShape
{
    /// <summary>Equal error above and below the nominal value; rebuilds a <see cref="SymmetricUncertainty"/>.</summary>
    Symmetric,

    /// <summary>Independent upper/lower errors; rebuilds an <see cref="AsymmetricUncertainty"/>.</summary>
    Asymmetric,
}
