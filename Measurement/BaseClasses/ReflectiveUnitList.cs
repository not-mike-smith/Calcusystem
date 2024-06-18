using System;
using System.Collections.Generic;
using System.Linq;
using Measurement.Models;

namespace Measurement.BaseClasses;

public abstract class ReflectiveUnitList<T> : UnitList where T : class
{
    private readonly Type _type = typeof(T);

    protected ReflectiveUnitList()
    {
        _lazyByName = new(() => ReflectiveGetAllUnitsByName(_type));
        _lazyAll = new(() => ByName.Values.ToList());
        _lazyBySymbol = new(() => All.ToDictionary(
            uom => uom.Symbol,
            uom => uom));

        _lazyDimensionality = new(() => ByName.Values.First().Dimensionality);
    }

    private readonly Lazy<IReadOnlyDictionary<string, UnitOfMeasure>> _lazyByName;
    private readonly Lazy<IReadOnlyList<UnitOfMeasure>> _lazyAll;
    private readonly Lazy<IReadOnlyDictionary<string, UnitOfMeasure>> _lazyBySymbol;
    private readonly Lazy<Dimensionality> _lazyDimensionality;

    public override IReadOnlyDictionary<string, UnitOfMeasure> ByName => _lazyByName.Value;
    public override IReadOnlyList<UnitOfMeasure> All => _lazyAll.Value;
    public override IReadOnlyDictionary<string, UnitOfMeasure> BySymbol => _lazyBySymbol.Value;
    public override Dimensionality Dimensionality => _lazyDimensionality.Value;

    private static Dictionary<string, UnitOfMeasure> ReflectiveGetAllUnitsByName(Type type)
    {
        var uomFieldInfos = type.GetFields()
            .Where(info => info.IsStatic && info.IsPublic && info.FieldType == typeof(UnitOfMeasure));

        return uomFieldInfos.ToDictionary(
            info => info.Name,
            info => (info.GetValue(null) as UnitOfMeasure)!);
    }
}
