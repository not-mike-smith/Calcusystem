using Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Measurement.Test.Units;

public class TorqueTests
{
    [Fact]
    public void TorqueIsNotDimensionallyEqualToEnergy()
    {
        Torque.NewtonMeter.Dimensionality.Should().NotBe(Energy.Joule.Dimensionality);
    }

    [Fact]
    public void NewtonMeterIsKmsBase()
    {
        Torque.NewtonMeter.ConvertToKmsValue(1).Should().BeApproximately(1, 1E-9);
        Torque.NewtonMeter.ConvertFromKmsValue(1).Should().BeApproximately(1, 1E-9);
    }

    [Fact]
    public void ImperialSpotChecks()
    {
        // 1 lbf·ft ≈ 1.35582 N·m
        Torque.PoundForceFoot.ConvertToKmsValue(1).Should().BeApproximately(1.35582, 0.0001);

        // 1 lbf·in = 1/12 lbf·ft
        Torque.PoundForceInch.ConvertToKmsValue(1).Should().BeApproximately(1.35582 / 12, 0.0001);

        // 1 ozf·in = 1/16 lbf·in
        Torque.OunceForceInch.ConvertToKmsValue(1).Should().BeApproximately(1.35582 / 192, 0.0001);
    }

    [Fact]
    public void TwelvePoundForceInchesEqualsOnePoundForceFoot()
    {
        Torque.PoundForceInch.ConvertToKmsValue(12)
            .Should().BeApproximately(Torque.PoundForceFoot.ConvertToKmsValue(1), 1E-9);
    }

    [Fact]
    public void SixteenOunceForceInchesEqualsOnePoundForceInch()
    {
        Torque.OunceForceInch.ConvertToKmsValue(16)
            .Should().BeApproximately(Torque.PoundForceInch.ConvertToKmsValue(1), 1E-9);
    }
}
