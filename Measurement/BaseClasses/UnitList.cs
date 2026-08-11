using System.Collections.Generic;

namespace Calcusystem.Measurement.BaseClasses;

public abstract class UnitList
{
    public abstract IReadOnlyDictionary<string, UnitOfMeasure> ByName { get; }
    public abstract IReadOnlyList<UnitOfMeasure> All { get; }
    public abstract IReadOnlyDictionary<string, UnitOfMeasure> BySymbol { get; }
    public abstract Dimensionality Dimensionality { get; }
}
