using Calcusystem.Serialization.Interfaces;

namespace Calcusystem.Serialization.Exceptions;

public class ExpressionNotFoundDeserializationException : Exception
{
    public readonly string IdOfMissingExpression;
    public readonly ISerializedObject DtoLoadingMissingExpression;

    public ExpressionNotFoundDeserializationException(
        string idOfMissingExpression,
        ISerializedObject dtoLoadingMissingExpression,
        string? message = null,
        Exception? innerException = null) : base(message, innerException)
    {
        IdOfMissingExpression = idOfMissingExpression;
        DtoLoadingMissingExpression = dtoLoadingMissingExpression;
    }
}
