using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.Provenance;
using DimensionedExpression.Systems;
using Measurement.Interfaces;
using Measurement;
using Measurement.Uncertainty;

namespace Calcusystem.Serialization.Mappers;

public class SerializingMapper
{
    public Dtos.ExpressionSystem Map(ExpressionSystem system)
    {
        var value = new Dtos.ExpressionSystem
        {
            Id = system.Id,
            Type = system.GetType().Name,
            Description = system.Description,
            Name = system.Name
        };

        value.DirectExpressions.AddRange(system.DirectExpressions.Select(MapVariable));
        value.SingleDerivedVariables.AddRange(system.DerivedExpressions
            .Select(MapSingleDerivedExpressionByPattern)
            .Where(x => x != null)!);

        value.ListDerivedVariables.AddRange(system.DerivedExpressions
            .Select(MapListDerivedExpressionByPattern)
            .Where(x => x != null)!);

        value.PairDerivedVariables.AddRange(system.DerivedExpressions
            .Select(MapPairDerivedExpressionByPattern)
            .Where(x => x != null)!);

        value.Constraints.AddRange(system.Constraints.Select(Map));
        value.Definitions.AddRange(system.Definitions.Select(Map));
        return value;
    }

    private Dtos.SingleDerivedVariable? MapSingleDerivedExpressionByPattern(IExpression expression)
    {
        var x = MapDerivedExpressionByPattern(expression);
        if (x is Dtos.SingleDerivedVariable variable) return variable;

        return null;
    }

    private Dtos.ListDerivedVariable? MapListDerivedExpressionByPattern(IExpression expression)
    {
        var x = MapDerivedExpressionByPattern(expression);
        if (x is Dtos.ListDerivedVariable variable) return variable;

        return null;
    }

    private Dtos.PairDerivedVariable? MapPairDerivedExpressionByPattern(IExpression expression)
    {
        var x = MapDerivedExpressionByPattern(expression);
        if (x is Dtos.PairDerivedVariable variable) return variable;

        return null;
    }

    private Dtos.ExpressionBase MapDerivedExpressionByPattern(IExpression expression)
    {
        return expression switch
        {
            ReciprocalExpression reciprocal => Map(reciprocal),
            NegatedExpression negated => Map(negated),
            ProductExpression product => Map(product),
            SumExpression sum => Map(sum),
            QuotientExpression quotient => Map(quotient),
            _ => throw new NotImplementedException(
                $"No mapping for derived expression of type {expression.GetType().Name}")
        };
    }

    public Dtos.SingleVariable MapVariable(Variable v)
    {
        if (v.Value == null)
        {
            return new Dtos.SingleVariable
            {
                Id = v.Id,
                Type = v.GetType().Name,
                Dimensionality = v.Dimensionality,
                KmsValue = null,
                Uncertainty = null,
                Symbol = v.Symbol,
                Provenance = Map(v.Provenance),
            };
        }
        // else
        return new Dtos.SingleVariable
        {
            Id = v.Id,
            Type = v.GetType().Name,
            Dimensionality = v.Dimensionality,
            KmsValue = v.Value.KmsValue,
            Uncertainty = Map(v.Value.Uncertainty),
            Symbol = v.Symbol,
            Provenance = Map(v.Provenance),
        };
    }

    private Dtos.Provenance? Map(IProvenance? provenance)
    {
        return provenance switch
        {
            null => null,
            MeasuredProvenance m => new Dtos.Provenance
            {
                Id = m.Id, Type = m.GetType().Name,
                InstrumentId = m.InstrumentId, CalibrationDate = m.CalibrationDate
            },
            ReferenceProvenance r => new Dtos.Provenance
            {
                Id = r.Id, Type = r.GetType().Name,
                Citation = r.Citation, Url = r.Url, Year = r.Year
            },
            DesignProvenance d => new Dtos.Provenance
            {
                Id = d.Id, Type = d.GetType().Name,
                SpecReference = d.SpecReference
            },
            ModelProvenance md => new Dtos.Provenance
            {
                Id = md.Id, Type = md.GetType().Name,
                ModelName = md.ModelName, FittingReference = md.FittingReference
            },
            _ => throw new NotImplementedException(
                $"No mapping for provenance of type {provenance.GetType().Name}")
        };
    }

    private Dtos.Uncertainty Map(IUncertainty uncertainty)
    {
        return uncertainty switch
        {
            SymmetricUncertainty symmetric => new Dtos.SymmetricUncertainty
            {
                Type = symmetric.GetType().Name,
                IsStoredAsAbs = symmetric.IsStoredAsAbs,
                Magnitude = symmetric.Magnitude
            },
            AsymmetricUncertainty asymmetric => new Dtos.AsymmetricUncertainty
            {
                Type = asymmetric.GetType().Name,
                IsStoredAsAbs = asymmetric.IsStoredAsAbs,
                UpperMagnitude = asymmetric.UpperMagnitude,
                LowerMagnitude = asymmetric.LowerMagnitude
            },
            _ => throw new NotImplementedException(
                $"No mapping for uncertainty of type {uncertainty.GetType().Name}")
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

    public Dtos.SingleDerivedVariable Map(NegatedExpression x)
    {
        return new Dtos.SingleDerivedVariable
        {
            Id = x.Id,
            Type = x.GetType().Name,
            InnerId = x.Operand.Id
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

    public Dtos.BinaryOperator Map(IBinaryOperator x)
    {
        return new Dtos.BinaryOperator
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Type = x.GetType().Name,
            LhsId = x.Lhs.Id,
            RhsId = x.Rhs.Id,
            Provenance = Map(x.Provenance),
        };
    }
}
