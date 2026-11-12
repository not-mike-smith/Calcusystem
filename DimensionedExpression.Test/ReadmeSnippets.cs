using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Primitives;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test;

/* Snippets printed in this assembly's READMEs, compiled. Only the fixed-value snippet of the Expressions README
   is here; the assembly's older snippets have no compiled counterpart. */
public class ReadmeSnippets
{
    [Fact]
    public void ExpressionsReadmeFixedValues()
    {
        var gravity = new Quantity(9.80665, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time))
            .WithoutUncertainty();

        var g = ExpressionFactory.FromSnapshot(
            new FixedExpressionSnapshot(FixedExpressionType.Constant, "const-g", "g", gravity.GetSnapshot(), null));

        g.Value.KmsValue.Should().Be(9.80665);
        g.IsFullyDescribed.Should().BeTrue();

        FixedValueRule.Problem(FixedExpressionType.Scalar, gravity).Should().StartWith("A scalar is dimensionless, and ");
    }
}
