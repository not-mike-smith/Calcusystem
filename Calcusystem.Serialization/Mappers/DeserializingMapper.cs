using Calcusystem.Serialization.Exceptions;
using Calcusystem.Serialization.Interfaces;
using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.State;
using DimensionedExpression.Systems;
using Measurement;
using Measurement.Interfaces;
using Measurement.State;

namespace Calcusystem.Serialization.Mappers;

/// <summary>
/// Rebuilds a live <see cref="ExpressionSystem"/> from flat, id-referenced DTOs.
/// </summary>
/// <remarks>
/// Constructs nothing directly. Each DTO is translated into the corresponding domain state record and handed to
/// that type's own reconstruction, so this class owns the wire format and the rebuild <i>order</i> — not how any
/// domain object is assembled.
/// </remarks>
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

    /// <remarks>
    /// Order matters and is this layer's responsibility: leaves first, then derived expressions as their
    /// dependencies appear, then operators over those expressions, and finally the system that references them
    /// all. By the time any <c>FromState</c> asks the resolver for a neighbour, it is already present.
    /// </remarks>
    public ExpressionSystem Map(Dtos.ExpressionSystem x)
    {
        foreach (var dto in x.DirectExpressions)
        {
            _context.AddLoadedExpression(MapDirectExpressionByPattern(dto));
        }

        MapAllDerivedExpressions(x);

        foreach (var dto in x.Definitions.Concat(x.Constraints))
        {
            _context.AddLoadedOperator(MapBinaryOperatorByPattern(dto));
        }

        _context.ReferencingDto = x;
        return ExpressionSystem.FromState(
            new ExpressionSystemState(
                x.Id,
                x.Name,
                x.Description,
                x.DirectExpressions.Select(d => d.Id).ToList(),
                x.SingleDerivedVariables.Select(d => d.Id)
                    .Concat(x.ListDerivedVariables.Select(d => d.Id))
                    .Concat(x.PairDerivedVariables.Select(d => d.Id))
                    .ToList(),
                x.Definitions.Select(d => d.Id).ToList(),
                x.Constraints.Select(d => d.Id).ToList()),
            _context);
    }

    /// <remarks>
    /// <para>
    /// The flattened lists arrive in arbitrary order, so a parent may be read before the children it references
    /// exist. Each mapping is queued as a deferred build; running one either succeeds, or defers because a child
    /// id is not loaded yet and goes to the back of the queue. The queue drains as dependencies fill in, without
    /// a topological pre-sort.
    /// </para>
    /// <para>
    /// That terminates only while deferrals keep becoming buildable. A missing or cyclic reference makes at
    /// least one entry permanently unbuildable, so the counter below tracks consecutive deferrals: once a full
    /// pass over the remaining queue has produced no progress, nothing can change and the payload is rejected.
    /// Without it the loop spins forever — and because the retry is iterative, not even a stack overflow would
    /// stop it.
    /// </para>
    /// </remarks>
    private void MapAllDerivedExpressions(Dtos.ExpressionSystem expressionSystem)
    {
        var singlePending = expressionSystem.SingleDerivedVariables
            .Select(d => new PendingExpression(
                d.Id,
                [d.InnerId],
                () => MapDerivedExpressionByPattern(d)));

        var pairPending = expressionSystem.PairDerivedVariables
            .Select(d => new PendingExpression(
                d.Id,
                [d.InnerId1, d.InnerId2],
                () => MapDerivedExpressionByPattern(d)));

        var listPending = expressionSystem.ListDerivedVariables
            .Select(d => new PendingExpression(
                d.Id,
                d.InnerIds,
                () => MapDerivedExpressionByPattern(d)));

        // Queue is FIFO by default
        var pending = new Queue<PendingExpression>(singlePending.Concat(pairPending).Concat(listPending));
        var deferralsSinceProgress = 0;

        while (pending.Count > 0)
        {
            var next = pending.Dequeue();

            var expression = next.Build();
            if (expression != null)
            {
                _context.AddLoadedExpression(expression);
                deferralsSinceProgress = 0;
                continue;
            }

            pending.Enqueue(next);
            deferralsSinceProgress++;

            if (deferralsSinceProgress >= pending.Count)
            {
                throw BuildUnresolvableGraphException(pending.ToList(), expressionSystem);
            }
        }
    }

    /// <summary>
    /// Distinguishes the two ways the queue can stall: an id referenced but absent from the payload is a
    /// dangling reference, while one that is present yet still unbuilt is part of a cycle.
    /// </summary>
    private UnresolvableGraphException BuildUnresolvableGraphException(
        List<PendingExpression> pending,
        Dtos.ExpressionSystem x)
    {
        var idsInPayload = x.DirectExpressions.Select(d => d.Id)
            .Concat(x.SingleDerivedVariables.Select(d => d.Id))
            .Concat(x.ListDerivedVariables.Select(d => d.Id))
            .Concat(x.PairDerivedVariables.Select(d => d.Id))
            .ToHashSet();

        var unresolved = pending
            .SelectMany(p => p.DependsOn)
            .Where(id => ! _context.Contains(id))
            .Distinct()
            .ToList();

        return new UnresolvableGraphException(
            pending.Select(p => p.Id).ToList(),
            unresolved.Where(id => ! idsInPayload.Contains(id)).ToList(),
            unresolved.Where(idsInPayload.Contains).ToList());
    }

    /// <summary>An expression whose build is deferred until the ids it references are loaded.</summary>
    private readonly record struct PendingExpression(
        string Id,
        IReadOnlyList<string> DependsOn,
        Func<IExpression?> Build);

    public Variable MapDirectExpressionByPattern(Dtos.SingleVariable x) => x.Type switch
    {
        nameof(Variable) => MapVariable(x),
        _ => throw new NotImplementedException(
            $"No deserialization method defined for SingleVariable object with saved type, {x.Type}")
    };

    public IExpression? MapDerivedExpressionByPattern(Dtos.SingleDerivedVariable x)
    {
        if (! _context.Contains(x.InnerId)) return null;

        _context.ReferencingDto = x;
        return ExpressionFactory.FromState(
            new UnaryExpressionState(WireNames.UnaryKind(x.Type), x.Id, x.InnerId),
            _context);
    }

    public IExpression? MapDerivedExpressionByPattern(Dtos.ListDerivedVariable x)
    {
        if (! x.InnerIds.All(_context.Contains)) return null;

        _context.ReferencingDto = x;
        return ExpressionFactory.FromState(
            new NaryExpressionState(WireNames.NaryKind(x.Type), x.Id, x.InnerIds, x.ErrorPropagation),
            _context);
    }

    public IExpression? MapDerivedExpressionByPattern(Dtos.PairDerivedVariable x)
    {
        if (! _context.Contains(x.InnerId1) || ! _context.Contains(x.InnerId2)) return null;

        _context.ReferencingDto = x;
        return ExpressionFactory.FromState(
            new BinaryExpressionState(
                WireNames.BinaryKind(x.Type), x.Id, x.InnerId1, x.InnerId2, x.ErrorPropagation),
            _context);
    }

    public IBinaryOperator MapBinaryOperatorByPattern(Dtos.BinaryOperator x)
    {
        _context.ReferencingDto = x;
        return BinaryOperatorFactory.FromState(
            new BinaryOperatorState(
                WireNames.OperatorKind(x.Type),
                x.Id,
                x.LhsId,
                x.RhsId,
                x.Name,
                x.Description,
                MapProvenance(x.Provenance)),
            _context,
            _equalityEstimator);
    }

    public Variable MapVariable(Dtos.SingleVariable v) => Variable.FromState(
        new VariableState(
            v.Id,
            v.Symbol,
            DimensionalityCodec.Decode(v.Dimensionality),
            v.KmsValue is { } kms
                ? new MeasurandState(
                    new QuantityState(kms, DimensionalityCodec.Decode(v.Dimensionality)),
                    MapUncertainty(v.Uncertainty))
                : null,
            MapProvenance(v.Provenance)));

    private static ProvenanceState? MapProvenance(Dtos.Provenance? provenance)
    {
        if (provenance is null) return null;

        return WireNames.ProvenanceKindOf(provenance.Type) switch
        {
            ProvenanceKind.Measured => ProvenanceState.Measured(
                provenance.Id, provenance.InstrumentId, provenance.CalibrationDate),
            ProvenanceKind.Reference => ProvenanceState.Reference(
                provenance.Id, provenance.Citation!, provenance.Url, provenance.Year),
            ProvenanceKind.Design => ProvenanceState.Design(
                provenance.Id, provenance.SpecReference),
            ProvenanceKind.Model => ProvenanceState.Model(
                provenance.Id, provenance.ModelName!, provenance.FittingReference),
            var kind => throw new NotImplementedException($"No state mapping for provenance kind {kind}")
        };
    }

    private static UncertaintyState MapUncertainty(Dtos.Uncertainty? uncertainty)
    {
        if (uncertainty is null) return UncertaintyState.Symmetric(false, 0);

        return uncertainty.Type switch
        {
            nameof(SymmetricUncertainty) => UncertaintyState.Symmetric(
                uncertainty.IsStoredAsAbs,
                Required(uncertainty.Magnitude, nameof(uncertainty.Magnitude), uncertainty.Type)),

            nameof(AsymmetricUncertainty) => UncertaintyState.Asymmetric(
                uncertainty.IsStoredAsAbs,
                Required(uncertainty.UpperMagnitude, nameof(uncertainty.UpperMagnitude), uncertainty.Type),
                Required(uncertainty.LowerMagnitude, nameof(uncertainty.LowerMagnitude), uncertainty.Type)),

            _ => throw new NotImplementedException(
                $"No deserialization method defined for uncertainty type {uncertainty.Type}")
        };
    }

    /// <summary>
    /// Reads a field of the flat uncertainty DTO that is required for the shape named by its discriminator.
    /// Missing means the payload is malformed; substituting a default would quietly change the error band.
    /// </summary>
    private static double Required(double? value, string field, string type) =>
        value ?? throw new InvalidOperationException(
            $"Uncertainty of type {type} is missing required field {field}.");
}
