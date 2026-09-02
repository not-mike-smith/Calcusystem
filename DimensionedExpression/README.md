# DimensionedExpression

The expression layer of Calcusystem. Builds trees of dimensioned variables and formulas — a system of equations whose leaves are measured/known values and whose interior nodes compute derived values with uncertainty propagation. Constraints and definitions are expressed as binary operators over those expressions.

Depends only on `Measurement` (for `Measurand`, `Dimensionality`, `UncertaintyPropagation`). It has no dependency on serialization, evaluation, or solving.

---

## The central idea: lazy, dimension-checked expression trees

Every node in the tree is an `IExpression`. A node knows its `Dimensionality` *structurally* — always available, even before any values are supplied — but produces a value **only once every leaf it depends on has been given a value**. Until then `ComputeIfFullyDescribed()` returns `null` and `IsFullyDescribed` is `false`.

```csharp
var mass  = new Variable("m", Dimensionality.Mass);
var accel = new Variable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

var force = new ProductExpression([mass, accel]);

force.Dimensionality;      // M·L·T⁻²  — known immediately
force.IsFullyDescribed;    // false
force.ComputeIfFullyDescribed();  // null
force.UnsetVariables();     // [mass, accel]  — the distinct unbound leaves

mass.Value  = Mass.Kilogram.Quantity(2).WithError(1.0.Percent());
accel.Value = /* … */;

force.IsFullyDescribed;    // true once both are set
force.ComputeIfFullyDescribed();  // a Measurand (value + propagated uncertainty)
```

**`ComputeIfFullyDescribed()` is a method, and named for what it costs.** It walks the entire graph beneath the node on every call and caches nothing, so a sub-expression shared by three parents is computed three times. It was once a `Value` property, which invited callers to read it like a field and to call it in a loop.

Nothing is memoised on the node deliberately: a node cannot learn that a leaf beneath it was reassigned, so a cached answer there could silently go stale. Caching belongs to a caller that knows over what scope the graph is unchanged — `Calcusystem.Analysis`'s `system.Calculate()` computes each node exactly once per run by walking in dependency order and feeding results to `ComputeFrom`. **Prefer it for anything beyond a one-off read.**

Arithmetic and uncertainty propagation are delegated entirely to `Measurand` (see the Measurement README); this layer only assembles the graph and walks it.

### Structure is immutable; values are not

**What an expression is built *from* is fixed at construction. What a leaf is *worth* can change.** Operands are constructor arguments — `new ProductExpression([m, a])` — and there is no `AddFactor`, no operand setter, and no way to re-point a relationship's `Lhs`. `Variable.Value` stays settable, because supplying and revising values is the entire point of a leaf.

The split is what makes the rest of the layer safe to reason about:

- **The staleness question shrinks to one axis.** A cache can only be invalidated by a value change, never by the graph rearranging underneath it. That is why `Calculate` can memoise for the duration of a run, and why nothing needs to detect structural edits.
- **Cycles become unconstructible** through these types, since a node's operands must exist before it does.
- **Validation happens once, where the operand arrives.** `SumExpression` checks that its addends share a dimensionality in its constructor and nowhere else; the unary types check dimensionlessness there and nowhere else. There is no second path to keep in agreement.

Two further consequences worth internalizing:

- **Dimensionality is total; value is partial.** Ask for `Dimensionality` any time. A null result from `ComputeIfFullyDescribed()` *is* the "not fully described" answer, so prefer checking the result over calling `IsFullyDescribed` first — the latter is itself a walk, and asking both walks the graph twice.
- **`UnsetVariables()` names the unbound leaves.** A fully-described tree has none. It is set-valued rather than a count because the graph is a **DAG, not a tree** — a shared sub-expression is reachable by several paths, and anything that asks "how many distinct unknowns" must deduplicate. It is also what the caller needs: a report has to name the missing values, not just tally them. Whether a whole *system* is solvable is a different question — see `Calcusystem.Analysis`.

---

## Type hierarchy

### Expression interfaces (`Interfaces/IExpression.cs`)

| Interface | Extends | Adds |
| --- | --- | --- |
| `IExpression` | | `Id`, `IsDirectlyMutable`, `IsFullyDescribed`, `Dimensionality`, `Children`, `ComputeFrom(known)` |
| `IDirectExpression` | `IExpression` | adds `Measurand? Value { get; set; }` — a mutable leaf's *stored* value. A genuine property: there is nothing beneath a leaf to walk. It no longer shadows anything, which is what kept the difference in cost between the two hidden. |
| `IComputedExpression` | `IExpression` | `UncertaintyPropagation { get; set; }` — the `UncertaintyPropagation` used when combining children |

### Expression node types (`Expressions/`)

| Class | Implements | Role |
| --- | --- | --- |
| `Variable` | `IDirectExpression` | Leaf. Holds a settable `Measurand? Value`; setting a value of the wrong dimensionality throws `IncompatibleDimensionsException`. A leaf: `Children` is empty, and it is the only node type that can be a free variable. Constructible with just a dimensionality (unbound) or with an initial `Measurand`. Carries an optional `IProvenance` (see [Provenance](#provenance-interfacesiprovenancecs-provenanceprovenancefactorycs)). |
| `SumExpression` | `IComputedExpression` | n-ary `+` over `Addends`, supplied at construction. All addends must share a dimensionality, enforced there. |
| `ProductExpression` | `IComputedExpression` | n-ary `×` over `Factors`. Dimensionality is the product of its factors'. |
| `QuotientExpression` | `IComputedExpression` | `Numerator / Denominator` (both `required`). |
| `NegatedExpression` | `IExpression` | Unary negation wrapper over any `IExpression` (its `Operand`). Not directly mutable. |
| `ReciprocalExpression` | `IExpression` | Unary `1/x` wrapper over any `IExpression`; reciprocates the dimensionality. |
| `SqrtExpression` | `IExpression` | Unary `√x` over any `IExpression` (its `Argument`); halves each dimension exponent (odd exponent throws `NondiscreteDimensionalityException`). Uncertainty: `RelativeUncertainty(√x) = ½·RelativeUncertainty(x)`. |
| `ExponentialExpression` | `IExpression` | Unary `e^x`; argument must be dimensionless (enforced on construction/assignment), result dimensionless. Uncertainty: `RelativeUncertainty(eˣ) ≈ \|x\|·RelativeUncertainty(x)`. |
| `NaturalLogExpression` | `IExpression` | Unary `ln(x)`; argument must be dimensionless and positive, result dimensionless. Uncertainty: `AbsoluteUncertainty(ln x) ≈ RelativeUncertainty(x)`. Degenerate at `x = 1` (result 0 → relative error undefined; throws). |

Composite nodes (`Sum`/`Product`/`Quotient`) derive from `ComputedExpressionBase` (which supplies `Id`, `IsDirectlyMutable => false`, and the `UncertaintyPropagation` property); each still implements `Dimensionality`/`IsFullyDescribed`/`Children`/`ComputeFrom` itself.

### `ComputeFrom`: the node's arithmetic, without the walk

`ComputeFrom(known)` answers "given the values established so far, what is mine?" — the node's own arithmetic and uncertainty propagation, with the traversal that produced those operands factored out. The rule for an implementor is **look up yourself and your own children, nothing else**.

It is keyed by node rather than positional because position is a contract a caller can silently get wrong: handed a list, a quotient cannot tell numerator from denominator except by trusting the order, and computing `d/n` is not an error anything would catch. It also means a leaf can look *itself* up — which is the whole of the trial-value override mechanism, with no special case anywhere in any walk:

```csharp
// Variable
public Measurand? ComputeFrom(IReadOnlyDictionary<IExpression, Measurand> known) =>
    known.TryGetValue(this, out var supplied) ? supplied : _value;
```

`ComputeIfFullyDescribed()` (an extension in `Traversal/`, written once for every node type) is that function applied to children which computed themselves recursively. `Calculate` is the same function applied to operands it computed in dependency order and kept. That is the whole point of the split: **a node owns how values combine; a caller owns the order they are produced in and whether any are worth keeping.**

### The derived walks (`BaseModels/ExpressionBase.cs`)

A node type contributes exactly two things: **what its operands are** (`Children`) and **how their values combine** (`ComputeFrom`). Everything else a node can be asked follows from those, has one sensible implementation, and lives on `ExpressionBase` — so adding a node type never means rewriting any of it:

| Extension | Yields |
| --- | --- |
| `ComputeIfFullyDescribed(overrides?, propagator?)` | the node's value, walking to the leaves. Named to match `ComputeFrom`, and takes the same overrides `Calculate` does, for a caller working on one sub-expression |
| `SelfAndDescendants()` | the node and everything reachable from it, each exactly once |
| `InDependencyOrder()` | children before parents — the order values can be computed in |

`ExpressionSystem.InDependencyOrder()` is the same walk over a whole system, which is the only form ranging over several roots at once — there is no single node to ask, so it sits on the system beside `GetAllExpressions()`. Both delegate to an internal `ExpressionGraph`; neither callers nor node types touch it.
| `UnsetVariables()` | the distinct unbound `Variable` leaves — on an `IExpression`, or on an `IBinaryOperator` across both its sides |

They are **declared on `IExpression` and implemented on `ExpressionBase`** rather than being extension methods, so they are part of the contract and visible on the interface. The cost is that a type implementing `IExpression` without deriving from `ExpressionBase` must supply all of them; deriving is the expected path, and the test doubles do.

All deduplicate by identity (`IdBase` defines equality and hashing on `Id`), and all are iterative — nothing bounds how deep a graph can be, and a stack frame per node is an avoidable way to fail.

**Cycles are detected, not assumed away.** They are now unconstructible through this assembly's own types — a node's operands are supplied at construction and never change, so a node's children always predate it — but `IExpression` is a public interface, and an implementation outside this assembly can present whatever `Children` it likes. Every walk here assumes a DAG, so the check stays. A visited set alone only stops the descent; it leaves a node ordered before an operand it depends on, so a caller folding over that order finds the operand missing and reports a value as unresolvable when nothing is actually absent. `InDependencyOrder` therefore verifies that every node follows all of its own children, and `ComputeIfFullyDescribed()` goes through it. This matters: the per-type `DegreesOfFreedom()` these replaced summed over children, so an unknown referenced from two places was counted twice, and a system with one unknown reported two — enough to misclassify it as underdetermined at the solver gate.

### Binary operators (`BinaryOperators/`)

All operators implement `IBinaryOperator` (`Lhs`/`Rhs` expressions, `IsCommutative`, `Symbol`, `AreBothSidesFullyDescribed`) via `BinaryOperatorBase` and its `CommutativeOperatorBase` / `NonCommutativeOperatorBase` splits.

**A verdict comes in two halves**, mirroring `ComputeFrom` / `ComputeIfFullyDescribed` on expressions:

| | Answers | Reads |
| --- | --- | --- |
| `bool IsSatisfiedGiven(lhs, rhs)` | the predicate over two supplied values | nothing — a pure function |
| `bool? IsSatisfied(overrides?, propagator?)` | the same, having resolved both sides first | the model, plus any `overrides` |

**`IsSatisfied()` returns `null` when either side does not resolve** — a three-valued result (`true` / `false` / `unknown`), not a bare bool. Each operator supplies only the predicate; resolving both sides and answering `null` if either is missing is identical for all thirteen, so it lives on the base class rather than being copied thirteen times.

The split exists because **a verdict must be a function of the values it was handed.** `Calculate` has already computed every node; if it asked each operator instead, the operator would re-walk both subgraphs — twice per relationship — and, worse, would resolve them against the *stored* model, so a calculation run at trial values would quietly report checks against values it was told to ignore. See `Calcusystem.Analysis` for the outcomes it produces.

There are three families — equality, tolerance (compatibility within uncertainty), and inequality (ordering, three strictness levels per direction). **The full taxonomy — every class, its symbol, commutativity, and exact interval condition — lives in [`BinaryOperators/OPERATORS.md`](BinaryOperators/OPERATORS.md).** Read that rather than the individual operator files.

### Operators declare, they do not compare

Each operator supplies `IReadOnlyList<ComparisonRule> Rules` — a landmark of the subject against a landmark of the criterion, at a stated strictness — and the base class ANDs them. Every one of the fourteen turned out to be such a conjunction, so none of them writes interval arithmetic and none of them decides what "less than" means. That happens in exactly one place, `MeasurandComparer`, which is also where tolerance, dimensional mismatch and non-finite values are handled.

The verdict is therefore **three-valued**: `IsSatisfiedGiven` returns `bool?`, and `null` means the comparison has no answer rather than that the relationship failed. `ExpressionSystem.Add` refuses a cross-dimensional relationship outright, so in practice this arises from non-finite values.

### The confidence ladder

Under uncertainty a comparison has several nested answers, and the arithmetic producing any of them produces all of them. The ladders give those answers a vocabulary:

- **`OrderingLadder`** — `Possible` / `Nominal` / `Certain`, a clean chain, over a named `OrderingDirection`. It is a **classifier**, not an evaluator: `RungOf` asks which rung a rule is, `AchievedTier` computes the strongest tier reached *when asked* and stops as soon as it knows. Operators do not go through it — they declare their own rules and the ladder places them afterwards, so nothing has to know that tiers are less-than by convention.
- **`ContainmentLadder`** — `Overlaps` / `NominalWithin` / `NominalAndUpperWithin` / `NominalAndLowerWithin` / `WhollyWithin`, and a classifier for the same reasons: `RungOf` places a rule *set* on the ladder, `Reaches` evaluates one rung on demand. The middle rungs form a **lattice, not a chain**, because upper and lower bounds are independently checkable — so there is deliberately no single ordered "achieved rung" here.

`UpperBoundsLessThan` and `LowerBoundsGreaterThan` sit outside both because they compare a derived *statistic* of each side — ceiling against ceiling, floor against floor — rather than asking how the quantities stand to one another.

The named operators are kept as vocabulary: `AnyToleranceOverlap` says what it means better than "the bottom rung of the containment ladder". The modeller also gets back more than they asked for — author "do these overlap at all", get `true`, and be able to learn the achieved rung was "wholly contained".

Two operators take constructor arguments; every other is constructed purely through `required` init properties:

```csharp
var op = new WhollyWithinToleranceOperator { Id = Constants.CREATE_NEW, Lhs = measured, Rhs = spec };

// How strictly "equal" is read is the modeller's call, and part of the model — not a strategy the reader supplies.
var eq = new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
    { Id = Constants.CREATE_NEW, Lhs = a, Rhs = b };

// The general form: any of the 63 rules, including the ones with no named operator.
var conservative = new SimpleComparison(
        new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.LowerBound))
    { Id = Constants.CREATE_NEW, Lhs = measured, Rhs = guarantee };
```

### `SolvingRole` — what a relationship does to the problem

`IBinaryOperator.SolvingRole` says what a relationship contributes:

| Role | Meaning | Effect on DoF |
| --- | --- | --- |
| `Equation` | contributes a residual a solver drives to zero — `mass_in == mass_out` | removes one |
| `Coherence` | asserts that separately computed routes to one quantity agree — `T_eos == T_path` | removes one |
| `Requirement` | bounds a value without producing one — `T_out < T_max` | removes none |

`IsDetermining` remains, **derived** as `Equation or Coherence` — that's the question degrees-of-freedom code actually asks, and deriving it means it can't disagree with the role.

**Why three and not a boolean.** "Not an equation" was doing two jobs. And the `Equation`/`Coherence` split is *not recoverable from the predicate* — both assert equality, and only the modeller knows whether one side defines a quantity or the two are independent routes to it. A solver wants that intent: any route is a usable initial estimate for the others, and a coherence group is where to relax an over-determined system. It's also why the **wire stores the role, not `IsDetermining`** — a boolean writes `true` for both and they can't be told apart again on load.

**Not** in here: whether a requirement is *enforced or merely reported*. That's a search policy belonging to whoever asks for a solve, while this is structure the model owns.

**Only `EqualityOperator` can be anything but `Requirement`.** Ordering and tolerance relations confine a value to an interval rather than producing a point, so nothing can be derived from them: `BinaryOperatorBase.SolvingRole` returns `Requirement` and the other twelve offer no constructor parameter to say otherwise. Nothing to validate, nothing to throw — an operator that cannot determine cannot be built claiming it does.

`solvingRole` has **no default** on `EqualityOperator`, because all three readings are common and none is safe to assume. Every construction states its intent.

### `Subject` and `Criterion` — the roles of the two *sides*

A different axis from `SolvingRole`, and the reason that enum is named for the axis rather than the carrier: this is about presenting a result and says nothing about the shape of the problem. `IExpression? Subject` and `IExpression? Criterion` say whether a relationship distinguishes its operands at all.

| `SolvingRole` | `Subject` | `Criterion` |
| --- | --- | --- |
| `Requirement` | `Lhs` — the value under test | `Rhs` — what it is tested against |
| `Equation` | `null` | `null` |
| `Coherence` | `null` | `null` |

Twelve of the thirteen operators are always requirements, and their `Lhs` is the value under test by construction — which is what the table in [`OPERATORS.md`](BinaryOperators/OPERATORS.md) has always documented. An `Equation` or `Coherence` has no such asymmetry: neither side of `T_eos == T_path` is the one being judged, and labelling one would invent an authority the model never asserted.

"Criterion" rather than "reference", which is already spoken for by `ProvenanceFactory.Reference`, and rather than "expected", which lies about corroboration — two peers compared, neither expected — and about a failed equation, where neither side is the authority.

**Derived, never stored.** There is deliberately no side-labelling enum, so nothing sits beside the operands that a later change could leave pointing at the wrong one. The consequence worth knowing: `Criterion is not null` is *exactly* `SolvingRole is Requirement`, which is also what separates a **violation** from an **inconsistency** in a calculation's outcomes. The role structure and the finding taxonomy turn out to be one distinction viewed twice.

Note `SolvingRole` has **no zero member** — none of the three means "no role", so an unsupplied value is detectably invalid rather than silently a `Requirement`.

---

## Identity: `IdBase` and `Constants.CREATE_NEW`

Every expression, operator, and system carries a string `Id` via `IdBase`. Passing the sentinel `Constants.CREATE_NEW` (the default on most constructors) generates a fresh GUID; passing an explicit id preserves it (this is what deserialization relies on to rebuild references). A null/whitespace id throws.

---

## `ExpressionSystem` (`Systems/ExpressionSystem.cs`)

The container for one coherent model. Create it via the factory (auto-generated id):

```csharp
var system = ExpressionSystem.Create("Newton's second law", "F = m·a");
```

**Everything goes in through `Add`**, and the collections are read-only:

```csharp
system.Add(mass);                                            // a variable
system.Add(force);                                           // a composite, and everything beneath it
system.Add(new DefinitelyLessThanOperator { … });            // a relationship, and both of its operands
```

| Member | Contents |
| --- | --- |
| `Variables` | every `Variable` the system contains |
| `DerivedExpressions` | every computed node it contains, including nodes nested inside others |
| `Relationships` | every asserted relationship — definitions and constraints alike |

plus three read-only views over that third one, one per [`SolvingRole`](#solvingrole--what-a-relationship-does-to-the-problem):

| View | Contents |
| --- | --- |
| `Equations` | relationships that define a quantity — conservation laws, constitutive equations |
| `CoherenceChecks` | relationships asserting that separately computed routes to one quantity agree |
| `Requirements` | everything else — tolerance/ordering checks evaluated against values (pass / fail / unknown) |

### Membership is reachability

**`Add` absorbs the whole subgraph beneath what it is given.** Hand it a product and its factors join the system; hand it a relationship and both operands do, along with anything beneath them. So `Variables` is not "the variables you mentioned by name" — it is every variable the system reaches, and `GetAllExpressions()` is complete rather than a subset.

This is the same rule that made those three views rather than lists. *What the system contains* and *what the system reaches* are two ways of asking one question, and any design that answers them separately eventually answers them differently. Concretely, it did: a limit compared against but never filed was invisible to `Calculate` while `Flatten` counted it, and a node nested inside another was referenced by id on the wire without ever being written.

Absorbing **eagerly** is safe only because an expression's operands are fixed at construction — see [Structure is immutable](#structure-is-immutable-values-are-not). Were the graph able to change afterwards, a set captured at `Add` time could drift from what the graph holds, and the collections would need re-deriving on every read.

The scope of one `ExpressionSystem` is a single model (one equation of state, one heat exchanger); composing multiple systems into a flowsheet is a future (Milestone 5) concern.

---

## Provenance (`Interfaces/IProvenance.cs`, `Provenance/ProvenanceFactory.cs`)

An optional audit annotation recording *where a value came from* — carried by both a leaf `Variable` (`Variable.Provenance`) and a relationship (`IBinaryOperator.Provenance`, e.g. a citation for a constitutive equation). It is attached by **composition, not inheritance**: the property is an `IProvenance?` (null = untracked), and provenance is purely descriptive — it never affects evaluation.

`IProvenance` exposes `Id` (it round-trips through serialization like any node) and `Summary()` (a one-line string for UI display). All kinds are created through the single factory — read `ProvenanceFactory` to see the full set available:

| Factory method | Type | Metadata |
| --- | --- | --- |
| `ProvenanceFactory.Measured(instrumentId?, calibrationDate?)` | instrument/sensor reading | instrument id, calibration date |
| `ProvenanceFactory.Reference(citation, url?, year?)` | literature/tabulated value | citation, URL, year |
| `ProvenanceFactory.Design(specReference?)` | engineer-specified value | spec/drawing reference |
| `ProvenanceFactory.Model(modelName, fittingReference?)` | fitted constitutive constant | model name, fitting reference |

The concrete kinds (`MeasuredProvenance`, `ReferenceProvenance`, `DesignProvenance`, `ModelProvenance`) are **public** so callers can pattern-match on a kind, but their constructors **and their metadata** are **internal** — construction always flows through the factory, and the metadata leaves the assembly only as a `ProvenanceSnapshot`. The factory methods above mint a fresh identity and take no `id`; restoring a persisted one is `ProvenanceFactory.FromSnapshot(state)`, deliberately kept apart from the creation vocabulary so a caller recording where a value came from is never offered a parameter that only makes sense to a deserializer.

`IProvenance.GetSnapshot()` is implemented *explicitly*, so a consumer holding a `MeasuredProvenance` sees `Summary()` and `Id`, not the raw fields. Reading them is a persistence concern and this is its one door.

---

## Persistence: state, not DTOs

There are **no DTOs and no mappers in this assembly** — those live in `Calcusystem.Serialization`. What lives here is the *state* each type is defined by. This assembly answers "what data describes this node"; the persistence layer answers "how is that data encoded, versioned, and migrated". Records in `State/`:

| State | Discriminator | Covers |
| --- | --- | --- |
| `VariableSnapshot` | — | `Variable` |
| `UnaryExpressionSnapshot` | `UnaryExpressionType` | `Reciprocal`, `Negated`, `Sqrt`, `Exponential`, `NaturalLog` |
| `NaryExpressionSnapshot` | `NaryExpressionType` | `Product`, `Sum` |
| `BinaryExpressionSnapshot` | `BinaryExpressionType` | `Quotient` (M5's `PowerExpression` joins by adding a kind) |
| `BinaryOperatorSnapshot` | `BinaryOperatorType` | all thirteen operators |
| `ExpressionSystemSnapshot` | — | `ExpressionSystem` |
| `ProvenanceSnapshot` | `ProvenanceType` | the four provenance kinds |

Grouped by **arity, not by type** — the kinds within a group differ in what they compute, not in what must be stored. The semantic difference lives in the discriminator, which is also what reconstruction dispatches on.

### Two seams, because a graph is not a value

`Variable` rebuilds from its own state alone, so it uses `ISnapshotting<Variable, VariableSnapshot>` (from `Calcusystem.Core`). Every other node references neighbours **by id** — nesting them would duplicate shared sub-expressions and could not express the sharing at all — so they use `ISnapshottingNode<TSelf, TSnapshot>`, whose `FromSnapshot` also takes an `INodeResolver` to turn those ids back into nodes:

```csharp
public static ProductExpression FromSnapshot(NaryExpressionSnapshot state, INodeResolver resolve) =>
    new(state.InnerIds.Select(resolve.Resolve<IExpression>))
    {
        Id = state.Id,
        UncertaintyPropagation = state.UncertaintyPropagation,
    };
```

The axis is *does rebuilding need outside help*, not where a node sits in the tree — `Variable` is a genuine leaf, but that is incidental.

`INodeResolver.Resolve<TNode>(id)` is a per-reference query rather than one typed delegate because a node's neighbours need not share a type: `ExpressionSystem` refers to expressions in two of its lists and to operators in the other two. **Supplying the resolver, and rebuilding in an order that makes each referenced node available before it is asked for, is the caller's job** — that ordering is a persistence strategy, not domain knowledge. A resolver throws when an id cannot be resolved; a node is never asked to decide what a dangling reference means.

### Reconstruction gateways

Where a state carries a discriminator, the concrete type is chosen by inspecting it, so reconstruction is a static gateway over the closed set rather than a `static abstract` on each type — the same treatment `IUncertainty` and `IProvenance` get:

- `ExpressionFactory.FromSnapshot(state, resolve)` — one overload per arity, each delegating to the concrete type's own `FromSnapshot`, which is where per-type construction actually lives.
- `BinaryOperatorFactory.FromSnapshot(state, resolve)` — a gateway rather than per-type implementations, because construction is identical across all fourteen apart from which type is instantiated. `BinaryOperatorSnapshot.SolvingRole` is read only for the equality kind; the others have no way to represent anything but `Requirement`, so reconstruction drops it rather than inventing an equation. Two kinds carry state of their own — an equality's `AgreementRule` and a simple comparison's `ComparisonRule` — and reconstruction *refuses* a state missing either rather than guessing, since a guessed reading is exactly the ambiguity storing them removed.
- `ProvenanceFactory.FromSnapshot(state)` — see [Provenance](#provenance-interfacesiprovenancecs-provenanceprovenancefactorycs).

If you are round-tripping an `ExpressionSystem` to storage, `Calcusystem.Serialization` is still the assembly to reach for; it consumes these seams.

---

## Scope boundaries

**What belongs here:** the `IExpression` tree and its node types, binary operators, `ExpressionSystem`, and the interfaces above.

**What does NOT belong here:**

- Physical quantities, units, dimensional algebra, uncertainty types, error propagation math → `Measurement`
- Serialization DTOs, wire formats, type-discriminator strings, and schema migration → `Calcusystem.Serialization`. The state records above are not an exception: a state record says *what data defines a node*, which only this assembly can answer; a DTO adds *how that data is labelled and encoded*, which is the persistence layer's business.
- Deciding the order in which a graph is rebuilt, or what a dangling id reference means → whatever supplies the `INodeResolver`
- Degrees of freedom for a *system*, calculating one, constraint reporting, and solving → `Calcusystem.Analysis` (this layer provides `ComputeFrom`, `IsFullyDescribed`, `Children`, and `UnsetVariables()` as the primitives those build on, but performs no orchestration and keeps no cache itself)
