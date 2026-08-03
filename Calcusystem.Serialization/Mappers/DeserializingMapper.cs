using Calcusystem.Serialization.Exceptions;
using Calcusystem.Serialization.Interfaces;
using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.Provenance;
using DimensionedExpression.Systems;
using Measurement;
using Measurement.Interfaces;
using Measurement.Uncertainty;

namespace Calcusystem.Serialization.Mappers;

public class DeserializingMapper
{
    private readonly DeserializationContext _context;
    private readonly IEqualityEstimating _equalityEstimator;

    public DeserializingMapper(
        DeserializationContext context,
        IEqualityEstimating equalityEstimator)
    {
        _context = context;
        _equalityEstimator = equalityEstimator;
    }

    public ExpressionSystem Map(Dtos.ExpressionSystem x)
    {
        var system = new ExpressionSystem(x.Id)
        {
            Name = x.Name,
            Description = x.Description
        };

        system.DirectExpressions.AddRange(x.DirectExpressions.Select(MapDirectExpressionByPattern));
        system.DerivedExpressions.AddRange(MapAllDerivedExpressions(x));
        system.Definitions.AddRange(x.Definitions.Select(MapBinaryOperatorByPattern));
        system.Constraints.AddRange(x.Constraints.Select(MapBinaryOperatorByPattern));
        return system;
    }

    private List<IExpression> MapAllDerivedExpressions(Dtos.ExpressionSystem x)
    {
        var deserializedExpressions = new List<IExpression>();
        List<MapDerivedExpressionFcn> functions = new List<MapDerivedExpressionFcn>();
        functions.AddRange(x.SingleDerivedVariables.Select(GetMapper));
        functions.AddRange(x.ListDerivedVariables.Select(GetMapper));
        functions.AddRange(x.PairDerivedVariables.Select(GetMapper));
        while (functions.Any())
        {
            var fcn = functions[0];
            functions.RemoveAt(0);
            var expression = fcn();
            if (expression != null)
            {
                deserializedExpressions.Add(expression);
            }
            else
            {
                functions.Add(fcn);
            }
        }

        return deserializedExpressions;
    }

    delegate IExpression? MapDerivedExpressionFcn();

    public Variable MapDirectExpressionByPattern(Dtos.SingleVariable x)
    {
        Variable variable = x.Type switch
        {
            nameof(Variable) => MapVariable(x),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for SingleVariable object with saved type, {x.Type}")
        };

        _context.AddLoadedExpression(variable);
        return variable;
    }

    public IExpression? MapDerivedExpressionByPattern(Dtos.SingleDerivedVariable x)
    {
        if (! _context.ExpressionsById.ContainsKey(x.InnerId)) return null;

        IExpression expression = x.Type switch
        {
            nameof(ReciprocalExpression) => MapReciprocal(x),
            nameof(NegatedExpression) => MapNegated(x),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for SingleDerivedVariable object with saved type, {x.Type}")
        };

        _context.AddLoadedExpression(expression);
        return expression;
    }

    private MapDerivedExpressionFcn GetMapper(Dtos.SingleDerivedVariable x)
    {
        return () => MapDerivedExpressionByPattern(x);
    }

    public IExpression? MapDerivedExpressionByPattern(Dtos.ListDerivedVariable x)
    {
        if (! x.InnerIds.All(_context.ExpressionsById.ContainsKey)) return null;

        IExpression expression = x.Type switch
        {
            nameof(ProductExpression) => MapProduct(x),
            nameof(SumExpression) => MapSum(x),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for ListDerivedVariable object with saved type, {x.Type}")
        };

        _context.AddLoadedExpression(expression);
        return expression;
    }

    private MapDerivedExpressionFcn GetMapper(Dtos.ListDerivedVariable x)
    {
        return () => MapDerivedExpressionByPattern(x);
    }

    public IExpression? MapDerivedExpressionByPattern(Dtos.PairDerivedVariable x)
    {
        if (! _context.ExpressionsById.ContainsKey(x.InnerId1) || ! _context.ExpressionsById.ContainsKey(x.InnerId2))
            return null;

        IExpression expression = x.Type switch
        {
            nameof(QuotientExpression) => MapQuotient(x),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for PairDerivedVariable object with saved type, {x.Type}")
        };

        _context.AddLoadedExpression(expression);
        return expression;
    }

    private MapDerivedExpressionFcn GetMapper(Dtos.PairDerivedVariable x)
    {
        return () => MapDerivedExpressionByPattern(x);
    }

    public IBinaryOperator MapBinaryOperatorByPattern(Dtos.BinaryOperator x)
    {
        IBinaryOperator op = x.Type switch
        {
            nameof(AnyToleranceOverlapOperator) => MapAnyToleranceOverlapOperator(x),
            nameof(EqualityOperator) => MapEqualityOperator(x),
            nameof(MutuallyWithinToleranceOperator) => MapMutuallyWithToleranceOperator(x),
            nameof(WhollyWithinToleranceOperator) => MapWhollyWithinToleranceOperator(x),
            nameof(WithinBindingToleranceOperator) => MapWithinBindingToleranceOperator(x),
            nameof(PointAndUpperBoundWithinToleranceOperator) => MapPointAndUpperBoundWithinToleranceOperator(x),
            nameof(PointAndLowerBoundWithinToleranceOperator) => MapPointAndLowerBoundWithinToleranceOperator(x),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for BinaryOperator object with saved type, {x.Type}")
        };

        op.Provenance = MapProvenance(x.Provenance);
        return op;
    }

    public Variable MapVariable(Dtos.SingleVariable v)
    {
        var variable = v.KmsValue == null
            ? new Variable(v.Symbol, v.Dimensionality, v.Id)
            : new Variable(v.Symbol, new Quantity(v.KmsValue.Value, v.Dimensionality).Measurand(MapUncertainty(v.Uncertainty)), v.Id);

        variable.Provenance = MapProvenance(v.Provenance);
        return variable;
    }

    private IProvenance? MapProvenance(Dtos.Provenance? provenance)
    {
        if (provenance is null) return null;

        return provenance.Type switch
        {
            nameof(MeasuredProvenance) => ProvenanceFactory.Measured(provenance.InstrumentId, provenance.CalibrationDate, provenance.Id),
            nameof(ReferenceProvenance) => ProvenanceFactory.Reference(provenance.Citation!, provenance.Url, provenance.Year, provenance.Id),
            nameof(DesignProvenance) => ProvenanceFactory.Design(provenance.SpecReference, provenance.Id),
            nameof(ModelProvenance) => ProvenanceFactory.Model(provenance.ModelName!, provenance.FittingReference, provenance.Id),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for provenance type {provenance.Type}")
        };
    }

    private IUncertainty MapUncertainty(Dtos.Uncertainty? uncertainty)
    {
        return uncertainty switch
        {
            Dtos.SymmetricUncertainty sym => GaussianUncertainty.FromRelErr(sym.RelativeError ?? 0),
            Dtos.AsymmetricUncertainty asym => new AsymmetricUncertainty(
                asym.UpperRelativeError ?? 0,
                asym.LowerRelativeError ?? 0),
            null => GaussianUncertainty.FromRelErr(0),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for uncertainty type {uncertainty.GetType().Name}")
        };
    }

    private IExpression GetExpression(string id, ISerializedObject expressionDto)
    {
        var foundIt = _context.ExpressionsById.TryGetValue(id, out var value);
        if (foundIt is false)
        {
            throw new ExpressionNotFoundDeserializationException(id, expressionDto);
        }

        return value!;
    }

    public ReciprocalExpression MapReciprocal(Dtos.SingleDerivedVariable x)
    {
        return new ReciprocalExpression(GetExpression(x.InnerId, x), x.Id);
    }

    public NegatedExpression MapNegated(Dtos.SingleDerivedVariable x)
    {
        return new NegatedExpression(GetExpression(x.InnerId, x), x.Id);
    }

    public ProductExpression MapProduct(Dtos.ListDerivedVariable x)
    {
        var expressions = x.InnerIds.Select(id => GetExpression(id, x)).ToList();
        var value = new ProductExpression
        {
            Id = x.Id,
            ErrorPropagation = x.ErrorPropagation,
        };

        expressions.ForEach(expression => value.AddFactor(expression));
        return value;
    }

    public SumExpression MapSum(Dtos.ListDerivedVariable x)
    {
        var expressions = x.InnerIds.Select(id => GetExpression(id, x)).ToList();
        var value = new SumExpression(expressions)
        {
            Id = x.Id,
            ErrorPropagation = x.ErrorPropagation,
        };

        return value;
    }

    public QuotientExpression MapQuotient(Dtos.PairDerivedVariable x)
    {
        return new QuotientExpression
        {
            Id = x.Id,
            Numerator = GetExpression(x.InnerId1, x),
            Denominator = GetExpression(x.InnerId2, x)
        };
    }

    public AnyToleranceOverlapOperator MapAnyToleranceOverlapOperator(Dtos.BinaryOperator x)
    {
        return new AnyToleranceOverlapOperator
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Lhs = GetExpression(x.LhsId, x),
            Rhs = GetExpression(x.RhsId, x)
        };
    }

    public EqualityOperator MapEqualityOperator(Dtos.BinaryOperator x)
    {
        return new EqualityOperator(_equalityEstimator)
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Lhs = GetExpression(x.LhsId, x),
            Rhs = GetExpression(x.RhsId, x)
        };
    }

    public MutuallyWithinToleranceOperator MapMutuallyWithToleranceOperator(Dtos.BinaryOperator x)
    {
        return new MutuallyWithinToleranceOperator
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Lhs = GetExpression(x.LhsId, x),
            Rhs = GetExpression(x.RhsId, x)
        };
    }

    public WhollyWithinToleranceOperator MapWhollyWithinToleranceOperator(Dtos.BinaryOperator x)
    {
        return new WhollyWithinToleranceOperator
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Lhs = GetExpression(x.LhsId, x),
            Rhs = GetExpression(x.RhsId, x)
        };
    }

    public WithinBindingToleranceOperator MapWithinBindingToleranceOperator(Dtos.BinaryOperator x)
    {
        return new WithinBindingToleranceOperator
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Lhs = GetExpression(x.LhsId, x),
            Rhs = GetExpression(x.RhsId, x)
        };
    }

    public PointAndUpperBoundWithinToleranceOperator MapPointAndUpperBoundWithinToleranceOperator(Dtos.BinaryOperator x)
    {
        return new PointAndUpperBoundWithinToleranceOperator
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Lhs = GetExpression(x.LhsId, x),
            Rhs = GetExpression(x.RhsId, x)
        };
    }

    public PointAndLowerBoundWithinToleranceOperator MapPointAndLowerBoundWithinToleranceOperator(Dtos.BinaryOperator x)
    {
        return new PointAndLowerBoundWithinToleranceOperator
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Lhs = GetExpression(x.LhsId, x),
            Rhs = GetExpression(x.RhsId, x)
        };
    }
}
