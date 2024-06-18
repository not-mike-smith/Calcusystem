using System.Collections.Generic;
using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Temperature : ReflectiveUnitList<Temperature>
{
    private Temperature() { }
    public static readonly Temperature Units = new();

    public static readonly UnitOfMeasure Kelvin = UnitFactory.Create("K",
        Dimensionality.Temperature, 1);

    public static readonly OffsetUnitOfMeasure Celsius = UnitFactory.Create(
        "°C",
        1,
        Kelvin,
        273.15);

    public static readonly UnitOfMeasure DeltaCelsius = Celsius.DeltaUnit;

    public static readonly UnitOfMeasure Rankine = UnitFactory.Create("°R",
        Dimensionality.Temperature, 1.8);

    public static readonly OffsetUnitOfMeasure Fahrenheit = UnitFactory.Create(
        "°F",
        1,
        Rankine,
        459.67);

    public static readonly UnitOfMeasure DeltaFahrenheit = Fahrenheit.DeltaUnit;
}
