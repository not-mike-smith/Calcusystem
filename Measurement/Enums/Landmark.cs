using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Enums;

/// <summary>
/// Named location in a <see cref="Measurand"/>'s uncertainty band, i.e. lower bound, upper bound, or nominal value
/// </summary>
public enum Landmark
{
    LowerBound = 1,
    Nominal = 2,
    UpperBound = 3
}