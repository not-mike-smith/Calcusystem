using System.Net;
using System.Net.Http.Json;
using Calcusystem.Api.Reports;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Units;
using Calcusystem.Serialization.Mappers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Calcusystem.Api.Test;

/* The catalogue serves a conversion as the two numbers `UnitOfMeasure` converts with, for a client that cannot
   hold the objects. The numbers are the domain's own; what is carried elsewhere is the *formula*, and these
   apply it back and compare against the unit itself. An inverted direction is the failure to catch, and it
   looks right at 1. */
public class UnitCatalogueTests(WebApplicationFactory<Program> host)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<UnitCatalogueReport> Catalogue() =>
        (await host.CreateClient().GetFromJsonAsync<UnitCatalogueReport>("/api/units", Wire.Options))!;

    /* Every unit, not a sample, and at values on both sides of zero. A formula that is right at 1 and wrong at
       1000 would still look right in a spot check, and one with the factor inverted is right at 1 exactly.

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
                var read = kms / report.KmsConversionFactor - report.ZeroOffset;

                read.Should().BeApproximately(
                    unit.ConvertFromKmsValue(kms),
                    Math.Abs(unit.ConvertFromKmsValue(kms)) * 1e-9 + 1e-12,
                    $"{unit.Symbol} read at {kms} KMS");

                // And back, which is the direction a client writes a typed value in.
                var written = (read + report.ZeroOffset) * report.KmsConversionFactor;

                written.Should().BeApproximately(kms, Math.Abs(kms) * 1e-9 + 1e-12,
                    $"{unit.Symbol} written back from {read}");
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

        var band = 1 / fahrenheit.KmsConversionFactor;

        band.Should().Be(Temperature.DeltaFahrenheit.ConvertFromKmsValue(1),
            "the factor alone is the delta unit's own conversion, exactly");
        band.Should().BeApproximately(1.8, 1e-12, "a 1 K band is 1.8 Δ°F");
        (1 / fahrenheit.KmsConversionFactor - fahrenheit.ZeroOffset).Should().BeApproximately(-457.87, 1e-9,
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

        (measurand.KmsAbsoluteUncertainty / fahrenheit.KmsConversionFactor).Should().BeApproximately(
            measurand.AbsoluteUncertaintyIn(Temperature.Fahrenheit.DeltaUnit),
            1e-9);
    }

    [Fact]
    public async Task AKmsValueIsAlreadyInTheBaseUnit()
    {
        var mass = (await Catalogue()).Dimensionalities.Single(group => group.Dimensionality == "M1");

        mass.BaseSymbol.Should().Be(Mass.Kilogram.Symbol);
        mass.Units.Single(unit => unit.Symbol == Mass.Kilogram.Symbol)
            .Should().Match<UnitReport>(unit => unit.KmsConversionFactor == 1 && unit.ZeroOffset == 0);
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
