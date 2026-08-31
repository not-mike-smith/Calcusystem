namespace Calcusystem.DimensionedExpression.Enums;

public enum ComparisonType : byte
{
    EqualTo = 0b000,
    LessThan = 0b001,
    GreaterThan = 0b010,
    Undetermined = 0b011,
    InequalTo = 0b100,
    GreaterThanOrEqualTo = 0b101,
    LessThanOrEqualTo = 0b110,
    Determinable = 0b111
}