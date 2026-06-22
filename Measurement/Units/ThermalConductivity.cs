using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Thermal conductivity (M·L·t⁻³·T⁻¹ = W/(m·K)).
// Temperature appears as a delta (temperature difference), so DeltaFahrenheit is used
// in Imperial units rather than the absolute Fahrenheit scale.
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class ThermalConductivity : ReflectiveUnitList<ThermalConductivity>
{
    private ThermalConductivity() { }
    public static readonly ThermalConductivity Units = new();

    // SI
    public static readonly Uom WattPerMeterKelvin = UnitFactory.Create(
        "W/(m·K)",
        (Power.Watt, 1),
        (Length.Meter, -1),
        (Temperature.Kelvin, -1));

    public static readonly Uom MilliwattPerMeterKelvin = Metric.m(WattPerMeterKelvin);

    // English Engineering
    /// <summary>BTU per hour per foot per degree Fahrenheit, thermochemical definition.
    /// Other BTU definitions (IT, Mean, etc.) exist but are rarely used in heat transfer practice.</summary>
    public static readonly Uom BtuThermochemicalPerHourFootDeltaFahrenheit = UnitFactory.Create(
        "Btuₜₕ/(hr·ft·°F)",
        (Energy.BtuThermochemical, 1),
        (Time.Hour, -1),
        (Length.Foot, -1),
        (Temperature.DeltaFahrenheit, -1));
}
