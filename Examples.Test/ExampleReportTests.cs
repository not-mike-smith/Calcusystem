using Calcusystem.Analysis.Extensions;
using Calcusystem.Examples.Presentation;
using Calcusystem.Examples.Problems;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Examples.Test;

public class ExampleReportTests
{
    public static TheoryData<string> EveryExample()
    {
        var titles = new TheoryData<string>();
        foreach (var example in Catalogue.Build()) titles.Add(example.Title);

        return titles;
    }

    private static WorkedExample ByTitle(string title) =>
        Catalogue.Build().Single(example => example.Title == title);

    /* A readout naming a unit of the wrong dimensionality throws on conversion rather than printing something
       odd, so this is the check that every line of every report can actually be produced. Power.Watt was
       declared as a joule times a second rather than per second, and the electrical example's power line is
       what surfaced it. */
    [Theory]
    [MemberData(nameof(EveryExample))]
    public void EveryReadoutConvertsIntoTheUnitItNames(string title)
    {
        var example = ByTitle(title);
        var calculation = example.System.Calculate();

        foreach (var readout in example.Givens.Concat(example.Computed))
        {
            var value = calculation.ValueOf(readout.Expression);

            value.Should().NotBeNull($"'{readout.Label}' is reported, so it must resolve");
            value!.Dimensionality.Should().Be(
                readout.Unit.Dimensionality,
                $"'{readout.Label}' is reported in {readout.Unit.Symbol}");
        }
    }

    /* Every readout reaches the page. A label that silently stops being printed is the failure mode a report
       cannot show you, because what is missing looks like what was never asked for. */
    [Theory]
    [MemberData(nameof(EveryExample))]
    public void EveryReadoutAndCheckIsNamedInTheReport(string title)
    {
        var example = ByTitle(title);
        var report = ExampleReport.Render(example);

        foreach (var readout in example.Givens.Concat(example.Computed))
        {
            report.Should().Contain(readout.Label);
        }

        foreach (var relationship in example.System.Relationships)
        {
            report.Should().Contain(relationship.Id);
        }
    }

    [Theory]
    [MemberData(nameof(EveryExample))]
    public void EveryGivenCarriesItsProvenance(string title)
    {
        var example = ByTitle(title);

        foreach (var given in example.Givens)
        {
            example.Leaf(given.Expression.Id).Provenance
                .Should().NotBeNull($"'{given.Label}' is a given, so where it came from is part of it");
        }
    }

    /* The glyph is the whole of what a reader scans for, so a failing check that renders like a passing one
       is worse than no report. */
    [Fact]
    public void AViolatedRequirementRendersAsAFailure()
    {
        var example = ParallelDcLoads.Build();
        var report = ExampleReport.Render(example);

        report.Should().Contain($"✗ {ParallelDcLoads.BreakerCheckId}");
        report.Should().Contain("✓ instrument fuse holds");
    }

    /* Withholding a value takes the checks to outstanding, not to failed, and the report has to keep that
       distinction visible - an engineer reading a clean page must be able to tell a check that passed from
       one that never ran. */
    [Fact]
    public void AWithheldValueRendersAsOutstandingRatherThanFailed()
    {
        var example = OrificeDrainTime.Build();
        example.Leaf(OrificeDrainTime.DischargeCoefficientId).Value = null;

        var report = ExampleReport.Render(example, example.System.Calculate());

        report.Should().Contain("— withheld —");
        report.Should().Contain("— unresolved —");
        report.Should().Contain("OUTSTANDING");
        report.Should().NotContain("✗");
    }
}
