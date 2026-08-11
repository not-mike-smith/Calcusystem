# DimensionedExpression

The expression layer of Calcusystem. Builds trees of dimensioned variables and formulas — a system of equations whose leaves are measured/known values and whose interior nodes compute derived values with uncertainty propagation. Constraints and definitions are expressed as binary operators over those expressions.

Depends only on `Measurement` (for `Measurand`, `Dimensionality`, `ErrorPropagationMethod`). It has no dependency on serialization, evaluation, or solving.

---

## The central idea: lazy, dimension-checked expression trees

Every node in the tree is an `IExpression`. A node knows its `Dimensionality` *structurally* — always available, even before any values are supplied — but produces a value **only once every leaf it depends on has been given a value**. Until then `CalculateValueIfDetermined()` returns `null` and `IsFullyDescribed` is `false`.

```csharp
var mass  = new Variable("m", Dimensionality.Mass);
var accel = new Variable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

var force = new ProductExpression();
force.AddFactor(mass);
force.AddFactor(accel);

force.Dimensionality;      // M·L·T⁻²  — known immediately
force.IsFullyDescribed;    // false
force.CalculateValueIfDetermined();  // null
force.FreeVariables();     // [mass, accel]  — the distinct unbound leaves

mass.Value  = Mass.Kilogram.Quantity(2).WithError(1.0.Percent());
accel.Value = /* … */;

force.IsFullyDescribed;    // true once both are set
force.CalculateValueIfDetermined();  // a Measurand (value + propagated uncertainty)
```

**`CalculateValueIfDetermined()` is a method, and named for what it costs.** It walks the entire graph beneath the node on every call and caches nothing, so a sub-expression shared by three parents is computed three times. It was once a `Value` property, which invited callers to read it like a field and to call it in a loop.

Nothing is memoised on the node deliberately: a node cannot learn that a leaf beneath it was reassigned, so a cached answer there could silently go stale. Caching belongs to a caller that knows over what scope the graph is unchanged — `Calcusystem.Analysis`'s `system.Calculate()` computes each node exactly once per run by walking in dependency order and feeding results to `ComputeFrom`. **Prefer it for anything beyond a one-off read.**

Arithmetic and uncertainty propagation are delegated entirely to `Measurand` (see the Measurement README); this layer only assembles the graph and walks it.

Two consequences worth internalizing:

- **Dimensionality is total; value is partial.** Ask for `Dimensionality` any time. A null result from `CalculateValueIfDetermined()` *is* the "not fully described" answer, so prefer checking the result over calling `IsFullyDescribed` first — the latter is itself a walk, and asking both walks the graph twice.
- **`FreeVariables()` names the unbound leaves.** A fully-described tree has none. It is set-valued rather than a count because the graph is a **DAG, not a tree** — a shared sub-expression is reachable by several paths, and anything that asks "how many distinct unknowns" must deduplicate. It is also what the caller needs: a report has to name the missing values, not just tally them. Whether a whole *system* is solvable is a different question — see `Calcusystem.Analysis`.

---

## Type hierarchy

### Expression interfaces (`Interfaces/IExpression.cs`)

| Interface | Extends | Adds |
| --- | --- | --- |
| `IExpression` | | `Id`, `IsDirectlyMutable`, `IsFullyDescribed`, `Dimensionality`, `Children`, `ComputeFrom(known)` |
| `IDirectExpression` | `IExpression` | adds `Measurand? Value { get; set; }` — a mutable leaf's *stored* value. A genuine property: there is nothing beneath a leaf to walk. It no longer shadows anything, which is what kept the difference in cost between the two hidden. |
| `IComputedExpression` | `IExpression` | `ErrorPropagation { get; set; }` — the `ErrorPropagationMethod` used when combining children |

### Expression node types (`Expressions/`)

| Class | Implements | Role |
| --- | --- | --- |
| `Variable` | `IDirectExpression` | Leaf. Holds a settable `Measurand? Value`; setting a value of the wrong dimensionality throws `IncompatibleDimensionsException`. A leaf: `Children` is empty, and it is the only node type that can be a free variable. Constructible with just a dimensionality (unbound) or with an initial `Measurand`. Carries an optional `IProvenance` (see [Provenance](#provenance-interfacesiprovenancecs-provenanceprovenancefactorycs)). |
| `SumExpression` | `IComputedExpression` | n-ary `+` over `Addends`. All addends must share a dimensionality (enforced on `AddAddend`); constructor can seed a fixed dimensionality for an empty sum. |
| `ProductExpression` | `IComputedExpression` | n-ary `×` over `Factors`. Dimensionality is the product of its factors'. |
| `QuotientExpression` | `IComputedExpression` | `Numerator / Denominator` (both `required`). |
| `NegatedExpression` | `IExpression` | Unary negation wrapper over any `IExpression` (its `Operand`). Not directly mutable. |
| `ReciprocalExpression` | `IExpression` | Unary `1/x` wrapper over any `IExpression`; reciprocates the dimensionality. |
| `SqrtExpression` | `IExpression` | Unary `√x` over any `IExpression` (its `Argument`); halves each dimension exponent (odd exponent throws `NondiscreteDimensionalityException`). Uncertainty: `RelativeError(√x) = ½·RelativeError(x)`. |
| `ExponentialExpression` | `IExpression` | Unary `e^x`; argument must be dimensionless (enforced on construction/assignment), result dimensionless. Uncertainty: `RelativeError(eˣ) ≈ \|x\|·RelativeError(x)`. |
| `NaturalLogExpression` | `IExpression` | Unary `ln(x)`; argument must be dimensionless and positive, result dimensionless. Uncertainty: `AbsoluteError(ln x) ≈ RelativeError(x)`. Degenerate at `x = 1` (result 0 → relative error undefined; throws). |

Composite nodes (`Sum`/`Product`/`Quotient`) derive from `ComputedExpressionBase` (which supplies `Id`, `IsDirectlyMutable => false`, and the `ErrorPropagation` property); each still implements `Dimensionality`/`IsFullyDescribed`/`Children`/`ComputeFrom` itself.

### `ComputeFrom`: the node's arithmetic, without the walk

`ComputeFrom(known)` answers "given the values established so far, what is mine?" — the node's own arithmetic and uncertainty propagation, with the traversal that produced those operands factored out. The rule for an implementor is **look up yourself and your own children, nothing else**.

It is keyed by node rather than positional because position is a contract a caller can silently get wrong: handed a list, a quotient cannot tell numerator from denominator except by trusting the order, and computing `d/n` is not an error anything would catch. It also means a leaf can look *itself* up — which is the whole of the trial-value override mechanism, with no special case anywhere in any walk:

```csharp
// Variable
public Measurand? ComputeFrom(IReadOnlyDictionary<IExpression, Measurand> known) =>
    known.TryGetValue(this, out var supplied) ? supplied : _value;
```

`CalculateValueIfDetermined()` (an extension in `Traversal/`, written once for every node type) is that function applied to children which computed themselves recursively. `Calculate` is the same function applied to operands it computed in dependency order and kept. That is the whole point of the split: **a node owns how values combine; a caller owns the order they are produced in and whether any are worth keeping.**

### Traversal (`Traversal/ExpressionTraversal.cs`)

`Children` is the one accessor every graph walk goes through, so the walks are written **once** as extension methods rather than once per node type:

| Extension | Yields |
| --- | --- |
| `CalculateValueIfDetermined()` | the node's value, walking to the leaves — one implementation over `Children` + `ComputeFrom`, for every node type |
| `SelfAndDescendants()` | the node and everything reachable from it, each exactly once |
| `FreeVariables()` | the distinct unbound `Variable` leaves — on an `IExpression`, or on an `IBinaryOperator` across both its sides |

All deduplicate by identity (`IdBase` defines equality and hashing on `Id`). This matters: the per-type `DegreesOfFreedom()` these replaced summed over children, so an unknown referenced from two places was counted twice, and a system with one unknown reported two — enough to misclassify it as underdetermined at the solver gate.

### Binary operators (`BinaryOperators/`)

All operators implement `IBinaryOperator` (`Lhs`/`Rhs` expressions, `IsCommutative`, `bool? IsSatisfied()`, `AreBothSidesFullyDescribed`) via `BinaryOperatorBase` and its `CommutativeOperatorBase` / `NonCommutativeOperatorBase` splits. **`IsSatisfied()` returns `null` when either side is not fully described** — a three-valued result (`true` / `false` / `unknown`), not a bare bool.

There are three families — equality, tolerance (compatibility within uncertainty), and inequality (ordering, three strictness levels per direction). **The full taxonomy — every class, its symbol, commutativity, and exact interval condition — lives in [`BinaryOperators/OPERATORS.md`](BinaryOperators/OPERATORS.md).** Read that rather than the individual operator files.

One construction wrinkle: **`EqualityOperator` is the only operator with constructor arguments** — an `IEqualityEstimating` (the strategy deciding when two `Measurand`s count as equal) and `isDetermining` (below). Every other operator is constructed purely through `required` init properties:

```csharp
var op = new WhollyWithinToleranceOperator      { Id = Constants.CREATE_NEW, Lhs = measured, Rhs = spec };
var eq = new EqualityOperator(estimator, true)  { Id = Constants.CREATE_NEW, Lhs = a,        Rhs = b   };
```

### `IsDetermining` — equations vs. checks

`IBinaryOperator.IsDetermining` says whether a relationship *determines* a value (an equation a solver may use to compute an unknown) or merely *checks* one. It is what the degrees-of-freedom calculation counts against the unknowns; a non-determining relationship reduces DoF by nothing.

**Only `EqualityOperator` can be determining.** Ordering and tolerance relations yield an interval rather than a point, so no value can be derived from them: `BinaryOperatorBase.IsDetermining` returns `false` and the other twelve operators offer no constructor parameter to say otherwise. There is nothing to validate and nothing to throw — an operator that cannot determine cannot be built claiming it does.

`isDetermining` has **no default** on `EqualityOperator`, because both readings are common and neither is safe to assume: `mass_in == mass_out` is an equation to solve, `measured_T == design_T` is an assertion to check. Every construction states its intent.

---

## Identity: `IdBase` and `Constants.CREATE_NEW`

Every expression, operator, and system carries a string `Id` via `IdBase`. Passing the sentinel `Constants.CREATE_NEW` (the default on most constructors) generates a fresh GUID; passing an explicit id preserves it (this is what deserialization relies on to rebuild references). A null/whitespace id throws.

---

## `ExpressionSystem` (`Systems/ExpressionSystem.cs`)

The container for one coherent model. Create it via the factory (auto-generated id):

```csharp
var system = ExpressionSystem.Create("Newton's second law", "F = m·a");
```

It holds three lists plus a `Name`/`Description`:

| Member | Type | Purpose |
| --- | --- | --- |
| `DirectExpressions` | `List<Variable>` | the mutable leaf variables |
| `DerivedExpressions` | `List<IExpression>` | computed nodes built over those leaves |
| `Relationships` | `List<IBinaryOperator>` | every asserted relationship — definitions and constraints alike |

plus two read-only views over that third list:

| View | Contents |
| --- | --- |
| `Definitions` | relationships where `IsDetermining` — always-true relationships used to *compute* unknowns (conservation laws, constitutive equations) |
| `Constraints` | everything else — tolerance/ordering checks evaluated against values (pass / fail / unknown) |

`GetAllExpressions()` returns direct + derived. The scope of one `ExpressionSystem` is a single model (one equation of state, one heat exchanger); composing multiple systems into a flowsheet is a future (Milestone 5) concern.

**Add through `Relationships`.** Definitions and constraints share one list because which one a relationship is belongs to the operator — its `IsDetermining` — not to where it was filed. Two parallel lists would encode the same fact twice and let the two answers diverge; as views they cannot.

---

## Provenance (`Interfaces/IProvenance.cs`, `Provenance/ProvenanceFactory.cs`)

An optional audit annotation recording *where a value came from* — carried by both a leaf `Variable` (`Variable.Provenance`) and a relationship (`IBinaryOperator.Provenance`, e.g. a citation for a constitutive equation). It is attached by **composition, not inheritance**: the property is an `IProvenance?` (null = untracked), and provenance is purely descriptive — it never affects evaluation.

`IProvenance` exposes `Id` (it round-trips through serialization like any node) and `Summary()` (a one-line string for UI display). All kinds are created through the single factory — read `ProvenanceFactory` to see the full set available:

| Factory method | Kind | Metadata |
| --- | --- | --- |
| `ProvenanceFactory.Measured(instrumentId?, calibrationDate?)` | instrument/sensor reading | instrument id, calibration date |
| `ProvenanceFactory.Reference(citation, url?, year?)` | literature/tabulated value | citation, URL, year |
| `ProvenanceFactory.Design(specReference?)` | engineer-specified value | spec/drawing reference |
| `ProvenanceFactory.Model(modelName, fittingReference?)` | fitted constitutive constant | model name, fitting reference |

The concrete kinds (`MeasuredProvenance`, `ReferenceProvenance`, `DesignProvenance`, `ModelProvenance`) are **public** so callers can pattern-match on a kind, but their constructors **and their metadata** are **internal** — construction always flows through the factory, and the metadata leaves the assembly only as a `ProvenanceState`. The factory methods above mint a fresh identity and take no `id`; restoring a persisted one is `ProvenanceFactory.FromState(state)`, deliberately kept apart from the creation vocabulary so a caller recording where a value came from is never offered a parameter that only makes sense to a deserializer.

`IProvenance.GetState()` is implemented *explicitly*, so a consumer holding a `MeasuredProvenance` sees `Summary()` and `Id`, not the raw fields. Reading them is a persistence concern and this is its one door.

---

## Persistence: state, not DTOs

There are **no DTOs and no mappers in this assembly** — those live in `Calcusystem.Serialization`. What lives here is the *state* each type is defined by. This assembly answers "what data describes this node"; the persistence layer answers "how is that data encoded, versioned, and migrated". Records in `State/`:

| State | Discriminator | Covers |
| --- | --- | --- |
| `VariableState` | — | `Variable` |
| `UnaryExpressionState` | `UnaryExpressionKind` | `Reciprocal`, `Negated`, `Sqrt`, `Exponential`, `NaturalLog` |
| `NaryExpressionState` | `NaryExpressionKind` | `Product`, `Sum` |
| `BinaryExpressionState` | `BinaryExpressionKind` | `Quotient` (M5's `PowerExpression` joins by adding a kind) |
| `BinaryOperatorState` | `BinaryOperatorKind` | all thirteen operators |
| `ExpressionSystemState` | — | `ExpressionSystem` |
| `ProvenanceState` | `ProvenanceKind` | the four provenance kinds |

Grouped by **arity, not by type** — the kinds within a group differ in what they compute, not in what must be stored. The semantic difference lives in the discriminator, which is also what reconstruction dispatches on.

### Two seams, because a graph is not a value

`Variable` rebuilds from its own state alone, so it uses `IStateful<Variable, VariableState>` (from `Calcusystem.Core`). Every other node references neighbours **by id** — nesting them would duplicate shared sub-expressions and could not express the sharing at all — so they use `IStatefulNode<TSelf, TState>`, whose `FromState` also takes an `INodeResolver` to turn those ids back into nodes:

```csharp
public static ProductExpression FromState(NaryExpressionState state, INodeResolver resolve)
{
    var product = new ProductExpression { Id = state.Id, ErrorPropagation = state.ErrorPropagation };
    foreach (var id in state.InnerIds) product.AddFactor(resolve.Resolve<IExpression>(id));
    return product;
}
```

The axis is *does rebuilding need outside help*, not where a node sits in the tree — `Variable` is a genuine leaf, but that is incidental.

`INodeResolver.Resolve<TNode>(id)` is a per-reference query rather than one typed delegate because a node's neighbours need not share a type: `ExpressionSystem` refers to expressions in two of its lists and to operators in the other two. **Supplying the resolver, and rebuilding in an order that makes each referenced node available before it is asked for, is the caller's job** — that ordering is a persistence strategy, not domain knowledge. A resolver throws when an id cannot be resolved; a node is never asked to decide what a dangling reference means.

### Reconstruction gateways

Where a state carries a discriminator, the concrete type is chosen by inspecting it, so reconstruction is a static gateway over the closed set rather than a `static abstract` on each type — the same treatment `IUncertainty` and `IProvenance` get:

- `ExpressionFactory.FromState(state, resolve)` — one overload per arity, each delegating to the concrete type's own `FromState`, which is where per-type construction actually lives.
- `BinaryOperatorFactory.FromState(state, resolve, equalityEstimator)` — a gateway rather than per-type implementations, because construction is identical across all thirteen apart from which type is instantiated, and because `EqualityOperator` needs an `IEqualityEstimating` that a two-argument seam has nowhere to accept. `BinaryOperatorState.IsDetermining` is read only for the equality kind; the other twelve have no way to represent it, so reconstruction drops it rather than inventing an equation.
- `ProvenanceFactory.FromState(state)` — see [Provenance](#provenance-interfacesiprovenancecs-provenanceprovenancefactorycs).

If you are round-tripping an `ExpressionSystem` to storage, `Calcusystem.Serialization` is still the assembly to reach for; it consumes these seams.

---

## Scope boundaries

**What belongs here:** the `IExpression` tree and its node types, binary operators, `ExpressionSystem`, and the interfaces above.

**What does NOT belong here:**

- Physical quantities, units, dimensional algebra, uncertainty types, error propagation math → `Measurement`
- Serialization DTOs, wire formats, type-discriminator strings, and schema migration → `Calcusystem.Serialization`. The state records above are not an exception: a state record says *what data defines a node*, which only this assembly can answer; a DTO adds *how that data is labelled and encoded*, which is the persistence layer's business.
- Deciding the order in which a graph is rebuilt, or what a dangling id reference means → whatever supplies the `INodeResolver`
- Degrees of freedom for a *system*, calculating one, constraint reporting, and solving → `Calcusystem.Analysis` (this layer provides `ComputeFrom`, `IsFullyDescribed`, `Children`, and `FreeVariables()` as the primitives those build on, but performs no orchestration and keeps no cache itself)
