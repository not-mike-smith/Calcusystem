using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Specific heat capacity (L²·t⁻²·T⁻¹ = J/(kg·K)).
// Temperature appears as a delta, so DeltaFahrenheit is used in Imperial units.
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class SpecificHeatCapacity : ReflectiveUnitList<SpecificHeatCapacity>
{
    private SpecificHeatCapacity() { }
    public static readonly SpecificHeatCapacity Units = new();

    // SI
    public static readonly Uom JoulePerKilogramKelvin = UnitFactory.Create(
        "J/(kg·K)",
        (Energy.Joule, 1),
        (Mass.Kilogram, -1),
        (Temperature.Kelvin, -1));

    public static readonly Uom KilojoulePerKilogramKelvin = UnitFactory.Create(
        "kJ/(kg·K)",
        (Energy.Kilojoule, 1),
        (Mass.Kilogram, -1),
        (Temperature.Kelvin, -1));

    // CGS / chemistry (1 calₜₕ/(g·K) = 4184 J/(kg·K))
    public static readonly Uom CalorieThermochemicalPerGramKelvin = UnitFactory.Create(
        "calₜₕ/(g·K)",
        (Energy.CalorieThermochemical, 1),
        (Mass.Gram, -1),
        (Temperature.Kelvin, -1));

    // English Engineering
    /// <summary>BTU per pound per degree Fahrenheit, thermochemical definition.
    /// Other BTU definitions (IT, Mean, etc.) exist but are rarely used in heat transfer practice.</summary>
    public static readonly Uom BtuThermochemicalPerPoundDeltaFahrenheit = UnitFactory.Create(
        "Btuₜₕ/(lb·°F)",
        (Energy.BtuThermochemical, 1),
        (Mass.Pound, -1),
        (Temperature.DeltaFahrenheit, -1));
}
