# Calcusystem.Analysis

Asks whether an `ExpressionSystem` is well-posed. Given a system, it reports how many values are still needed, whether the relationships available can supply them, and which unknowns nothing can reach.

Depends on `DimensionedExpression` and `Measurement`. It reads the expression graph and never mutates it.

This is where the evaluation walk (Milestone 3) and the solver abstraction (Milestone 4) will live. It exists as its own assembly because `DimensionedExpression` deliberately performs no orchestration — it supplies `Value`, `IsFullyDescribed`, `Children`, and `FreeVariables()`, and stops there.

---

## The central idea: flatten first, then analyse

Degrees of freedom is the classic process-engineering quantity:

```
DoF = unknowns − determining equations
```

Computing it means reducing a system to exactly those two things. `SystemFlattener.Flatten` produces a `FlatSystem` holding them, and everything else is read off that.

```csharp
var flat = SystemFlattener.Flatten(system);

flat.Unknowns;             // the distinct unbound Variables
flat.Equations;            // determining relationships, each with its incident unknowns
flat.DegreesOfFreedom;     // Unknowns.Count - Equations.Count
flat.Determination;        // Underdetermined / ExactlyDetermined / Overdetermined
flat.UnknownsWithNoEquation; // unknowns no equation touches (a constraint is not an equation)
```

### What becomes a row, a column, or neither

| In the system | In the flat system |
| --- | --- |
| unbound `Variable` | an **unknown** (a column) |
| bound `Variable` | nothing — it is already known |
| computed node (`Product`, `Reciprocal`, …) | **nothing** — it is the *path* by which an equation reaches a leaf |
| relationship where `IsDetermining` | an **equation** (a row) |
| relationship where not | nothing — a check removes no degree of freedom |

The third row is the one worth internalising. Given `a`, `b = 1/a`, and the equation `b == c`, the flat system has columns `a` and `c` and one row; `b` appears only as incidence, putting a mark in column `a`. Admitting `b` as an unknown would add a column *and* force a compensating row (`b = 1/a`), leaving DoF unchanged while doubling the size of the problem. Only a `Variable` can be assigned, so only a `Variable` can be an unknown.

A corollary worth relying on: **valuing a leaf and asserting an equation against a constant agree about DoF.** Setting `c.Value = 2 s` removes one column; writing `c == 2s` instead keeps that column but adds a row. The modeller's choice of style cannot corrupt the arithmetic.

---

## Calculating

`Calculate` works out everything the system's current values and relationships determine, and reports what it could not and why.

```csharp
var calc = system.Calculate(overrides);

calc.Overrides;      // the values supplied — the assumptions this calculation rests on
calc.Values;         // every node that resolved
calc.ValueOf(f);     // one node's value, or null
calc.Unresolved;     // the expressions it references that could not be computed
calc.MissingValues;  // the unbound variables responsible
calc.IsComplete;     // nothing outstanding
```

**Named for the engineering artefact, not the operation.** A calculation is a thing an engineer produces, keeps, and hands to a reviewer — and it is defined as much by its inputs as its outputs, which is why `Overrides` rides on the record. A bare set of values is not reproducible or reviewable without the assumptions that produced it, and carrying both is what lets two calculations of the same system be compared on equal terms.

It never throws on an incomplete system. A model half-built is the normal case, and "which values are still missing" is the answer the caller wants.

It *does* throw on a **cyclic** graph — `CyclicExpressionGraphException`. That is not an incomplete model but a malformed one, and reporting it as unresolved would produce a calculation claiming nodes could not be computed while listing nothing as missing, which reads as an absent value and sends the reader looking for one that does not exist.

**It covers what the system *references*, not only what it lists.** A relationship's operands are reachable from the system without appearing in `DirectExpressions` or `DerivedExpressions` — a limit compared against, or an expression assembled purely for the comparison — so `Calculate` roots its walk at `GetReferencedExpressions()`. Narrowing to the two lists left such a node uncomputed and absent from `MissingValues`, which surfaced as a `null` value indistinguishable from "you have not supplied enough", and put `Calculate` at odds with `Flatten`, which has always gathered unknowns through `Relationships`.

**Each node is computed once.** Nodes are visited in dependency order and handed the values already established, via `IExpression.ComputeFrom`. Contrast `CalculateValueIfDetermined()`, which re-walks to the leaves on every call — a sub-expression shared by three parents costs three walks there and one here. This is the caching a node deliberately cannot do for itself: a node has no way to learn that a leaf beneath it was reassigned, whereas `Calculate` knows the graph is unchanged for the duration of a run.

`Calculation` is a snapshot, not a live view: a pure function of the system and its overrides, holding immutable `Measurand`s. Later assignments do not change it; re-running is how you get a newer one. `Values` covers every node reached, which is what makes it the natural home for caching across runs too.

### Uncertainty treatment

`Calculate` also takes an optional `IErrorPropagator`, defaulting to the conservative Gaussian one. This is the seam for an alternative uncertainty model — Monte Carlo, correlation-aware — applied to a whole calculation.

It is deliberately a *different axis* from a computed node's `ErrorPropagation`:

| | Says | Belongs to |
| --- | --- | --- |
| `IComputedExpression.ErrorPropagation` | are *these* operands correlated? | the **model** — a physical fact about the quantities |
| `IErrorPropagator` | how do uncertainties combine at all? | the **calculation** — a numerical method |

Both are passed through together, so choosing a propagator never discards what the model records about correlation. A global switch that flattened everything to "assume correlated" would be the opposite: it would silently throw away modelling knowledge, e.g. a node marked correlated because both its inputs come off the same instrument. There is a test that an injected propagator still sees `Correlated` where the model said so.

### Why it is not async, and does not parallelise internally

`Calculate` is CPU-bound with no I/O, so `async` would buy nothing and cost every caller an `await` up their whole stack. Parallelising *within* one system is possible in principle but unpromising: the dependency graph is largely sequential, and the work per node — a handful of floating-point operations on a `Measurand` — is far smaller than the coordination overhead.

The parallelism worth having is across *independent* calculations, and purity already provides it with no API at all:

```csharp
var curve = trials
    .AsParallel()
    .Select(t => system.Calculate(new Dictionary<Variable, Measurand> { [x] = t }))
    .ToList();
```

Nothing is mutated and nothing is shared but immutable reads, so this is safe today; a test runs 500 of them in parallel. That is the payoff for keeping the memo outside the graph rather than caching on nodes. If intra-system parallelism ever does become worth it, the block-triangular ordering from Milestone 4's structural analysis is what would identify the independent blocks — and it would be an internal change, not a signature one.

---

## `overrides`: probing without mutating

Both entry points take an optional `IReadOnlyDictionary<Variable, Measurand>`. A variable named there is not an unknown, and calculates to the supplied value, whatever its own `Value` says.

```csharp
var pinned = system.Flatten(new Dictionary<Variable, Measurand> { [m] = trial });
```

Keyed by the variable itself rather than by its id: an id that matches no variable in the system is a silent no-op, whereas the typed key means you must have the variable in hand. (`IdBase` defines equality and hashing on `Id`, so a rebuilt-from-state instance still matches.)

This exists because the model must not be scratch space. A solver evaluates the same system at many trial values, and an ODE integrator does so several times per step; with values living only on nodes, each of those has to assign and restore, and a restore missed on an exception path leaves the caller's model holding a solver's intermediate. Passing bindings instead keeps analysis a pure function of `(system, bindings)`.

It is also how an over-determined system is interrogated — pin different subsets, calculate each, and compare. Consistent answers corroborate; inconsistent ones are the finding. Because each `Calculation` carries the `Overrides` that produced it, the comparison is self-describing.

---

## Classification, and what the number does *not* promise

| `Determination` | Meaning | What to do |
| --- | --- | --- |
| `Underdetermined` | DoF > 0 | supply more values; `Unknowns` names what is outstanding |
| `ExactlyDetermined` | DoF = 0 | evaluate or solve |
| `Overdetermined` | DoF < 0 | report redundancy — this is a finding, not an error |

**Over-determined systems are never refused.** Redundant equations either agree, in which case they corroborate a result, or disagree, in which case the model or the measurements are inconsistent and the engineer needs to know. Refusing to look would discard the more interesting of the two outcomes.

**`ExactlyDetermined` is necessary, not sufficient.** The count does not check that the equations are independent. Two equations asserting the same thing, alongside a genuinely free variable, also lands on zero — and no count can tell that apart from a well-posed square system. `UnknownsWithNoEquation` catches the cheapest slice of this (a column no row touches), but the general case needs a matching over the incidence structure. Treat DoF as a gate that can *reject*, never as a promise that solving will succeed.

---

## Composition (Milestone 5), and why the flat form exists

Connecting sub-systems maps their variables onto one another. That is **aliasing**: two mapped variables become one unknown. An identity connection therefore merges two columns and adds no row, while a connection asserting a real relation (flows summing to zero at a junction) adds a row like any other equation.

The consequence that drove this design: **degrees of freedom is not additive over sub-systems.** Two stages at DoF 3 each, joined by four port identities, is neither 6 nor 2 — it depends on whether each merged variable was unknown on both sides. Any API shaped like `composed.DegreesOfFreedom => children.Sum(…)` is wrong, and is exactly what a structural recursion over the system's object graph would tempt you into writing.

Flattening first makes that mistake unavailable. A forty-stage distillation column flattens into one `FlatSystem` and is analysed by the same code as a single stage.

---

## Scope boundaries

**What belongs here:** reducing a system to unknowns and equations, degrees of freedom and classification, calculating a system's values, and — as they arrive — constraint reporting, structural analysis (bipartite matching / Dulmage–Mendelsohn), and the solver abstraction.

Both entry points are extension methods on `ExpressionSystem`, so they read as `system.Flatten()` and `system.Calculate()` without the expression layer knowing this one exists. That direction is deliberate: it is what lets a second strategy — a solver, an interval evaluator — sit beside these rather than inside the domain type.

**What does NOT belong here:**

- The expression graph, operators, and `ExpressionSystem` itself → `DimensionedExpression`
- Arithmetic, dimensional algebra, and uncertainty propagation → `Measurement`
- Wire formats and persistence → `Calcusystem.Serialization`
- Mutating a caller's model. Analysis reads; trial values arrive through `bindings`.

---

## Next

The count-based DoF here is the tractable first cut. Milestone 4 replaces it with a matching over the incidence structure already carried on `Equation.Unknowns`, which yields under- and over-determined *subsets* rather than one global number, detects the structural singularity the count cannot, and produces a block lower-triangular ordering — which is also the order the evaluator should compute in.
