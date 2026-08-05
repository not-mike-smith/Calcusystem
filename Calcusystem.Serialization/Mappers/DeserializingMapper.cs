using Calcusystem.Serialization.Exceptions;
using Calcusystem.Serialization.Interfaces;
using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.Provenance;
using DimensionedExpression.State;
using DimensionedExpression.Systems;
using Measurement;
using Measurement.State;
using Measurement.Interfaces;

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
        var dimensionality = Dimensionality.FromState(DimensionalityCodec.Decode(v.Dimensionality));

        var variable = v.KmsValue == null
            ? new Variable(v.Symbol, dimensionality, v.Id)
            : new Variable(v.Symbol, new Quantity(v.KmsValue.Value, dimensionality).Measurand(MapUncertainty(v.Uncertainty)), v.Id);

        variable.Provenance = MapProvenance(v.Provenance);
        return variable;
    }

    private IProvenance? MapProvenance(Dtos.Provenance? provenance)
    {
        if (provenance is null) return null;

        // This layer reads the wire type-name and hands DimensionedExpression nothing but the state it needs.
        var state = provenance.Type switch
        {
            nameof(MeasuredProvenance) => ProvenanceState.Measured(
                provenance.Id, provenance.InstrumentId, provenance.CalibrationDate),
            nameof(ReferenceProvenance) => ProvenanceState.Reference(
                provenance.Id, provenance.Citation!, provenance.Url, provenance.Year),
            nameof(DesignProvenance) => ProvenanceState.Design(
                provenance.Id, provenance.SpecReference),
            nameof(ModelProvenance) => ProvenanceState.Model(
                provenance.Id, provenance.ModelName!, provenance.FittingReference),
            _ => throw new NotImplementedException(
                $"No deserialization method defined for provenance type {provenance.Type}")
        };

        return ProvenanceFactory.FromState(state);
    }

    private IUncertainty MapUncertainty(Dtos.Uncertainty? uncertainty)
    {
        // This layer owns the wire format — including any version fix-up of older payloads — and hands
        // Measurement nothing but the state it needs to rebuild.
        if (uncertainty is null) return SymmetricUncertainty.FromRelErr(0);

        return uncertainty.Type switch
        {
            nameof(SymmetricUncertainty) => UncertaintyFactory.FromState(
                UncertaintyState.Symmetric(
                    uncertainty.IsStoredAsAbs,
                    Required(uncertainty.Magnitude, nameof(uncertainty.Magnitude), uncertainty.Type))),

            nameof(AsymmetricUncertainty) => UncertaintyFactory.FromState(
                UncertaintyState.Asymmetric(
                    uncertainty.IsStoredAsAbs,
                    Required(uncertainty.UpperMagnitude, nameof(uncertainty.UpperMagnitude), uncertainty.Type),
                    Required(uncertainty.LowerMagnitude, nameof(uncertainty.LowerMagnitude), uncertainty.Type))),

            _ => throw new NotImplementedException(
                $"No deserialization method defined for uncertainty type {uncertainty.Type}")
        };
    }

    /// <summary>
    /// Reads a field of the flat uncertainty DTO that is required for the shape named by its discriminator.
    /// Missing means the payload is malformed; substituting a default would quietly change the error band.
    /// </summary>
    private static double Required(double? value, string field, string type)
    {
        return value ?? throw new InvalidOperationException(
            $"Uncertainty of type {type} is missing required field {field}.");
    }

    private IExpression GetExpression(string id, ISerializedObject expressionDto)
    {
        var foundIt = _context.ExpressionsById.TryGetValue(id, out var value);
        if (foundIt is false)
        {
            throw new ReferencedNodeNotFoundException(id, expressionDto);
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
