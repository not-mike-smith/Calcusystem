using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Specific energy (L²·t⁻²) — energy per unit mass; also covers specific enthalpy.
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class SpecificEnergy : ReflectiveUnitList<SpecificEnergy>
{
    private SpecificEnergy() { }
    public static readonly SpecificEnergy Units = new();

    // SI
    public static readonly Uom JoulePerKilogram = UnitFactory.Create(
        "J/kg",
        (Energy.Joule, 1),
        (Mass.Kilogram, -1));

    public static readonly Uom KilojoulePerKilogram = UnitFactory.Create(
        "kJ/kg",
        (Energy.Kilojoule, 1),
        (Mass.Kilogram, -1));

    public static readonly Uom MegajoulePerKilogram = UnitFactory.Create(
        "MJ/kg",
        (Energy.Megajoule, 1),
        (Mass.Kilogram, -1));

    // Watt-hour variants (common for battery/fuel energy density)
    public static readonly Uom WattHourPerKilogram = UnitFactory.Create(
        "Wh/kg",
        (Power.Watt, 1),
        (Time.Hour, 1),
        (Mass.Kilogram, -1));

    public static readonly Uom KilowattHourPerKilogram = UnitFactory.Create(
        "kWh/kg",
        (Power.Kilowatt, 1),
        (Time.Hour, 1),
        (Mass.Kilogram, -1));

    // English Engineering
    /// <summary>BTU per pound, thermochemical definition.
    /// Other BTU definitions (IT, Mean, 39°F, 59°F, 60°F) exist but are rarely used in practice.</summary>
    public static readonly Uom BtuThermochemicalPerPound = UnitFactory.Create(
        "Btuₜₕ/lb",
        (Energy.BtuThermochemical, 1),
        (Mass.Pound, -1));

    // CGS / nutritional
    public static readonly Uom CalorieThermochemicalPerGram = UnitFactory.Create(
        "calₜₕ/g",
        (Energy.CalorieThermochemical, 1),
        (Mass.Gram, -1));
}
