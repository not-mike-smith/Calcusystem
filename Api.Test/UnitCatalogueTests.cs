using System.Net;
using System.Net.Http.Json;
using Calcusystem.Api.Reports;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Units;
using Calcusystem.Serialization.Mappers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Calcusystem.Api.Test;

/* The catalogue serves a conversion as two numbers for a client to apply, which is a rule `Measurement` owns
   being carried somewhere else. These are what keep the copy honest: the numbers are measured from each unit's
   own method, and here they are applied back and compared against it. */
public class UnitCatalogueTests(WebApplicationFactory<Program> host)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<UnitCatalogueReport> Catalogue() =>
        (await host.CreateClient().GetFromJsonAsync<UnitCatalogueReport>("/api/units", Wire.Options))!;

    /* Every unit, not a sample, and at values on both sides of zero. An affine model that is right at 1 and
       wrong at 1000 would still look right in a spot check.

       Keyed by dimensionality as well as symbol, because a symbol is only unique within one: a gross and a
       grain are both `gr`. */
    [Fact]
    public async Task ApplyingTheServedNumbersGivesWhatTheUnitItselfAnswers()
    {
        var served = (await Catalogue()).Dimensionalities
            .SelectMany(group => group.Units.Select(unit => (group.Dimensionality, unit)))
            .ToDictionary(pair => (pair.Dimensionality, pair.unit.Symbol), pair => pair.unit);

        var covered = 0;

        foreach (var unit in Lists.UnitTypes.All.SelectMany(list => list.All))
        {
            var key = (DimensionalityCodec.Encode(unit.Dimensionality.GetSnapshot()), unit.Symbol);
            var report = served.Should().ContainKey(key).WhoseValue;

            foreach (var kms in new[] { -1234.5, -1, 0, 1, 2.5, 1000, 98765.4321 })
            {
                var byTheNumbers = report.Slope * kms + report.Offset;

                byTheNumbers.Should().BeApproximately(
                    unit.ConvertFromKmsValue(kms),
                    Math.Abs(unit.ConvertFromKmsValue(kms)) * 1e-9 + 1e-12,
                    $"{unit.Symbol} at {kms} KMS");
            }

            covered++;
        }

        covered.Should().BeGreaterThan(400, "every unit in the library should have been covered");
    }

    /* The distinction that is silent when it goes wrong. A band is a difference, so the offset must not touch
       it: ±1 K is ±1.8 °F, and applying the offset would make it ±33.8. */
    [Fact]
    public async Task ABandConvertsByTheSlopeAloneWhereAUnitHasAnOffset()
    {
        var fahrenheit = (await Catalogue()).Dimensionalities
            .SelectMany(group => group.Units)
            .Single(unit => unit.Symbol == Temperature.Fahrenheit.Symbol);

        var band = fahrenheit.Slope * 1;

        band.Should().Be(Temperature.DeltaFahrenheit.ConvertFromKmsValue(1),
            "the slope is the delta unit's own conversion, exactly");
        band.Should().BeApproximately(1.8, 1e-12, "a 1 K band is 1.8 Δ°F");
        (fahrenheit.Slope * 1 + fahrenheit.Offset).Should().BeApproximately(-457.87, 1e-9,
            "a value of 1 K reads −457.87 °F, which is what converting a band this way would give");
        fahrenheit.DeltaSymbol.Should().Be(Temperature.DeltaFahrenheit.Symbol);
    }

    /* The band's own unit agrees with the domain's delta unit, so a client applying the slope and labelling it
       with DeltaSymbol says the same thing `AbsoluteUncertaintyIn` would. */
    [Fact]
    public async Task ABandByTheSlopeMatchesWhatTheDomainReadsInTheDeltaUnit()
    {
        var measurand = Temperature.Kelvin.Quantity(300).WithUncertainty(2.0.Percent());

        var fahrenheit = (await Catalogue()).Dimensionalities
            .SelectMany(group => group.Units)
            .Single(unit => unit.Symbol == Temperature.Fahrenheit.Symbol);

        (fahrenheit.Slope * measurand.KmsAbsoluteUncertainty).Should().BeApproximately(
            measurand.AbsoluteUncertaintyIn(Temperature.Fahrenheit.DeltaUnit),
            1e-9);
    }

    [Fact]
    public async Task AKmsValueIsAlreadyInTheBaseUnit()
    {
        var mass = (await Catalogue()).Dimensionalities.Single(group => group.Dimensionality == "M1");

        mass.BaseSymbol.Should().Be(Mass.Kilogram.Symbol);
        mass.Units.Select(unit => unit.Symbol).Should().Contain([Mass.Pound.Symbol, Mass.Tonne.Symbol]);
    }

    /* A dimensionality appears once, whichever unit lists claim it, so a picker asks one question and gets one
       list of answers. */
    [Fact]
    public async Task EachDimensionalityIsListedOnce()
    {
        var groups = (await Catalogue()).Dimensionalities;

        groups.Select(group => group.Dimensionality).Should().OnlyHaveUniqueItems();
        groups.Should().HaveCountGreaterThan(30);
    }

    [Fact]
    public async Task TheCatalogueIsServed()
    {
        var response = await host.CreateClient().GetAsync("/api/units");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
