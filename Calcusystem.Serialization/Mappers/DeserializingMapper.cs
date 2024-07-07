using Calcusystem.Serialization.Exceptions;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;

namespace Calcusystem.Serialization.Mappers;

public class Deserializer
{
    private readonly Measurement.Factories.Deserializer _quantityDeserializer;

    public Deserializer(Measurement.Factories.Deserializer quantityDeserializer)
    {
        _quantityDeserializer = quantityDeserializer;
    }

    public DeltaVariable MapDelta(Dtos.SingleVariable v)
    {
        var delta = _quantityDeserializer.DeserializeDelta(v.Dimensionality, v.KmsValue, v.RelativeError);
        if (delta == null)
        {
            return new DeltaVariable(
                v.Symbol,
                v.Dimensionality,
                v.Id);
        }

        return new DeltaVariable(
            v.Symbol,
            delta,
            v.Id);
    }

    public MagnitudeVariable MapMagnitude(Dtos.SingleVariable v)
    {
        var magnitude = _quantityDeserializer.DeserializeMagnitude(v.Dimensionality, v.KmsValue, v.RelativeError);
        if (magnitude == null)
        {
            return new MagnitudeVariable(
                v.Symbol,
                v.Dimensionality,
                v.Id);
        }

        return new MagnitudeVariable(
            v.Symbol,
            magnitude,
            v.Id);
    }

    private IExpression GetExpression(DeserializationContext context, string id, Dtos.ExpressionBase expressionDto)
    {
        var foundIt = context.ExpressionsById.TryGetValue(expressionDto.Id, out var value);
        if (foundIt is false)
        {
            throw new ExpressionNotFoundDeserializationException(id, expressionDto);
        }

        context.AddLoadedExpression(value!);
        return value!;
    }

    public ReciprocalExpression MapReciprocal(Dtos.SingleDerivedVariable x, DeserializationContext context)
    {
        return new ReciprocalExpression(GetExpression(context, x.InnerId, x), x.Id);
    }

    public NegatedVariable MapNegated(Dtos.SingleDerivedVariable x, DeserializationContext context)
    {
        return new NegatedVariable(GetExpression(context, x.InnerId, x), x.Id);
    }

    public ProductExpression MapProduct(Dtos.ListDerivedVariable x, DeserializationContext context)
    {
        var expressions = x.InnerIds.Select(id => GetExpression(context, id, x)).ToList();
        var value = new ProductExpression
        {
            Id = x.Id,
            ErrorPropagation = x.ErrorPropagation,
        };

        expressions.ForEach(expression => value.AddFactor(expression));
        return value;
    }

    public SumExpression MapSum(Dtos.ListDerivedVariable x, DeserializationContext context)
    {
        var expressions = x.InnerIds.Select(id => GetExpression(context, id, x)).ToList();
        var value = new SumExpression(expressions)
        {
            Id = x.Id,
            ErrorPropagation = x.ErrorPropagation,
        };

        return value;
    }

    public QuotientExpression MapQuotient(Dtos.PairDerivedVariable x, DeserializationContext context)
    {
        return new QuotientExpression
        {
            Id = x.Id,
            Numerator = GetExpression(context, x.InnerId1, x),
            Denominator = GetExpression(context, x.InnerId2, x)
        };
    }
}
