using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.Systems;

namespace Calcusystem.Serialization.Mappers;

public class SerializingMapper
{
    public Dtos.ExpressionSystem Map(ExpressionSystem system)
    {
        var value = new Dtos.ExpressionSystem
        {
            Description = system.Description,
            Name = system.Name
        };

        value.DirectExpressions.AddRange(system.DirectExpressions.Select(MapDirectExpressionByPattern));
        value.DerivedExpressions.AddRange(system.DerivedExpressions.Select(MapDerivedExpressionByPattern));
        // TODO Constraints
        // TODO Definitions
        return value;
    }

    private Dtos.SingleVariable MapDirectExpressionByPattern(IExpression expression)
    {
        return expression switch
        {
            DeltaVariable delta => Map(delta),
            MagnitudeVariable magnitude => Map(magnitude),
            _ => throw new NotImplementedException(
                $"No mapping for direct expression of type {expression.GetType().Name}")
        };
    }

    private Dtos.ExpressionBase MapDerivedExpressionByPattern(IExpression expression)
    {
        return expression switch
        {
            ReciprocalExpression reciprocal => Map(reciprocal),
            NegatedVariable negated => Map(negated),
            ProductExpression product => Map(product),
            SumExpression sum => Map(sum),
            QuotientExpression quotient => Map(quotient),
            _ => throw new NotImplementedException(
                $"No mapping for derived expression of type {expression.GetType().Name}")
        };
    }

    public Dtos.SingleVariable Map(DeltaVariable v)
    {
        return new Dtos.SingleVariable
        {
            Id = v.Id,
            Type = v.GetType().Name,
            Dimensionality = v.Dimensionality,
            KmsValue = v.Value?.KmsValue,
            RelativeError = v.Value?.RelativeError,
            Symbol = v.Symbol,
        };
    }

    public Dtos.SingleVariable Map(MagnitudeVariable v)
    {
        return new Dtos.SingleVariable
        {
            Id = v.Id,
            Type = v.GetType().Name,
            Dimensionality = v.Dimensionality,
            KmsValue = v.Value?.KmsValue,
            RelativeError = v.Value?.RelativeError,
            Symbol = v.Symbol,
        };
    }

    public Dtos.SingleDerivedVariable Map(ReciprocalExpression x)
    {
        return new Dtos.SingleDerivedVariable
        {
            Id = x.Id,
            Type = x.GetType().Name,
            InnerId = x.Reciprocand.Id
        };
    }

    public Dtos.SingleDerivedVariable Map(NegatedVariable x)
    {
        return new Dtos.SingleDerivedVariable
        {
            Id = x.Id,
            Type = x.GetType().Name,
            InnerId = x.NegatedExpression.Id
        };
    }

    public Dtos.ListDerivedVariable Map(ProductExpression x)
    {
        return new Dtos.ListDerivedVariable
        {
            Id = x.Id,
            Type = x.GetType().Name,
            InnerIds = x.Factors.Select(f => f.Id).ToList(),
            ErrorPropagation = x.ErrorPropagation,
        };
    }

    public Dtos.ListDerivedVariable Map(SumExpression x)
    {
        return new Dtos.ListDerivedVariable
        {
            Id = x.Id,
            Type = x.GetType().Name,
            InnerIds = x.Addends.Select(f => f.Id).ToList(),
            ErrorPropagation = x.ErrorPropagation,
        };
    }

    public Dtos.PairDerivedVariable Map(QuotientExpression x)
    {
        return new Dtos.PairDerivedVariable
        {
            Id = x.Id,
            Type = x.GetType().Name,
            InnerId1 = x.Numerator.Id,
            InnerId2 = x.Denominator.Id
        };
    }
}
