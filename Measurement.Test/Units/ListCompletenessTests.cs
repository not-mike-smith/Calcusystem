using System.Reflection;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test.Units;

/* The lists are built by reflection, so a unit goes missing from them by being declared differently rather
   than by being deleted - and a list that is missing a unit looks exactly like a list that is complete. */
public class ListCompletenessTests
{
    /* Collecting only fields whose declared type is exactly `UnitOfMeasure` drops every one declared as its
       subtype: °C, °F, bar(g), kPa(g) and psi(g). Each is a unit someone would look for first, and each was
       absent from every list, from `BySymbol`, and from anything built over them. */
    [Fact]
    public void AUnitDeclaredAsItsConcreteTypeIsStillInItsList()
    {
        var temperature = Lists.UnitTypes.ByName["Temperature"].All.Select(unit => unit.Symbol);
        var pressure = Lists.UnitTypes.ByName["Pressure"].All.Select(unit => unit.Symbol);

        temperature.Should().Contain([Temperature.Celsius.Symbol, Temperature.Fahrenheit.Symbol]);
        pressure.Should().Contain([
            Pressure.BarGauge.Symbol, Pressure.KilopascalGauge.Symbol, Pressure.PsiGauge.Symbol,
        ]);
    }

    /* Derived from the declarations rather than compared against a number, so this does not need editing when
       a unit is added - which is the only way it keeps working. */
    [Theory]
    [MemberData(nameof(EveryUnitList))]
    public void EveryDeclaredUnitReachesItsList(string name)
    {
        var list = Lists.UnitTypes.ByName[name];

        var declared = list.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => typeof(UnitOfMeasure).IsAssignableFrom(field.FieldType))
            .Select(field => field.Name);

        list.ByName.Keys.Should().BeEquivalentTo(declared);
    }

    public static TheoryData<string> EveryUnitList()
    {
        var names = new TheoryData<string>();
        foreach (var name in Lists.UnitTypes.ByName.Keys) names.Add(name);

        return names;
    }

    /* `BySymbol` keys a dictionary, so two units of one kind sharing a symbol throws on first use rather than
       on being declared - and nothing used it, so `Dimensionless` carried two units called α until a unit
       picker needed to name one of them. Across kinds is a different matter and allowed: a gross and a grain
       are both `gr`, and no picker offers them together. */
    [Theory]
    [MemberData(nameof(EveryUnitList))]
    public void NoTwoUnitsOfOneKindShareASymbol(string name)
    {
        var list = Lists.UnitTypes.ByName[name];

        list.All.Select(unit => unit.Symbol).Should().OnlyHaveUniqueItems();
        foreach (var unit in list.All) list.BySymbol.Should().ContainKey(unit.Symbol);
    }
}
