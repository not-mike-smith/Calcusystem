using Measurement.Interfaces;

namespace Measurement.Uncertainty;

public delegate IUncertainty UncertaintyFromNominalValue(Quantity value);