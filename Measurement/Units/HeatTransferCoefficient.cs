using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Heat transfer coefficient (M·t⁻³·T⁻¹ = W/(m²·K)).
// Temperature appears as a delta, so DeltaFahrenheit is used in Imperial units.
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class HeatTransferCoefficient : ReflectiveUnitList<HeatTransferCoefficient>
{
    private HeatTransferCoefficient() { }
    public static readonly HeatTransferCoefficient Units = new();

    // SI
    public static readonly Uom WattPerSquareMeterKelvin = UnitFactory.Create(
        "W/(m²·K)",
        (Power.Watt, 1),
        (Area.SquareMeter, -1),
        (Temperature.Kelvin, -1));

    public static readonly Uom KilowattPerSquareMeterKelvin = Metric.k(WattPerSquareMeterKelvin);

    // English Engineering
    /// <summary>BTU per hour per square foot per degree Fahrenheit, thermochemical definition.
    /// Other BTU definitions (IT, Mean, etc.) exist but are rarely used in heat transfer practice.</summary>
    public static readonly Uom BtuThermochemicalPerHourSquareFootDeltaFahrenheit = UnitFactory.Create(
        "Btuₜₕ/(hr·ft²·°F)",
        (Energy.BtuThermochemical, 1),
        (Time.Hour, -1),
        (Area.SquareFoot, -1),
        (Temperature.DeltaFahrenheit, -1));
}
