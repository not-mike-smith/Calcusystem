using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Measurement.BaseClasses;
using Measurement.Models;

namespace Measurement;

public class Lists
{
    public static readonly Lists UnitTypes = new();
    private Lists()
    {
        _lazyByName = new(() =>
        {
            var unitListType = typeof(UnitList);
            var unitListTypes = unitListType.Assembly.GetTypes()
                .Where(type => unitListType.IsAssignableFrom(type) && !type.IsAbstract &&
                               type.GetFields().Any(IsTheUnitsSingleton))
                .ToList();

            return unitListTypes.ToDictionary(
                type => type.Name,
                type => (type.GetFields()
                    .FirstOrDefault(IsTheUnitsSingleton)?
                    .GetValue(null) as UnitList)!);
        });

        _lazyByDimensionality = new(() => ByName.Values.ToDictionary(
            list => list.Dimensionality,
            list => list));

        _lazyAll = new(() => ByName.Values.ToList());
    }

    public IReadOnlyDictionary<string, UnitList> ByName => _lazyByName.Value;
    public IReadOnlyDictionary<Dimensionality, UnitList> ByDimensionality => _lazyByDimensionality.Value;
    public IReadOnlyList<UnitList> All => _lazyAll.Value;

    private readonly Lazy<IReadOnlyDictionary<string, UnitList>> _lazyByName;
    private readonly Lazy<IReadOnlyDictionary<Dimensionality, UnitList>> _lazyByDimensionality;
    private readonly Lazy<IReadOnlyList<UnitList>> _lazyAll;
    private static bool IsTheUnitsSingleton(FieldInfo info)
    {
        return info is { IsStatic: true, IsPublic: true, Name: "Units" };
    }

}
