namespace Calcusystem.Serialization.Exceptions;

public class ExpressionNotFoundDeserializationException : Exception
{
    public readonly string IdOfMissingExpression;
    public readonly Dtos.ExpressionBase DtoLoadingMissingExpression;

    public ExpressionNotFoundDeserializationException(
        string idOfMissingExpression,
        Dtos.ExpressionBase dtoLoadingMissingExpression,
        string? message = null,
        Exception? innerException = null) : base(message, innerException)
    {
        IdOfMissingExpression = idOfMissingExpression;
        DtoLoadingMissingExpression = dtoLoadingMissingExpression;
    }
}
