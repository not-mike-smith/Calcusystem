using DimensionedExpression.Expressions;

namespace Calcusystem.Serialization.Mappers;

public class Serializer
{
    public Dtos.SingleVariable Map(SingleVariable v)
    {
        return new Dtos.SingleVariable
        {
            Id = v.Id,
            Type = v.GetType().Name,
            Dimensionality = v.Dimensionality,
            KmsValue = v.Value?.KmsValue,
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
            InnerIds = x.Factors.Select(f => f.Id).ToList()
        };
    }

    public Dtos.ListDerivedVariable Map(SumExpression x)
    {
        return new Dtos.ListDerivedVariable
        {
            Id = x.Id,
            Type = x.GetType().Name,
            InnerIds = x.Addends.Select(f => f.Id).ToList()
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
