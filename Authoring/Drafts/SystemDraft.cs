using System.Diagnostics.CodeAnalysis;
using Calcusystem.Authoring.Enums;
using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Authoring.Drafts;

/// <summary>
/// A system under construction: mutable, freely invalid, and able to report what stands between it and an
/// <see cref="ExpressionSystem"/>.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ExpressionSystem"/> refuses to exist in a half-built state — operands are fixed at
/// construction, a sum checks its addends agree, and <c>Add</c> rejects a cross-dimensional relationship. Those
/// rules are what make the built graph safe to cache and walk, and they are also exactly what an editor cannot
/// honor between one keystroke and the next. A draft holds the half-built state instead; building is where the
/// rules apply.
/// </para>
/// <para>
/// <b>Nothing here throws for a modelling mistake.</b> Problems come back as data from
/// <see cref="Validate"/> and <see cref="TryBuild"/>, each naming the node at fault.
/// </para>
/// <para>
/// Ids are fixed when a node is created and carry through to the built system, so a front-end can hold
/// references across a build.
/// </para>
/// </remarks>
public sealed class SystemDraft
{
    private readonly Dictionary<string, ExpressionDraft> _expressions = [];
    private readonly Dictionary<string, RelationshipDraft> _relationships = [];

    private SystemDraft(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    /// <summary>Starts an empty draft.</summary>
    /// <param name="name">What to call the system.</param>
    /// <param name="description">Optional longer description.</param>
    /// <param name="id">The id to build the system under, or null for a fresh one.</param>
    public static SystemDraft Create(string name, string description = "", string? id = null) =>
        new(id ?? NewId(), name, description);

    /// <summary>Identifies the system this draft builds.</summary>
    public string Id { get; }

    /// <summary>What to call the system.</summary>
    public string Name { get; set; }

    /// <summary>Optional longer description.</summary>
    public string Description { get; set; }

    /// <summary>Every expression node in the draft, in no particular order.</summary>
    public IReadOnlyCollection<ExpressionDraft> Expressions => _expressions.Values;

    /// <summary>Every relationship in the draft, in no particular order.</summary>
    public IReadOnlyCollection<RelationshipDraft> Relationships => _relationships.Values;

    /// <summary>Adds an expression node of any kind, typed or not.</summary>
    /// <param name="type">Which kind to build, or null to leave the choice for later.</param>
    /// <param name="id">The id to use, or null for a fresh one.</param>
    public ExpressionDraft AddExpression(ExpressionDraftType? type = null, string? id = null)
    {
        var draft = new ExpressionDraft(id ?? NewId()) { Type = type };
        _expressions[draft.Id] = draft;
        return draft;
    }

    /// <summary>Adds a variable, the one kind with anything to supply up front.</summary>
    /// <param name="symbol">What to call it.</param>
    /// <param name="dimensionality">Its dimensionality, which it cannot be built without.</param>
    /// <param name="id">The id to use, or null for a fresh one.</param>
    public ExpressionDraft AddVariable(
        string? symbol = null,
        Dimensionality? dimensionality = null,
        string? id = null)
    {
        var draft = AddExpression(ExpressionDraftType.Variable, id);
        draft.Symbol = symbol;
        draft.Dimensionality = dimensionality;
        return draft;
    }

    /// <summary>Adds a relationship, typed or not.</summary>
    /// <param name="type">Which operator to build, or null to leave the choice for later.</param>
    /// <param name="id">The id to use, or null for a fresh one.</param>
    public RelationshipDraft AddRelationship(BinaryOperatorType? type = null, string? id = null)
    {
        var draft = new RelationshipDraft(id ?? NewId()) { Type = type };
        _relationships[draft.Id] = draft;
        return draft;
    }

    /// <summary>The expression node with this id, or null if the draft holds none.</summary>
    public ExpressionDraft? Expression(string id) => _expressions.GetValueOrDefault(id);

    /// <summary>The relationship with this id, or null if the draft holds none.</summary>
    public RelationshipDraft? Relationship(string id) => _relationships.GetValueOrDefault(id);

    /// <summary>Removes the expression or relationship with this id.</summary>
    /// <returns>Whether anything was removed.</returns>
    /// <remarks>
    /// References to it are left where they are, and come back from <see cref="Validate"/> as
    /// <see cref="DraftProblemKind.ReferenceNotFound"/>. Scrubbing them would silently discard structure the
    /// author built, and a dangling reference is the more useful thing to show them.
    /// </remarks>
    public bool Remove(string id) => _expressions.Remove(id) | _relationships.Remove(id);

    /// <summary>Everything standing between this draft and a built system. Empty means it builds.</summary>
    /// <remarks>
    /// Runs the build and discards the result, so this and <see cref="TryBuild"/> cannot disagree about whether
    /// the draft is valid.
    /// </remarks>
    public IReadOnlyList<DraftProblem> Validate()
    {
        TryBuild(out _, out var problems);
        return problems;
    }

    /// <summary>Builds the draft into an <see cref="ExpressionSystem"/>, or reports why it cannot be.</summary>
    /// <param name="system">The built system, or null if there were problems.</param>
    /// <param name="problems">What stood in the way, empty on success.</param>
    /// <returns>Whether a system was built.</returns>
    /// <remarks>
    /// <para>
    /// Two passes, because the domain cannot be asked about a node until the node can be constructed at all.
    /// The first checks shape — a kind chosen, operand slots filled, references resolvable, no cycles. The
    /// second constructs each node in dependency order and <b>relays whatever the domain refuses</b>, rather
    /// than restating the domain's rules here where the two could drift apart.
    /// </para>
    /// <para>
    /// A node whose operand failed is not reported as well — one cause gives one problem, not a cascade up every
    /// node above it.
    /// </para>
    /// </remarks>
    public bool TryBuild(
        [NotNullWhen(true)] out ExpressionSystem? system,
        out IReadOnlyList<DraftProblem> problems)
    {
        var found = new List<DraftProblem>();
        var unbuildable = new HashSet<string>();

        ReportDuplicateIds(found);
        var candidates = ShapedWellEnoughToTry(found, unbuildable);
        var built = BuildExpressions(candidates, found, unbuildable);
        var operators = BuildRelationships(built, found, unbuildable);

        if (found.Count > 0)
        {
            system = null;
            problems = found;
            return false;
        }

        system = new ExpressionSystem(Id) { Name = Name, Description = Description };
        foreach (var expression in built.Expressions) system.Add(expression);
        foreach (var op in operators) TryAdd(system, op, found);

        if (found.Count > 0)
        {
            system = null;
            problems = found;
            return false;
        }

        problems = [];
        return true;
    }

    // `Add` is the last place a dimensional mismatch can surface, because it compares the two sides of a
    // relationship rather than anything within one expression.
    private static void TryAdd(ExpressionSystem system, IBinaryOperator op, List<DraftProblem> found)
    {
        try
        {
            system.Add(op);
        }
        catch (Exception e)
        {
            found.Add(new DraftProblem(DraftProblemKind.DomainRule, op.Id, e.Message));
        }
    }

    private void ReportDuplicateIds(List<DraftProblem> found)
    {
        foreach (var id in _expressions.Keys.Intersect(_relationships.Keys))
        {
            found.Add(new DraftProblem(
                DraftProblemKind.DuplicateId,
                id,
                "An expression and a relationship both claim this id."));
        }
    }

    /// <summary>
    /// The expression drafts whose shape allows the domain to be asked about them at all. Anything rejected here
    /// is recorded as unbuildable, so nodes above it stay silent.
    /// </summary>
    private List<ExpressionDraft> ShapedWellEnoughToTry(
        List<DraftProblem> found,
        HashSet<string> unbuildable)
    {
        var candidates = new List<ExpressionDraft>();

        foreach (var draft in _expressions.Values)
        {
            if (draft.Type is not { } type)
            {
                found.Add(new DraftProblem(
                    DraftProblemKind.TypeNotChosen, draft.Id, "No kind of expression has been chosen."));
                unbuildable.Add(draft.Id);
                continue;
            }

            if (type is ExpressionDraftType.Variable)
            {
                if (draft.Dimensionality is null)
                {
                    found.Add(new DraftProblem(
                        DraftProblemKind.DimensionalityMissing, draft.Id, "A variable needs a dimensionality."));
                    unbuildable.Add(draft.Id);
                    continue;
                }

                candidates.Add(draft);
                continue;
            }

            var operands = draft.OperandIds;

            if (operands.Any(string.IsNullOrWhiteSpace))
            {
                found.Add(new DraftProblem(
                    DraftProblemKind.OperandMissing, draft.Id, "An operand slot is empty."));
                unbuildable.Add(draft.Id);
                continue;
            }

            if (! CountSuitsType(type, operands.Count, out var expected))
            {
                found.Add(new DraftProblem(
                    DraftProblemKind.OperandCountWrong,
                    draft.Id,
                    $"A {type} takes {expected}, not {operands.Count}."));
                unbuildable.Add(draft.Id);
                continue;
            }

            var dangling = operands.Where(id => ! _expressions.ContainsKey(id!)).ToList();
            if (dangling.Count > 0)
            {
                found.Add(new DraftProblem(
                    DraftProblemKind.ReferenceNotFound,
                    draft.Id,
                    $"No node in the draft has id {string.Join(", ", dangling)}."));
                unbuildable.Add(draft.Id);
                continue;
            }

            candidates.Add(draft);
        }

        return candidates;
    }

    private static bool CountSuitsType(ExpressionDraftType type, int count, out string expected)
    {
        switch (type)
        {
            case ExpressionDraftType.Negated:
            case ExpressionDraftType.Reciprocal:
            case ExpressionDraftType.Sqrt:
            case ExpressionDraftType.Exponential:
            case ExpressionDraftType.NaturalLog:
                expected = "exactly one operand";
                return count == 1;
            case ExpressionDraftType.Quotient:
                expected = "exactly two operands";
                return count == 2;
            case ExpressionDraftType.Sum:
            case ExpressionDraftType.Product:
                expected = "at least one operand";
                return count >= 1;
            default:
                expected = "no operands";
                return count == 0;
        }
    }

    /// <summary>
    /// Constructs every candidate whose operands are already constructed, repeatedly, until a pass achieves
    /// nothing.
    /// </summary>
    /// <remarks>
    /// Repeated passes rather than a topological sort, because what is left over when progress stops is exactly
    /// what could not be ordered — a cycle, or a node above something that failed — and the two are told apart
    /// afterwards. One mechanism serves both, and neither needs the order itself.
    /// </remarks>
    private static Resolved BuildExpressions(
        List<ExpressionDraft> candidates,
        List<DraftProblem> found,
        HashSet<string> unbuildable)
    {
        var resolved = new Resolved();
        var remaining = new List<ExpressionDraft>(candidates);

        while (true)
        {
            var progressed = false;

            for (var i = remaining.Count - 1; i >= 0; i--)
            {
                var draft = remaining[i];
                if (! draft.OperandIds.All(id => resolved.Holds(id!))) continue;

                remaining.RemoveAt(i);
                progressed = true;

                try
                {
                    var expression = Construct(draft, resolved);

                    // Forces the dimensional rules that are evaluated lazily rather than at construction —
                    // a Sqrt of an odd exponent builds happily and only refuses when asked its dimensionality.
                    _ = expression.Dimensionality;
                    resolved.Add(expression);
                }
                catch (Exception e)
                {
                    found.Add(new DraftProblem(DraftProblemKind.DomainRule, draft.Id, e.Message));
                    unbuildable.Add(draft.Id);
                }
            }

            if (! progressed) break;
        }

        ReportWhatCouldNotBeOrdered(remaining, found, unbuildable);
        return resolved;
    }

    // Everything left when progress stopped is waiting on an operand that never arrived. If that operand failed,
    // this node is a consequence rather than a cause and says nothing; otherwise the wait is circular.
    private static void ReportWhatCouldNotBeOrdered(
        List<ExpressionDraft> remaining,
        List<DraftProblem> found,
        HashSet<string> unbuildable)
    {
        bool spread;
        do
        {
            spread = false;
            foreach (var draft in remaining.Where(d => ! unbuildable.Contains(d.Id)))
            {
                if (! draft.OperandIds.Any(id => unbuildable.Contains(id!))) continue;

                unbuildable.Add(draft.Id);
                spread = true;
            }
        }
        while (spread);

        foreach (var draft in remaining.Where(d => ! unbuildable.Contains(d.Id)))
        {
            found.Add(new DraftProblem(
                DraftProblemKind.CircularReference,
                draft.Id,
                "This node is reached from its own operands."));
        }
    }

    private static IExpression Construct(ExpressionDraft draft, INodeResolver resolve) => draft.Type switch
    {
        ExpressionDraftType.Variable => BuildVariable(draft),

        ExpressionDraftType.Sum => ExpressionFactory.FromSnapshot(Nary(draft, NaryExpressionType.Sum), resolve),
        ExpressionDraftType.Product => ExpressionFactory.FromSnapshot(Nary(draft, NaryExpressionType.Product), resolve),

        ExpressionDraftType.Quotient => ExpressionFactory.FromSnapshot(
            new BinaryExpressionSnapshot(
                BinaryExpressionType.Quotient,
                draft.Id,
                draft.OperandIds[0]!,
                draft.OperandIds[1]!,
                draft.UncertaintyCorrelation),
            resolve),

        ExpressionDraftType.Negated => Unary(draft, UnaryExpressionType.Negated, resolve),
        ExpressionDraftType.Reciprocal => Unary(draft, UnaryExpressionType.Reciprocal, resolve),
        ExpressionDraftType.Sqrt => Unary(draft, UnaryExpressionType.Sqrt, resolve),
        ExpressionDraftType.Exponential => Unary(draft, UnaryExpressionType.Exponential, resolve),
        ExpressionDraftType.NaturalLog => Unary(draft, UnaryExpressionType.NaturalLog, resolve),

        _ => throw new ArgumentOutOfRangeException(
            nameof(draft), draft.Type, "Unknown kind of expression draft."),
    };

    private static NaryExpressionSnapshot Nary(ExpressionDraft draft, NaryExpressionType type) =>
        new(type, draft.Id, [.. draft.OperandIds.Select(id => id!)], draft.UncertaintyCorrelation);

    private static IExpression Unary(ExpressionDraft draft, UnaryExpressionType type, INodeResolver resolve) =>
        ExpressionFactory.FromSnapshot(new UnaryExpressionSnapshot(type, draft.Id, draft.OperandIds[0]!), resolve);

    // Built directly rather than through a snapshot: a variable's snapshot carries its value and provenance as
    // snapshots of their own, and the draft already holds both as the objects themselves.
    private static Variable BuildVariable(ExpressionDraft draft)
    {
        var variable = new Variable(draft.Symbol ?? draft.Id, draft.Dimensionality!.Value, draft.Id)
        {
            Provenance = draft.Provenance,
        };

        // Assigning rather than using the measurand constructor, so a value disagreeing with the declared
        // dimensionality is refused instead of silently redefining it.
        variable.Value = draft.Value;
        return variable;
    }

    private List<IBinaryOperator> BuildRelationships(
        Resolved built,
        List<DraftProblem> found,
        HashSet<string> unbuildable)
    {
        var operators = new List<IBinaryOperator>();

        foreach (var draft in _relationships.Values)
        {
            if (draft.Type is not { } type)
            {
                found.Add(new DraftProblem(
                    DraftProblemKind.TypeNotChosen, draft.Id, "No kind of relationship has been chosen."));
                continue;
            }

            if (! SideIsReady(draft.LhsId, built, unbuildable, draft.Id, "left", found)) continue;
            if (! SideIsReady(draft.RhsId, built, unbuildable, draft.Id, "right", found)) continue;

            if (type is BinaryOperatorType.Equality && draft.Agreement is null)
            {
                found.Add(new DraftProblem(
                    DraftProblemKind.RelationshipRuleMissing,
                    draft.Id,
                    "An equality needs an agreement rule saying how strictly it reads equal."));
                continue;
            }

            if (type is BinaryOperatorType.SimpleComparison && draft.Rule is null)
            {
                found.Add(new DraftProblem(
                    DraftProblemKind.RelationshipRuleMissing,
                    draft.Id,
                    "A simple comparison needs the comparison rule it asserts."));
                continue;
            }

            // Only an equality can carry a role of its own; the rest are requirements and have no way to say
            // otherwise, so an unstated role is the one they would have had anyway.
            var role = draft.SolvingRole ?? SolvingRole.Requirement;

            try
            {
                operators.Add(BinaryOperatorFactory.FromSnapshot(
                    new BinaryOperatorSnapshot(
                        type,
                        draft.Id,
                        draft.LhsId!,
                        draft.RhsId!,
                        role,
                        draft.Agreement,
                        draft.Rule,
                        draft.Name,
                        draft.Description,
                        draft.Provenance?.GetSnapshot()),
                    built));
            }
            catch (Exception e)
            {
                found.Add(new DraftProblem(DraftProblemKind.DomainRule, draft.Id, e.Message));
            }
        }

        return operators;
    }

    private bool SideIsReady(
        string? operandId,
        Resolved built,
        HashSet<string> unbuildable,
        string relationshipId,
        string side,
        List<DraftProblem> found)
    {
        if (string.IsNullOrWhiteSpace(operandId))
        {
            found.Add(new DraftProblem(
                DraftProblemKind.OperandMissing, relationshipId, $"The {side} side is empty."));
            return false;
        }

        if (built.Holds(operandId)) return true;

        // Silent where the operand is a node that already reported a problem of its own.
        if (! unbuildable.Contains(operandId) && ! _expressions.ContainsKey(operandId))
        {
            found.Add(new DraftProblem(
                DraftProblemKind.ReferenceNotFound,
                relationshipId,
                $"The {side} side names id {operandId}, which no node in the draft has."));
        }

        return false;
    }

    private static string NewId() => Guid.NewGuid().ToString();

    /// <summary>What has been constructed so far, and the resolver the factories look operands up through.</summary>
    private sealed class Resolved : INodeResolver
    {
        private readonly Dictionary<string, IExpression> _byId = [];

        internal IEnumerable<IExpression> Expressions => _byId.Values;

        internal bool Holds(string id) => _byId.ContainsKey(id);

        internal void Add(IExpression expression) => _byId[expression.Id] = expression;

        public TNode Resolve<TNode>(string id) where TNode : class, IIdentified =>
            _byId.TryGetValue(id, out var node) && node is TNode typed
                ? typed
                : throw new KeyNotFoundException($"No built node has id {id}.");
    }
}
