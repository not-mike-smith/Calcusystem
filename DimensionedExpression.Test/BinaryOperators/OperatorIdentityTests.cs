using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using FluentAssertions;
using Measurement;
using Xunit;

namespace DimensionedExpression.Test.BinaryOperators;

public class OperatorIdentityTests
{
    private static Variable Leaf(string symbol) => new(symbol, Dimensionality.Length);

    [Fact]
    public void Operator_WithoutExplicitId_AutoGeneratesOne()
    {
        var op = new WithinBindingToleranceOperator { Lhs = Leaf("a"), Rhs = Leaf("b") };
        op.Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Operator_WithExplicitId_PreservesIt()
    {
        var op = new WithinBindingToleranceOperator { Id = "op-1", Lhs = Leaf("a"), Rhs = Leaf("b") };
        op.Id.Should().Be("op-1");
    }
}
