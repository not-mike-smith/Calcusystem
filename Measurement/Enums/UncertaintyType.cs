using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Snapshots;
using Calcusystem.Measurement.Uncertainties;


namespace Calcusystem.Measurement.Enums;

/// <summary>The directional shape an <see cref="UncertaintySnapshot"/> describes — which concrete
/// <see cref="IUncertainty"/> it rebuilds into.</summary>
public enum UncertaintyType
{
    /// <summary>Equal error above and below the nominal value; rebuilds a <see cref="SymmetricUncertainty"/>.</summary>
    Symmetric,

    /// <summary>Independent upper/lower errors; rebuilds an <see cref="AsymmetricUncertainty"/>.</summary>
    Asymmetric,
}
