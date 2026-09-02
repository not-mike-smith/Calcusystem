using System.Collections.Generic;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Units;

public abstract class UnitList
{
    public abstract IReadOnlyDictionary<string, UnitOfMeasure> ByName { get; }
    public abstract IReadOnlyList<UnitOfMeasure> All { get; }
    public abstract IReadOnlyDictionary<string, UnitOfMeasure> BySymbol { get; }
    public abstract Dimensionality Dimensionality { get; }
}
