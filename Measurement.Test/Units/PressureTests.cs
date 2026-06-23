using Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Measurement.Test.Units;

public class PressureTests
{
    private const double StandardAtmospherePa = 101325.0;

    [Fact]
    public void GaugeUnitsHaveSameDimensionalityAsAbsolute()
    {
        Pressure.BarGauge.Dimensionality.Should().Be(Pressure.Bar.Dimensionality);
        Pressure.PsiGauge.Dimensionality.Should().Be(Pressure.PoundPerSquareInch.Dimensionality);
        Pressure.KilopascalGauge.Dimensionality.Should().Be(Pressure.Kilopascal.Dimensionality);
    }

    [Fact]
    public void ZeroGaugePressureEqualsOneStandardAtmosphere()
    {
        Pressure.BarGauge.ConvertToKmsValue(0).Should().BeApproximately(StandardAtmospherePa, 1E-6);
        Pressure.PsiGauge.ConvertToKmsValue(0).Should().BeApproximately(StandardAtmospherePa, 1E-6);
        Pressure.KilopascalGauge.ConvertToKmsValue(0).Should().BeApproximately(StandardAtmospherePa, 1E-6);
    }

    [Fact]
    public void OneBarGaugeEqualsOneAtmospherePlusOneBar()
    {
        var expected = StandardAtmospherePa + Pressure.Bar.ConvertToKmsValue(1);
        Pressure.BarGauge.ConvertToKmsValue(1).Should().BeApproximately(expected, 1E-6);
    }

    [Fact]
    public void AbsolutePressureConvertsToZeroGauge()
    {
        Pressure.BarGauge.ConvertFromKmsValue(StandardAtmospherePa).Should().BeApproximately(0, 1E-9);
        Pressure.PsiGauge.ConvertFromKmsValue(StandardAtmospherePa).Should().BeApproximately(0, 1E-9);
        Pressure.KilopascalGauge.ConvertFromKmsValue(StandardAtmospherePa).Should().BeApproximately(0, 1E-9);
    }

    [Fact]
    public void GaugePressureRoundTrips()
    {
        const double valueInBarg = 2.5;
        var absolutePa = Pressure.BarGauge.ConvertToKmsValue(valueInBarg);
        Pressure.BarGauge.ConvertFromKmsValue(absolutePa).Should().BeApproximately(valueInBarg, 1E-9);

        const double valueInPsig = 30.0;
        var absolutePa2 = Pressure.PsiGauge.ConvertToKmsValue(valueInPsig);
        Pressure.PsiGauge.ConvertFromKmsValue(absolutePa2).Should().BeApproximately(valueInPsig, 1E-9);
    }

    [Fact]
    public void AbsoluteSpotChecks()
    {
        // 1 atm = 101325 Pa (exact by definition)
        Pressure.Atmosphere.ConvertToKmsValue(1).Should().BeApproximately(StandardAtmospherePa, 1E-6);

        // 1 bar = 100000 Pa (exact by definition)
        Pressure.Bar.ConvertToKmsValue(1).Should().BeApproximately(100000, 1E-6);

        // 1 psi ≈ 6894.76 Pa
        Pressure.PoundPerSquareInch.ConvertToKmsValue(1).Should().BeApproximately(6894.76, 0.01);
    }
}
