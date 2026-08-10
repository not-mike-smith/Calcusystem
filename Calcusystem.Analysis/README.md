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

## `bindings`: probing without mutating

Every entry point takes an optional `IReadOnlyDictionary<string, Measurand>` keyed by variable id. A variable named there is not an unknown, whatever its own `Value` says.

```csharp
var pinned = SystemFlattener.Flatten(system, new Dictionary<string, Measurand> { ["m"] = trial });
```

This exists because the model must not be scratch space. A solver evaluates the same system at many trial values, and an ODE integrator does so several times per step; with values living only on nodes, each of those has to assign and restore, and a restore missed on an exception path leaves the caller's model holding a solver's intermediate. Passing bindings instead keeps analysis a pure function of `(system, bindings)`.

It is also how an over-determined system is interrogated — pin different subsets, solve each, and compare. Consistent answers corroborate; inconsistent ones are the finding.

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

**What belongs here:** reducing a system to unknowns and equations, degrees of freedom and classification, and — as they arrive — the evaluation walk, constraint reporting, structural analysis (bipartite matching / Dulmage–Mendelsohn), and the solver abstraction.

**What does NOT belong here:**

- The expression graph, operators, and `ExpressionSystem` itself → `DimensionedExpression`
- Arithmetic, dimensional algebra, and uncertainty propagation → `Measurement`
- Wire formats and persistence → `Calcusystem.Serialization`
- Mutating a caller's model. Analysis reads; trial values arrive through `bindings`.

---

## Next

The count-based DoF here is the tractable first cut. Milestone 4 replaces it with a matching over the incidence structure already carried on `Equation.Unknowns`, which yields under- and over-determined *subsets* rather than one global number, detects the structural singularity the count cannot, and produces a block lower-triangular ordering — which is also the order the evaluator should compute in.
