using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class MassFlow : ReflectiveUnitList<MassFlow>
{
    private MassFlow() { }
    public static readonly MassFlow Units = new();

    public static readonly Uom KgPerS = UnitFactory.Create(
        (Mass.Kilogram, 1),
        (Time.Second, -1));

    public static readonly Uom LbPerS = UnitFactory.Create(
        (Mass.Pound, 1),
        (Time.Second, -1));

    public static readonly Uom KgPerMin = UnitFactory.Create(
        (Mass.Kilogram, 1),
        (Time.Minute, -1));

    public static readonly Uom LbPerMin = UnitFactory.Create(
        (Mass.Pound, 1),
        (Time.Minute, -1));

    public static readonly Uom TonnePerH = UnitFactory.Create(
        (Mass.Tonne, 1),
        (Time.Hour, -1));

    public static readonly Uom TonPerH = UnitFactory.Create(
        (Mass.Ton, 1),
        (Time.Hour, -1));

    public static readonly Uom GramPerS = UnitFactory.Create(
        (Mass.Gram, 1),
        (Time.Second, -1));
}
