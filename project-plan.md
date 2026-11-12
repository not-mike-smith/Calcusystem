# Calcusystem Project Plan

## Vision

An engineering calculation platform where physical quantities carry their units, dimensions, and measurement uncertainty as first-class concerns. Engineers describe a system of variables, formulas, and tolerances; the platform evaluates what can be computed, propagates uncertainty, checks constraints, and — eventually — solves for unknowns.

---

## Current State (as of November 2026)

Every project targets .NET 10. A model can be built in C# or in the browser, checked for dimensional and structural problems, calculated forward with uncertainty carried through, and have its relationships judged. Nothing yet solves for an unknown.

- **Core** — shared identity, the persistence seams, and `Either<T>` for an operation that may fail without throwing.
- **Measurement** — physical quantities with KMS-normalized units, dimensional algebra, a unified `Measurand` value type, and uncertainty propagation.
- **DimensionedExpression** — immutable expression trees, relationship operators on two confidence ladders, provenance on leaves and relationships, and `ExpressionSystem`, whose membership is what its graph reaches.
- **Serialization** — flat, id-referenced DTOs for a built system and for a draft.
- **Analysis** — `Flatten` (degrees of freedom), `Decompose` (the coarse Dulmage–Mendelsohn split), and `Calculate` (every node once, with relationship outcomes). The solver will live here.
- **Authoring** — `SystemDraft`, a freely invalid model that reports its problems and builds into an `ExpressionSystem`.
- **Api** — a stateless HTTP surface over a posted draft: validation, build, decomposition, calculation, plus the vocabulary and unit catalogue.
- **web/** — a React editor over the API. Its types are generated from the API's OpenAPI document. The draft lives in `localStorage`, and nothing can be saved to or reopened from the server yet.
- **Examples** — three engineering problems built by hand in C#. They show how much ceremony construction takes (see *Expression system creation ergonomics*).

*The longer-term product and ecosystem vision for Calcusystem lives in a companion private repository and is out of scope here.*

---

## Roadmap

The open work is grouped into **feature sets**: connected pieces of work that each get a loose spec in [`specs/`](specs/) before they start. A feature set replaces the old numbered milestones. The work already done under those milestones is kept under [Completed work](#completed-work).

```text
A Building ──┬──▶ C1 Editor: building ◀── B Saving/loading ──▶ G1 Combining systems
             ├──▶ F More expression types (constants → powers)
             ├──▶ H Formula text ◀── C1, D (not before either)
             └ ─ ▶ E Uncertainty (design together: SystemDraft.Build is where pairs get checked)

D1 Analysis entry point ─▶ D Solver ──┬──▶ C2 Editor: solving ◀── E
                                      └──▶ G2 Reconciliation, G3 ODEs ◀── E
```

A dashed arrow means *design together*, not *finish first*. Decide how propagation receives correlations (E1) before the solver puts uncertainty on solved values (D3), or D3 is built on a seam that is about to change.

**Order:**

1. **A** — Building a model in C# code, after the retroactive Authoring spec it leans on. Run **D1** (the `Analysis` entry point) alongside it; they touch different code.
2. **B** — Saving and loading.
3. **C1** — The editor: building. *May move after D while the UI/UX is still settling.*
4. **D** — The solver.
5. **H** — Formula text. Not before C1 and D; this is the earliest slot, and it can move later.
6. **E** — Uncertainty accuracy.
7. **C2** — The editor: solving and uncertainty.
8. **F** — More expression types. Can go anywhere after A.
9. **G** — Combining systems and advanced analysis.

| Feature set | Status | Spec |
| --- | --- | --- |
| A — Building a model in C# code | Not started | [Draft](specs/A-building-a-model.md) |
| B — Saving and loading | Not started | [Draft](specs/B-saving-and-loading.md) |
| C — The editor | C1 not started; an editor exists (see M4.5) | Not written; UX still settling |
| D — The solver | Coarse decomposition done | Not written |
| E — Uncertainty accuracy | Not started; correlation partly designed | Not written |
| F — More expression types | Not started | Not written |
| G — Combining systems | Scope not committed | Not written |
| H — Formula text | Not started; split from A | [Stub](specs/H-formula-text.md) |

---

## Feature sets

### A — Building a model in C# code

Goal: building a model in C# costs about as much as writing the formula down, mistakes come back as one report, and a model prints as text a person can read.

Depends on: the retroactive Authoring spec (see Chores), because every C# model now goes through `SystemDraft`. Design together with E1. Reading text into a model is [H](#h--formula-text).

**Why.** Building a model by hand is more ceremony than the formula it encodes, which the worked examples made concrete enough to act on. `Examples/Problems/Inputs.cs` is a local, deliberately minimal answer for leaves only (`Known`, `Exact`); it is evidence of demand rather than the design. What is actually awkward:
  - **A leaf needs its symbol twice.** `new Variable("h₀", measurand, "h₀")`, because the id defaults to a GUID and a readable id is what lets a report, a test and a relationship name a node the way the problem statement does.
  - **Construction is three shapes for one concept.** `SqrtExpression`/`ReciprocalExpression`/`NegatedExpression` take an id parameter; `ProductExpression`/`SumExpression` take none and need `{ Id = … }`; `QuotientExpression` takes neither and names its operands as required init properties. Nothing about a node's arity should decide how its id is supplied.
  - **A constant has to be a `Variable`.** `π/4`, `2`, `½` and `1 rad` each become a leaf with a provenance slot, and each then appears in `system.Variables` — honest, since a leaf is what they are, but a dimensionless exact literal is not what a reader means by a variable of the model, and `Flatten` has to be trusted to not count them (it does not, because they are valued).
  - **A tree reads inside-out.** `t = (A_tank / C_d·A₀)·√(2h₀/g)` becomes nested initializers in the reverse of the order the formula is read in.
  - ~~**Constraint on any answer:** the sugar must not become the only documented path. The examples use the real constructors deliberately — seeing them is the point — so whatever arrives has to be legible beside them rather than instead of them.~~ *Reversed 2026-11-11: every node goes through the factory (Spec A §5).*
  - **Settle this with its sibling in E1.** If construction moves behind a factory, that factory is also where **Expression correlation as a pairwise property** does its interrogating, so the two want one shape rather than two.

**Design decisions** (detail in [Spec A](specs/A-building-a-model.md#11-decisions)):

- [x] Ids are for the system; builders use names. `Variable.Symbol` becomes `Name`.
- [x] Repeated factors before F: a power helper builds its own correlated product.
- [x] Printed text is human-readable and is not required to parse. Relationships print with their operator's `Symbol`.
- [x] Reading text moves to H, after C1 and D.
- [x] C# builds through a draft: a typed model builder over `SystemDraft`, and `Build()` reports every problem at once.
- [x] Domain constructors internal; the builder is the only way in, and no ids on the C# surface. `SystemDraft.Build` builds through snapshots. (Reversed from "constructors left public" the same day.)
- [x] Names: required for variables and constants, optional for computed nodes; fixed once built; validity rules; unique, with a `DuplicateName` draft problem and an atomic `ExpressionSystem.Add`.
- [x] Constants live in a `Givens` list on `ExpressionSystem`.
- [x] The builder's rule and solving-role arguments are required; `Product` and `Sum` take an optional correlation setting until E1.
- [x] The printer prints built nodes only; definitions print as `name := formula`.
- [x] Leaf interfaces: `ILeafExpression`, with fixed values and variables as siblings.
- [x] Fixed values: a **constant** (named, exact), a **reference value** (named, with uncertainty), and a **scalar** (an unnamed, exact coefficient, not listed in `system.Givens`).
- [x] Names: emoji allowed except joiner sequences; look-alike characters allowed.
- [x] Provenance optional on all fixed values; scalars are dimensionless; one list for constants and reference values.
- [x] `Build()` returns `Either`; at most a small helper that turns a failure into an exception.
- [x] Handles are a new opaque type that knows its builder; constants and reference values live in `system.Givens`.
- [x] One namespace across expressions and relationships.
- [x] Correlation in printed text: brackets with a tag, such as `⟦V·I_total⟧ᶜ`.
- [x] Printer style: Unicode, with values and their uncertainty beside each named value.
- [x] The builder is `ModelBuilder`.

**Names:**

- [ ] `Variable.Symbol` becomes `Name` in the domain, snapshots, drafts, the wire and the editor. Drafts saved before B are not preserved.
- [ ] A name on computed nodes, optional, with snapshot, draft and wire forms.
- [ ] Names unique within a draft and a system; lookup by name in a draft, a system and a calculation.
- [ ] One way to supply an id and a name on every node type, whatever its arity. Ids are generated by default.

**Fixed values:**

- [ ] Constant, reference value and scalar node kinds, under a shared `IFixedExpression`, none of them in `system.Variables`.
  - [ ] Its snapshot, draft and wire forms, and a vocabulary entry.
  - [ ] `Flatten` and `Calculate` treat it as valued.

**Model builder:**

- [ ] `SystemDraft.Build` builds every node through snapshots; domain constructors become internal.
- [ ] A typed model builder over `SystemDraft`, with handles; one shape for every kind, an optional name, and no id.
- [ ] The power helper: `Power(x, n)` builds its own correlated product, with a reciprocal for negative `n`.
- [ ] **Factory-construct the binary operators** — mirror the provenance pattern for `IBinaryOperator`'s concrete types: keep the classes public but make their construction flow through a single factory (public types, internal ctors). Improves discoverability of the operator taxonomy and keeps the deserializer's operator switch aligned with one place. Surfaced during the provenance work. *Whether constructors become internal is now Spec A Q1.*

**Printer:**

- [ ] Expressions, relationships and whole systems print as human-readable formula text. Named nodes print by name; ids never appear.

**Adoption:**

- [ ] Rewrite the Examples with the model builder.
- [ ] Spike: a fluent layer over the builder, tried on one or two rewritten Examples. Keep, change or drop it based on how it reads.
- [ ] Each Example prints its model.
- [ ] Assembly and folder READMEs, and their `ReadmeSnippets`, show the builder.

---

### B — Saving and loading

Goal: a model outlives the browser tab.

Depends on: the retroactive [draft-document spec](specs/retroactive/draft-document.md), whose behavior B keeps. **Nothing written before B is preserved:** B introduces version 1.0.0, and land A's changes to the document first.

**Design decisions:**

- [x] A saved file holds the draft. (De)serializing a built `ExpressionSystem` stays possible, because composing systems (G1) will need it.
- [x] Versions use SemVer, aiming never to increment the major version.
- [x] Migration runs during deserialization, one version step at a time. Promoting across a major version may need a separate utility (to be decided).
- [x] Opening a file goes through an API route that migrates it and returns the current draft; the browser does not migrate.
- [x] `localStorage` holds the same versioned document as a file.
- [x] B owns saving and opening, with a minimal control in the editor; C1 owns the polish.
- [x] Version levels by who does the work: patch needs none, minor is a migration in `Serialization`, major needs the owner or code outside `Serialization` (Spec B §4.2). A reader refuses any newer version.
- [x] An envelope (`Kind`, `Version`, room for metadata) around the draft; the editor writes it, reading stays on the server.
- [x] Once warnings exist, a newer patch opens with a warning.
- [x] The command-line tool is a new `Cli/` project beside `Api/`.
- [x] Migrations are tested with a test-only step and a corpus of saved Example documents.
- [x] Saving after opening a newer patch writes the reader's own version and drops what it ignored; opening warns, and an explicit save asks once.
- [ ] Whether and when a server-side store is needed. This revisits the 2026-10-11 decision that the server stays stateless.

**Work:**

- [ ] A version number on the saved format, for both the system DTOs and the draft DTOs, starting at 1.0.0.
- [ ] Migration: an older version is upgraded on load; a newer one is refused with a clear problem.
- [ ] Save and open as a JSON file download and upload.
- [ ] An API route that takes a saved file, migrates it, and returns the current draft.
- [ ] A command-line tool that opens, validates, migrates and prints a saved file without a browser. A candidate home for the major-version utility.
- [ ] **Tolerant deserialization for model repair** *(from M5; absorbed by B, since a draft already holds dangling references — close when B ships)* — An opt-in mode that loads a payload with dangling id references instead of throwing `ReferencedNodeNotFoundException`, yielding a partial graph plus a structured report of what could not be resolved, so a corrupted or hand-edited model can be repaired rather than rejected outright. **Strictly not the default:** the normal path should keep refusing an internally inconsistent payload, since silently loading a graph with holes in it is how a model quietly starts computing the wrong answer. The design tension is that "invalid state is unrepresentable" is the right rule for the domain types, but JSON intake is exactly the boundary where invalid state arrives — so the flexibility belongs in the loading mode, not in the types. Needs a home for unresolved references (placeholder nodes? a side table?) and a decision on whether a partially-loaded system is allowed to evaluate at all.

---

### C — The editor

Goal: someone who is not a programmer can build, save, solve and read a model in the browser.

Depends on: C1 needs A and B. C2 needs D and E. **C1 may move after D** while the UI/UX is still settling.

**C1 — Building models:**

- [ ] UX direction settled: how variables, formulas and relationships sit on the page and refer to each other. This gates the rest of C1.
- [ ] Show each formula as text (A's printer) as well as by its parts. Typing a formula comes with H.
- [ ] Constants, reference values and scalars.
- [ ] Provenance on variables and relationships. It is on the wire already; the editor does not show or edit it.
- [ ] Save and open files (B).
- [ ] Open an Example as a starter model.
- [ ] **Editing affordances not yet designed** *(from M4.5)* — reordering an operand list, and what should happen to references when a node is deleted (`Remove` currently leaves them dangling and reports them, which is a deliberate first answer rather than a settled one). Better learned from the editor than guessed at.
  - [ ] Reordering operands.
  - [ ] What deleting a node does to the references to it.
- [ ] Show draft warnings beside the problems (see the *Draft warnings* chore).
- [ ] Undo and redo.
- [ ] Front-end tests: a browser test that opens each Example and checks what it computes, and tests for the building blocks.
- [ ] Density: the editor seen at forty rows, on a projector, and in print (see *Not settled* in `web/PATTERNS.md`).

**C2 — Solving and uncertainty:**

- [ ] Which unknowns were solved, which were not, and why (D).
- [ ] Solved values with their uncertainty (D3).
- [ ] Correlations shown and overridable (E1).
- [ ] The uncertainty budget (E2).
- [ ] The confidence level a passing relationship reached (E2).

---

### D — The solver

Goal: given a system with some unknowns, determine if it is solvable and solve it.

**Design principle:** A robust abstraction layer sits between `ExpressionSystem` and any concrete solver, so different solver strategies can be plugged in (e.g. symbolic, numeric, linear algebraic).

Depends on: nothing hard. Decide E1's propagation shape before D3.

**D1 — Prerequisites:**

- [ ] **Reorganize `Calcusystem.Analysis` so the entry point is not extension methods** — `Flatten`, `Decompose` and `Calculate` are all extensions in `Extensions/`, which leaves the namespace holding the results (`Outcomes/`) separate from the one a caller actually invokes, and `Outcomes/` with no reachable constructor. The folder README has flagged the split as under review since it was written. Decide the shape before the solver adds a fourth entry point. The HTTP surface did not have to wait for this: `Api/Workflows/DraftWorkflow` is the only type above `Analysis` that names it, so the change lands in one file.
- [ ] **Structural analysis, fine half** — subdivide `WellDetermined` into the blocks a solver takes one at a time: the strongly connected components of the matched pairs, in reverse topological order, giving the block lower-triangular ordering. A size-1 block is one equation solved for one unknown; a larger one must be solved simultaneously. That ordering is also the order the evaluator should compute in, so the two would share it.
- [ ] **The two seams a solver needs and does not have.** An equation currently produces a three-valued verdict (`IsSatisfiedGiven`), not a signed **residual** a solver can drive to zero — though `Equation`'s own docstring already describes one. And nothing can **invert** an expression toward a single unknown leaf; every node type (`Sum`, `Product`, `Quotient`, `Negated`, `Reciprocal`, `Sqrt`, `Exponential`, `NaturalLog`) is analytically invertible, so a symbolic descent from the root to the leaf is tractable, but it is a new member on `IExpression`. Both reach down into `DimensionedExpression`. Open with them: **what uncertainty does a solved value carry?** Forward propagation is `Measurand`'s job, but a value produced by inverting an equation takes its uncertainty from the *other* side, and no existing path computes that.
  - [ ] A signed residual for each equation.
  - [ ] Inversion for `Sum`, `Product`, `Quotient`, `Negated` and `Reciprocal`.
  - [ ] Inversion for `Sqrt`, `Exponential` and `NaturalLog`, including which branch or domain is chosen.

**D2 — Solving:**

- [ ] Define solver interface: takes an `ExpressionSystem`, returns a solution or a structured "unsolvable" result with explanation
- [ ] Implement a basic solver, for blocks of one equation in one unknown, for product/quotient/sum relationships (the linear and multiplicative cases are tractable without a CAS)
- [ ] Decide what happens to a block of several equations: report that it needs a simultaneous solve, or solve it numerically (for example Newton's method). Without one of these, most coupled systems cannot be solved.
- [ ] If numeric: where initial guesses come from, and how non-convergence is reported.
- [ ] Leave the door open for a symbolic or numeric solver as a future plugin

**D3 — Results:**

- [ ] Uncertainty on solved values, through the inverted expression or from the residual's sensitivity.
- [ ] Unknowns solved together in one block are correlated with each other; record it for E1.
- [ ] Solved values in `Calculation`: relationships judged with them, and what is still missing reported per unknown.
- [ ] Solver results through the API and its data format.
- [ ] An Example that needs solving. Every Example today computes forward.

---

### E — Uncertainty accuracy

Goal: an uncertainty is right without the author arranging the tree to make it so, and a report says where it came from.

Depends on: designed together with A. Correlations between unknowns solved together come from D3.

**E1 — Pairwise correlation:**

- [ ] **Expression correlation as a pairwise property** — make correlation a property of **any two given variables or expressions** rather than one setting on the node combining them. Construction interrogates the operands: the method that builds a product asks, for each *pair*, whether those two are correlated, instead of the author declaring one answer for all of them.

  This fixes the awkward case directly rather than by restructuring. In `I = ½·m·r²` the pair *(r, r)* is correlated and the pairs *(m, r)* are not, so the flat, algebraically natural product gives the right bar — 0.97 %, where one node-wide setting can only reach 0.77 % or 1.33 %. Today the only way to say it is to gather `r²` into a node of its own, which means **the shape of the tree carries a fact about the inputs**.

  Defaults come from a **correlation graph**: two expressions that reach a common variable are correlated by default. That is what makes `P = V·I_total` right without the author noticing anything — `I_total` reaches `V_bus`, so the pair is correlated unless something says otherwise. Today that form silently under-states the uncertainty and only `P = V²·G` states it, which is the failure worth removing: the natural form gives a wrong bar with nothing to warn anyone. **The author can see the default and override it**, which is the part that keeps it honest — a derived default that cannot be inspected is worse than a declared one.

  Undecided:
  - **The propagator seam changes shape.** `IUncertaintyPropagator.PropagateThroughProduct(correlation, params Measurand[])` takes one correlation and bare *values*, so it cannot tell which operand is which, let alone that two of them trace to one leaf. Pairwise means it needs the pairings alongside the magnitudes — the node precomputing them is the option that keeps `Measurand` a value.
  - **Binary per pair, or a coefficient?** Once correlation is pairwise, `Uncorrelated`/`Correlated` is ρ = 0 and ρ = 1, and RSS and direct sum are the two endpoints of one formula. Partial correlation is the common real case, so a two-valued reading will be asked to represent it. Decide before the graph stores anything.
  - **Where the graph lives.** Correlation between two leaves is a fact about the measurements, so it is model data and belongs with the system. But a node needs it when it computes, and `ComputeFrom` deliberately takes only values — so a graph on the system lets `Calculate` supply it while `ComputeIfFullyDescribed()` on a bare node could not, and the two answering differently is the shape this codebase keeps removing.
  - **How conservative the derived default should be.** Two expressions sharing a variable are correlated *to the degree each depends on it*, not fully. Full correlation is the safe reading and can be badly pessimistic — two long sums sharing one small term. This is the same question as the coefficient one.
  - **Whether an identity pair is offered at all.** *(r, r)* is one measurement; an author overriding that to uncorrelated asserts something false.
  - **Provenance already names the instrument.** `ProvenanceFactory.Measured(instrumentId, calibrationDate)` is where "both of these come off VT-01" is already written down, which is exactly what makes two leaves correlated. Leaf defaults could be derived from it rather than declared a second time.

  Lands on the two construction items in A: if building a node goes through a factory for ergonomic reasons, that factory is where the interrogation happens, and **Factory-construct the binary operators** above is the same move one layer up.

- [ ] Decide the undecided points above.
- [ ] A correlation graph with derived defaults.
- [ ] Author overrides, stored with the system and on the wire.
- [ ] Propagation uses the pairwise correlations.
- [ ] Re-check the Examples' bars, including `I = ½·m·r²` and `P = V·I_total`.

**E2 — Diagnostics and reporting:**

- [ ] **Uncertainty crossing zero for magnitudes — semantic (deferred to the evaluation layer).** When an error interval spans zero, for a **magnitude** (a physically non-negative quantity) a lower bound below zero is not physical. Decision made: **stay signed, don't clamp** in the uncertainty layer; instead surface a *diagnostic/flag* at report/constraint time when a quantity expected to be non-negative has an interval crossing zero. This needs the "expected non-negative" signal, which lives at the modeling layer — implement alongside the evaluation engine's result model.
  - [ ] A way to declare a variable non-negative. Nothing marks one since `Magnitude` and `Delta` merged into `Measurand`.
  - [ ] A flag in the calculation when such a variable's range crosses zero.
- [ ] **Confidence ladder, reporting half** — surface the achieved tier on `RelationshipOutcome`, so authoring "do these overlap at all" returns `true` *and* reports the achieved rung was "wholly contained". Design unmade: `IsSatisfiedGiven` returns plain `bool`, so something must carry the tier upward, and containment has no single ordered tier to carry.
- [ ] Monte Carlo propagation as an alternative propagator, for where the linear (RSS) estimate is poor: large relative uncertainties, or strongly nonlinear functions near a domain edge. The Measurement README has called it deferred with no plan item behind it.
- [ ] An uncertainty budget: each input's share of an output's uncertainty. It uses the same partial derivatives correlation needs, and answers which measurement to improve.
- [ ] *(punted 2026-08-11)* Add conversion factor provenance to `UnitOfMeasure` — a structured `ConversionSource` record carrying the standard name (e.g. "NIST SP 811"), URL, and year for non-trivial factors like lb→kg or BTU→J. Include provenance in the serialization DTOs so exported calculations carry a full audit trail of where their conversion factors came from.

---

### F — More expression types

Goal: the formulas engineers commonly write can be expressed.

Depends on: A. Powers need A's constants for their exponents, and each new kind goes through A's model builder and printer.

A new kind is done when it has uncertainty propagation, snapshot, draft and wire forms, a printed form, an inversion once D lands, and a spelling in the formula language once H lands.

- [ ] **Integer nth root** — `NthRootExpression(argument: IExpression, n: int)`. Natural generalization of `SqrtExpression` (M3) to arbitrary integer roots. Constraint: all dimensional exponents of the argument must be divisible by `n`, so the result always has integer-exponent dimensions (each exponent divided by `n`); e.g. `∛(m³·s⁻³)` → `m·s⁻¹` is valid, `∛(m²)` is not. Argument value must be non-negative (or handle odd `n` separately for signed values). Uncertainty: `RelativeUncertainty(x^(1/n)) = (1/n) · RelativeUncertainty(x)`.
- [ ] **Binary exponentiation** — `PowerExpression(base: IExpression, exponent: IExpression)`. Exponent must be dimensionless (and ideally a rational constant for dimensional analysis to remain tractable). Dimensionality of result = base dimensionality raised to the exponent; unlike `NthRootExpression`, integer-exponent results are not guaranteed and are the caller's responsibility. Uncertainty: standard power-rule propagation. Complements `ExponentialExpression` (`e^x`) from M3 for general `x^n` expressions.
- [ ] **Trigonometric functions** — `sin`, `cos`, `tan` on a dimensionless angle (radians), and `atan2` returning one. Complex numbers' `.Phase()` below already needs `atan2`.
- [ ] **`abs`, `min`, `max`** — G3's discontinuity check looks for these, and no model can contain them yet.
- [ ] **Complex number support** — A `ComplexExpression` type holding `Re : IExpression` and `Im : IExpression` children, supporting complex arithmetic (add, multiply, divide, conjugate). Exposes `.Magnitude()` → `sqrt(Re² + Im²)` and `.Phase()` → `atan2(Im, Re)` as regular `IExpression` nodes. Never promotes directly to `PhysicalQuantity`; callers must extract a real component. Primary motivation: AC circuit analysis with phasor impedance.

---

### G — Combining systems and advanced analysis *(scope not committed)*

Worth designing for, and deliberately after D is solid.

Depends on: G1 needs B. G2 needs D and E. G3 needs D's numeric solving.

**G1 — Combining systems:**

- [ ] **`SystemDraft.FromSystem`** *(moved from B)* — open a built system for editing, such as one loaded only in built form for composition. If something needs it sooner, it will show up as a requirement.
  - [ ] Round-trip test: every Example through `FromSystem` and `Build` gives an equivalent system.

- [ ] **`ExpressionSystem` composition** — Named *ports* on each `ExpressionSystem` (the subset of its variables exposed as inputs/outputs), plus a `ComposedExpressionSystem` that connects sub-systems by mapping ports. Any sub-system exposing matching port names can be substituted — enabling e.g. swapping ideal-gas EOS for Peng-Robinson within a larger reactor model without touching the surrounding system. The M4 solver abstraction should be designed port-aware so it can traverse a composed system; composition itself is deferred until post-M4. Granularity rule: one `ExpressionSystem` per coherent model; a full process flowsheet is a `ComposedExpressionSystem`.

**G2 — Reconciliation:**

- [ ] **Data reconciliation** — A `ReconciledVariable` type that aggregates multiple independent `MeasuredVariable` nodes referring to the same physical quantity and finds the weighted least-squares estimate consistent with all `Definitions` (conservation laws). When redundant measurements disagree beyond their stated uncertainties, reconciliation surfaces the inconsistency rather than silently propagating a single measurement's error. Requires the algebraic solver (M4) to be in place as the constraint backbone.

**G3 — Dynamics:**

- [ ] **Dynamic (ODE) relationships** — A `DerivativeRelationship` type linking two variables through a time derivative (`rate = d(quantity)/dt`), enabling lumped-parameter transient models: filling tanks, thermal mass, RC circuits, spring-mass systems. Scope is deliberately restricted to *time as the sole independent variable* (excludes all PDEs, including Navier-Stokes), *initial value problems only* (conditions at `t = 0`; excludes BVPs requiring shooting methods or collocation), and *explicit first-order form* `y' = f(t, y)` (higher-order systems reduce to first-order via state-space substitution; implicit DAEs deferred). Integral relationships (`quantity = ∫rate dt`) are the inverse case handled by the same mechanism. Solving requires a numerical ODE integrator (RK4, Dormand-Prince, or similar) plugged in via the M4 solver abstraction layer.
- [ ] **ODE system diagnostics** — A `SystemDiagnostics` report that runs before the ODE solver and surfaces structured findings so engineers can act on specific information rather than diagnosing solver failures at runtime:
  - *Stiffness*: compute the Jacobian `J = ∂f/∂y` (symbolic from the expression tree or numeric via finite differences); stiffness ratio = `max(|Re(λᵢ)|) / min(|Re(λᵢ)|)` over eigenvalues of `J`; ratio >> 1000 → recommend an implicit solver (BDF, Radau) instead of explicit RK4.
  - *Discontinuities*: walk the expression tree for `abs()`, `min/max`, conditional or lookup-table terms; flag as potential discontinuity sources and recommend event-detection restart logic.
  - *DAE structure*: detect algebraic constraints mixed with differential relationships; compute the DAE index (number of symbolic differentiations needed to recover a pure ODE); index > 1 requires index reduction (Pantelides algorithm) before any standard solver can proceed.
  - *Initial condition consistency*: verify that the `t = 0` values satisfy all algebraic constraints before integration begins.

---

### H — Formula text

Goal: a model can be written as text and read back, in C# and in the editor.

Depends on: A. **Not before C1 and D**, so the editor's UX and the solver's needs shape the language. Split from A on 2026-11-11, because reading text raised too many questions about internal structure for a model builder to answer. The decisions already made, and the open questions, are in [Spec H](specs/H-formula-text.md).

**Design decisions:**

- [ ] **Statement**, a missing domain concept: the unit that holds text and owns the nodes parsed from it. First pass: a statement has a single binary operator. Open: how definitions (`:=`) fit, and whether it exists in a built system.
- [ ] Source of truth: text as input only, or the statement owning its nodes.
- [ ] The solving role of a relationship written in text, possibly encoded in the operator notation of `OPERATORS.md`.
- [ ] Names in text: which names text can refer to, given that names like `π/4` and `D²` are not identifiers and quoting is ruled out.
- [ ] Two writers: A's human-readable printer, and a strict machine-readable form the parser reads back exactly.
- [ ] Name resolution independent of statement order.

**Work:**

- [ ] A table from operator symbol to operator kind, checked against each operator's `Symbol` by a test.
- [ ] Problems that carry a position in the text.
- [ ] The parser into `SystemDraft`, through a syntax-tree layer.
- [ ] The machine-readable writer, and a round-trip test.
- [ ] Formula entry in the editor, with syntax problems shown where they are.

---

## Chores

- [ ] **Retroactive specs** — write down the behavior that already exists, so a bug means *a deviation from specified behavior* and stays distinct from *behavior nobody specified*. A retroactive spec records what is true and intended today. It is not a redesign, and anything found to be wrong becomes a Known Bug against the spec.
  - **Escalation rule:** when drafting a feature-set spec means assuming existing behavior, or explaining too much context, the retroactive spec for that area is missing. Write it before that feature set starts. Each feature-set spec lists what it leans on in a *Leans on unspecified behavior* section.
  - [ ] Decide the layout: one spec per assembly or per behavior area, and where they live under `specs/`. *Proposed by the first one: `specs/retroactive/<area>.md`, with numbered rules (`AU-n`) each naming its test.*
  - [ ] Decide how a retroactive spec relates to the READMEs. A README's *Guarantees* section already states specified behavior; a spec could cite it rather than repeat it. *Proposed: the spec says what, the README says why, and the spec cites it.*
  - [ ] **Authoring — needed before A starts.** [Draft](specs/retroactive/authoring.md). All findings decided; four became Known Bugs. What `SystemDraft.Validate` and `Build` report and when, that ids carry through, how operand slots and `Remove` behave. Every C# model now goes through `SystemDraft`, so Spec A leans on it heavily (Spec A §8). The first application of the escalation rule.
  - [ ] Analysis: what `Flatten` counts as an unknown and an equation, and what `Calculate` reports. Spec A and D lean on it.
  - [ ] DimensionedExpression and Authoring: what happens to a reused id. `ExpressionSystem.Add` silently skips a different node with an existing id, and `SystemDraft.AddExpression` silently replaces one. Found in the Spec A review.
  - [ ] **The draft document — needed before B starts.** [Draft](specs/retroactive/draft-document.md). B must keep its behavior; its structure is not frozen. All findings decided; seven became Known Bugs.
  - [ ] Further areas as feature-set specs reveal them.
- [ ] **Restructure the repository and rename the project** — move `Calcusystem.sln` and the .NET projects out of the repository root, and give the project a shorter name (candidates: *Modicum*, *ByThatMuch*). Done together, so every path and namespace changes once.
  - **Pressure is building** (2026-11-11), though it is not a prerequisite for any feature set. Waiting on it: the saved file's extension and media type (B), and the command-line tool's command name (B).
  - [ ] Choose the name. **Presumptive winner: *Folga*** (2026-11-11), Portuguese for mechanical clearance or play, as in *ajuste com folga*, a clearance fit. To be confirmed with native speakers. **Miss Congeniality: *ByThatMuch*.** Also shortlisted: *Cerca* (*cerca de*, "about", and *cerca*, "fence") and *Tolerable*.
    - Availability, checked 2026-11-11:

      | | Folga | Cerca | Tolerable | ByThatMuch |
      | --- | --- | --- | --- | --- |
      | npm, NuGet (id and `Name.*`), crates.io | free | free | free | free |
      | PyPI | free | taken (dormant since 2017) | free | free |
      | GitHub user or org | taken (0 repos) | taken (dormant) | taken (active) | free |
      | Domains | `.io` looks free | all registered | all registered | `.dev`, `.io`, `.org` look free |

    - Not checkable from outside: whether the npm scope is held (try `npm org create`), and NuGet prefix reservation (apply on the account).
    - [ ] Create the npm organization, apply for the NuGet `Folga.*` prefix, and register a domain if wanted.
  - [ ] Choose the saved file's extension and media type, which carry the new name.
  - [ ] Choose the layout. For example: `dotnet/` beside `web/`, or `src/` and `tests/` with the solution in one of them. Consider moving to the `.slnx` solution format at the same time.
  - [ ] Rename the root namespace and assembly prefix. `Directory.Build.props` derives both, but every `namespace` declaration and `using` is spelled out in source.
  - [ ] Update the web package name, generated-type script paths, and the `Api/Api.json` path.
  - [ ] Update READMEs, `CLAUDE.md`, `DOCUMENTATION.md`, this plan and the specs.
  - [ ] Rename the GitHub repository.
  - [ ] Timing: early, while few specs and branches exist to update. No serialized data names a type or namespace today, so saved files do not force the timing; keep it that way when B versions the format.
- [ ] **Draft warnings** — let `Build` (and `Validate`) report **warnings**: findings that do not stop a build, returned beside the built system or beside the problems.
  - [ ] Decide the shape: how a successful `Either<ExpressionSystem>` carries warnings, whether warnings get their own kind enum, and how the API and the editor show them.
  - [ ] Candidate warnings:
    - [ ] A reference value with no provenance (Spec A).
    - [ ] A document of a newer patch version, opened anyway (Spec B).
    - [ ] An unknown property in a draft document. It is a refusal until warnings exist (draft-document DF5).
    - [ ] A non-equality relationship stating a solving role other than `Requirement`. This is a problem today (F3, `RoleNotAllowed`); decide whether it moves to a warning once warnings exist.
- [x] **Continuous integration** — `.github/workflows/ci.yml`, on every pull request and every push to `master`: build, run every test suite, build the editor, check that its generated API types are committed, and run the pattern-language check.
  - [x] Require the CI check to pass before merging. Done 2026-11-11 in the `master-protection` ruleset, which also stopped requiring approval of the most recent push, since a solo maintainer cannot give it.
  - [ ] Consider pinning the .NET SDK with `global.json`, so CI and local builds use the same one.
  - [ ] Consider Dependabot for the workflow's actions, NuGet and npm.
- [ ] **Test-project READMEs** — Add a `README.md` to `Measurement.Test/`, `DimensionedExpression.Test/`, and `Serialization.Test/`. These follow a different structure than the library READMEs (purpose, coverage, test helpers/patterns, how to run) — to be established when tackled.

---

## Known Bugs

- [x] **Uncertainty at and near zero — implementation.** Fixed: uncertainty now stores its error as *either* a relative fraction or an absolute KMS value, distinguished by a `bool IsStoredAsAbs` (a storage convention on what the `Magnitude` means; a genuinely different model like interval bounds would instead be a change at the `IUncertaintyPropagator` level). Absolute storage needs no divide-by-value, so a zero-valued result carries a meaningful error; `RelativeUncertainty(0)` returns `+∞` instead of throwing. Sums/differences propagate to an absolute-error result (no dividing by the summed value); products still compose relative errors. `ln(1)` and canceling sums no longer throw (regression tests in `Measurement.Test` and the updated `NaturalLogExpression` test). Serialization carries the storage flag.
- [ ] **Uncertainty crossing zero for magnitudes** — tracked in [E2](#e--uncertainty-accuracy).
- [x] **A reused id replaces a draft node silently** — breaks [AU-3 and AU-23](specs/retroactive/authoring.md#7-findings) (F1). Keep both nodes and report `DuplicateId`; add the missing `DuplicateId` test (F9).
- [x] **An equality with no solving role builds as a requirement** — breaks [AU-21 and AU-29](specs/retroactive/authoring.md#7-findings) (F2). Report it as `RelationshipRuleMissing`.
- [x] **A solving role on a non-equality is ignored silently** — breaks [AU-21 and AU-40](specs/retroactive/authoring.md#7-findings) (F3). Report it as a new problem kind, `RoleNotAllowed`.
- [ ] **The draft document's JSON conventions live in `Api`, and a camelCase file reads as an empty draft** — breaks [DD-1](specs/retroactive/draft-document.md#9-findings) (DF6). Move the conventions into `Serialization`; match property names without regard to case.
- [ ] **Reading a draft document throws, and stops at the first refusal** — breaks [DD-30 to DD-38](specs/retroactive/draft-document.md#9-findings) (DF2). Return an `Either` carrying every refusal with its kind and location.
- [ ] **A draft document refuses two nodes with one id in one list** — breaks [DD-31](specs/retroactive/draft-document.md#9-findings) (DF1). Hold both and let the draft report `DuplicateId`. Do with Authoring F1.
- [ ] **A draft document accepts what it should refuse** — breaks [DD-35 to DD-38](specs/retroactive/draft-document.md#9-findings): unknown properties (DF5), an uncertainty with no value (DF4), a partial comparison rule (DF7), a provenance missing a required field (DF3).
- [x] **A node above a cycle is reported as circular** — breaks [AU-31 and AU-35](specs/retroactive/authoring.md#7-findings) (F4). Report only the nodes in the cycle.

---

## Completed work

The numbered milestones, as they were when the plan moved to feature sets. Their open items moved to the feature sets above.

### Housekeeping

- [x] **Per-assembly READMEs** (PR #16) — Added `README.md` to the three library assemblies (`Measurement/`, `DimensionedExpression/`, `Serialization/`), each covering responsibility, key interfaces/types, invariants (e.g. all values are KMS-normalized), dependencies, and explicit scope boundaries. Goal met: README + interface files are enough to *use* a project without its implementation in context. *Test-project READMEs are the remaining piece — see [Chores](#chores).*
- [x] **Interface docstring comments** (PR #16) — All public interfaces across the three assemblies now carry XML `<summary>` / `<remarks>` / `<param>` comments on the interface and every member, articulating the contract: `IUncertainty`, `ISymmetricUncertainty`, `IUncertaintyPropagator` (Measurement); `IExpression`, `IDirectExpression`, `IComputedExpression`, `IBinaryOperator`, `IEqualityEstimating` (DimensionedExpression); `ISerializedObject` (Serialization). Measurement additionally documents the `Quantity`/`Dimensionality` structs, `FundamentalDimension`, and `UnitOfMeasure`/`OffsetUnitOfMeasure` (flagged in the Measurement README). This completes the LLM-context strategy: README for orientation, interface docstrings for contract, implementation only when modifying.
- [x] **Project README** (PR #16) — Top-level `README.md` filled out with overview/motivation, the reading model (README + `Interfaces/` to *use*; implementation only to *modify*), the assembly structure, a verified quick-start, and build/test/contributing notes.
- [x] **Worked examples** — `Calcusystem.Examples`, three engineering problems built as `ExpressionSystem`s and printed with what they computed and what their checks did: a tank draining through an orifice (chemical), parallel resistive loads on a DC bus (electrical), and a gimballed flywheel's precession (mechanical). Runnable (`dotnet run --project Examples`), depended on by nothing, and with a matching `Examples.Test` holding every quoted number to a hand-worked value rather than to a recorded run. Every example computes **forward**, because nothing yet inverts an equation toward an unknown — which is why the tank is modeled as the closed-form integral of its ODE and the circuit is resistive DC rather than LRC. One example's requirement is violated on purpose: a report where everything passes cannot show the difference between a check that held, one that failed, and one that never ran. Found and fixed a defect: `Power.Watt` was declared `(Joule, 1), (Second, 1)` rather than `-1`, giving every unit in `Power` the dimensionality of energy × time and carrying it into the four lists built on a watt — so `Energy.KilowattHour` would not convert to `Energy.Joule`, from the same list. A wrong exponent is invisible from inside its own family, because every unit scaled from it is wrong the same way; `ListCompletenessTests.EveryUnitOfOneKindMeasuresOneDimensionality` is the check that sees it.

*Incidental fixes made during the documentation pass (PR #16), each with a regression test:* `Measurand.TryAdd`/`TrySubtract` no longer throw on dimension mismatch (return a NaN-valued `Measurand`); `DeserializingMapper.GetExpression` was resolving the parent DTO's own id instead of the requested child id, breaking deserialization of every derived expression/operator (first `Calcusystem.Serialization.Test` project added). Also renamed `NegatedVariable` → `NegatedExpression` (inner property → `Operand`).

---

### Milestone 1 — Clean Foundation ✅ *complete*

Goal: get the codebase into a clean, consistent state before building new features.

- [x] Upgrade all projects to .NET 10
- [x] Fix `IsSubnormal()` bug in `BaseQuantity` (calls `IsNormal()` instead)
- [x] Rename `BaseQuantity` to `PhysicalQuantity`
- [x] Fix `Magnitude.TryAdd(Magnitude)` missing error propagation
- [x] Fix `Gradian`, `ArcMinute`, `ArcSecond` bugs in `Angle.cs` (inverted scale factors); add missing `readonly`
- [x] Fix `Rankine` and `Fahrenheit` conversion factors in `Temperature.cs` (were `1.8`/`1` instead of `5/9`)
- [x] Add unit types: `Pressure` (with nominal gauge `OffsetUnitOfMeasure` variants), `ElectricPotential`, `ElectricResistance`, `MomentOfInertia`, `AngularMomentum`

---

### Milestone 1.5 — Unit Library Completion ✅ *complete*

Goal: flesh out the unit library to cover the most common engineering domains before work begins on the expression layer.

**Mechanical:**

- [x] `Torque` — N·m, lbf·ft, lbf·in (same dimensions as Energy; separate class for semantic clarity)
- [x] `Momentum` — kg·m/s, lbf·s (M·L·t⁻¹; impulse has the same dimensions, can live here)
- [x] `SurfaceTension` — N/m (M·t⁻²)
- [x] `SpecificEnergy` — J/kg, BTU/lb, kWh/kg (L²·t⁻²; relevant for fuels, batteries, explosives)

**Fluid / Thermal:**

- [x] `DynamicViscosity` — Pa·s, cP (centipoise), poise (M·L⁻¹·t⁻¹)
- [x] `KinematicViscosity` — m²/s, cSt (centistoke), St (L²·t⁻¹)
- [x] `ThermalConductivity` — W/(m·K) (M·L·t⁻³·T⁻¹)
- [x] `SpecificHeatCapacity` — J/(kg·K) (L²·t⁻²·T⁻¹)
- [x] `HeatTransferCoefficient` — W/(m²·K) (M·t⁻³·T⁻¹)

**Electrical:**

- [x] `ElectricCapacitance` — F, µF, nF, pF (A²·s⁴·M⁻¹·L⁻²)
- [x] `ElectricInductance` — H, mH, µH, nH (M·L²·A⁻²·t⁻²)
- [x] `ElectricConductance` — S (Siemens = 1/Ω) (A²·t³·M⁻¹·L⁻²)

**Electromagnetic:**

- [x] `MagneticFluxDensity` — T (Tesla), G (Gauss) (M·t⁻²·I⁻¹)
- [x] `MagneticFlux` — Wb (Weber) (M·L²·t⁻²·I⁻¹)

---

### Milestone 2 — Complete the Expression Layer ✅ *complete*

Goal: close the gaps in `DimensionedExpression` so the expression system is fully usable.

- [x] Implement `DegreesOfFreedom()` as a recursive graph walk — sums children's DoFs in `ProductExpression`, `SumExpression`, and `QuotientExpression`; already correct in `DirectExpressionBase`, `NegatedVariable`, and `ReciprocalExpression`
- [x] Implement `EqualityOperator.IsSatisfied()` — wired `IEqualityEstimating` via primary constructor (DI); `Deserializer` updated to accept and forward the estimator
- [x] Add `ExpressionSystem.Create(name, description)` static factory method with auto-generated ID
- [x] Document and rename tolerance operators — `WithinToleranceAndNotOver` → `PointAndUpperBoundWithinToleranceOperator`, `WithinToleranceAndNotUnder` → `PointAndLowerBoundWithinToleranceOperator`; XML doc comments on all five operators
- [x] Extract uncertainty to `IUncertainty` interface (`Measurement/Uncertainty/`). Concrete implementations: `GaussianUncertainty` (existing behavior), `AsymmetricUncertainty(upper, lower)` (asymmetric relative errors), `BoundedUncertainty(upper, lower)` (asymmetric absolute KMS errors). Added `ISymmetricUncertainty : IUncertainty` sub-interface with default upper/lower implementations; `GaussianUncertainty` implements it. `IUncertainty` exposes `UpperAbsoluteUncertainty`/`LowerAbsoluteUncertainty` separately; all five tolerance operators updated to use directional bounds. Monte Carlo propagation deferred to Milestone 4.

---

### Milestone 2.5 — Inequality Operators ✅ *complete*

Goal: extend the `BinaryOperators` namespace with uncertainty-aware ordering operators and document the full operator taxonomy.

No `≤` / `≥` variants — floating point equality is essentially unreachable in practice; callers who need "at most" can negate the other side.

**`<` operators (non-commutative; Lhs = value under test, Rhs = bound):**

- [x] `DefinitelyLessThanOperator` — `Lhs.Upper < Rhs.Lower`: the entire Lhs interval is strictly below the entire Rhs interval; no overlap possible
- [x] `UpperBoundsLessThanOperator` — `Lhs.Upper < Rhs.Upper`: the ceiling of Lhs is below the ceiling of Rhs; intervals may overlap
- [x] `NominallyLessThanOperator` — `Lhs.KmsValue < Rhs.KmsValue`: point comparison only; uncertainty ignored

**`>` operators** (symmetric to `<`; lower bounds drive the checks):

- [x] `DefinitelyGreaterThanOperator` — `Lhs.Lower > Rhs.Upper`
- [x] `LowerBoundsGreaterThanOperator` — `Lhs.Lower > Rhs.Lower`
- [x] `NominallyGreaterThanOperator` — `Lhs.KmsValue > Rhs.KmsValue`

**Documentation and tests:**

- [x] Add `DimensionedExpression/BinaryOperators/OPERATORS.md` — a taxonomy table covering all operators (equality, the six tolerance operators from M2, and the six unequality operators above), with a one-line geometric description and the exact interval condition for each
- [x] Unit tests in `DimensionedExpression.Test` covering all six operators (symmetric and asymmetric uncertainty, boundary conditions, null returns for unset expressions)

---

### Milestone 3 — Evaluation Engine ✅ *complete (open items moved to E2)*

Goal: given a populated `ExpressionSystem`, compute everything that can be computed and report constraint satisfaction.

**Expression type additions (prerequisite for evaluation and provenance reporting):**

- [x] Rename `ICalculatedExpression` / `CalculatedExpressionBase` → `IComputedExpression` / `ComputedExpressionBase` throughout — "calculated" is ambiguous; "computed" avoids collision with "derivative" once ODE relationships are added in M5. *(Done; README caveat dropped.)*
- [x] Capture *provenance semantics* for leaf variables — orthogonal to *physical semantics* (point vs. signed delta/difference, a modeling concern the `Magnitude`/`Delta` removal left to context). **Design changed from the original "Variable subtypes" plan:** the four provenance categories have no behavioral differences, only kind-specific audit metadata, so they are modeled by **composition, not inheritance** — `Variable.Provenance` (and `IBinaryOperator.Provenance`, so Definitions/Constraints can carry citations too) is an optional `IProvenance` (null = untracked), created through the single `ProvenanceFactory`. `IProvenance` exposes `Id` (it round-trips like any node) and `Summary()` (for UI). The concrete kinds are public (so the serializer can map them) with internal ctors (factory-only construction). Serialization lives in `Calcusystem.Serialization` like everything else — a flat `Dtos.Provenance` (Type discriminator + union of fields) nested in the `SingleVariable`/`BinaryOperator` DTOs, mapped via `Map`/`MapProvenance` and reconstructed through the factory. *(Done, including serialization round-trip.)* The four kinds:
  - `Measured` — an instrument or sensor reading; uncertainty characterises instrument calibration and repeatability; metadata: instrument id, calibration date
  - `Reference` — a literature or tabulated value (thermodynamic property, material property, physical constant); uncertainty from the source's stated precision or treated as exact; metadata: citation, URL, year (same idea as `ConversionSource` for unit factors)
  - `Design` — an engineer-specified value, not measured or from literature; exact or carries a manufacturing/specification tolerance via `AsymmetricUncertainty.FromAbsolute`; metadata: spec/drawing reference
  - `Model` — an empirically fitted constant within a constitutive relationship (e.g. discharge coefficient `Cᵈ`, Nusselt correlation coefficients); uncertainty from the fitting process; distinct from `Reference` because it is model-specific, not a physical property; metadata: model name, fitting reference
- [x] Resolve the `Definitions` / `Constraints` / instances semantic model: `Definitions` are always-true relationships used to *compute* unknowns (conservation laws, constitutive equations); `Constraints` are tolerance checks run against computed or measured values (pass/fail); the provenance annotation on `Variable` above replaces the informal notion of "instances". **Resolved by moving the distinction onto the operator**, and refined twice since. `IBinaryOperator.SolvingRole` (PR #38) says what a relationship does to the problem — `Equation` defines a quantity, `Coherence` asserts that separately computed routes to one quantity agree, `Requirement` bounds a value without producing one — with `IsDetermining` surviving as a *derived* `Equation or Coherence`, which is the question degrees-of-freedom code asks. `ExpressionSystem` holds a single `Relationships` list with `Equations`/`CoherenceChecks`/`Requirements` as read-only views, so membership can no longer disagree with meaning. Only `EqualityOperator` can be anything but `Requirement`, and it is the only operator taking the role as a constructor argument, so an invalid combination is unconstructible rather than validated. `Coherence` earns a member because the distinction from `Equation` is not recoverable from the predicate — both assert equality, and only the modeler knows which is meant — which is also why the wire stores the role rather than the boolean.

**Foundation work, inserted 2026-08-24 while designing the verdicts slice and finding its groundwork unsound:**

- [x] **Relationship roots** (PR #34) — `Calculate` ignored nodes reachable only through a relationship's operands while `Flatten` counted them, so the two analyses disagreed about what one model contained. Superseded by #36.
- [x] **Immutable expression structure** (PR #35) — an expression's operands are fixed at construction; only `Variable.Value` stays mutable. Hashing the graph to detect staleness was considered and rejected: computing the hash costs the same walk it was meant to avoid. Removing the mutation is cheaper than detecting it, and it also makes cycles unconstructible and collapses each validation rule to one site.
- [x] **Membership is reachability** (PR #36) — `ExpressionSystem.Add` absorbs the whole subgraph beneath whatever it is given, so the collections cannot diverge from the graph. Safe only because of #35. Fixed a third instance of the same divergence: a node nested inside another was referenced by id on the wire but never written. `DirectExpressions` → `Variables`.
- [x] **Vacuous equations** (PR #37) — `DegreesOfFreedom` counts only equations an unknown is incident on. `Determination` stays a pure function of the count rather than also weighing redundancy, because vacuity is *orthogonal* to determination: the same redundant check appended to an under-, exactly-, or over-determined system leaves each unchanged.
- [x] **`SolvingRole`** (PR #38) — see the semantic-model item above.

**Evaluation engine:**

- [x] **Degrees of freedom for a system** — new `Calcusystem.Analysis` assembly. `SystemFlattener.Flatten(system, bindings?)` reduces a system to a `FlatSystem` of unknowns × equations; `DegreesOfFreedom` = unknowns − determining equations, classified as `Underdetermined`/`ExactlyDetermined`/`Overdetermined`. Over-determined is reported as redundancy, never refused. Prerequisites landed with it: `IExpression.Children` (the single accessor every graph walk goes through) and `IExpression.UnsetVariables()`, which replaced the per-type `DegreesOfFreedom()` that summed over children and so double-counted shared sub-expressions. The optional `bindings` argument keeps analysis a pure function of `(system, bindings)` so a solver can probe trial values without leaving scratch in the caller's model. Deliberately count-based: it assumes independence, which M4's structural analysis replaces.
- [x] **Graph walk** (PR #31) — `system.Calculate(overrides?, propagator?)` in `Calcusystem.Analysis` returns a `Calculation`: every node computed exactly once in dependency order, with what could not be resolved and the unset variables responsible. Pure in `(system, overrides)`, so a caller probes trial values without writing them into the model and independent calculations parallelize for free. Supporting seams landed with it: `IExpression.ComputeFrom` (a node's arithmetic without the walk), `ComputeIfFullyDescribed` replacing the `Value` property, `ExpressionBase` owning every derived walk, and cycle detection.
- [x] **Relationship verdicts** (PR #40) — `IsSatisfiedGiven(lhs, rhs)` on all thirteen operators, so a verdict is a function of supplied values rather than of a fresh read of the model. Required because `IsSatisfied()` called `ComputeIfFullyDescribed()` on both sides: it could not see a calculation's overrides (so under a trial value it would silently judge the *model's* values) and it re-walked both subgraphs, duplicating work `Calculate` had just done. `IsSatisfied` survives as the isolated-question form, now taking the same optional `overrides`/`propagator` as `ComputeIfFullyDescribed`, and is implemented **once** on `BinaryOperatorBase` — the guard that answers `null` for an unresolved side was thirteen identical copies, i.e. thirteen chances to differ.
- [x] **Relationship outcomes on `Calculation`** (PR #40) — a `RelationshipOutcome` per relationship (the operator, a three-valued verdict, and both sides' values), with `Violations` / `Inconsistencies` / `Undetermined` views. Every relationship appears exactly once, **including the unjudged ones**: one missing from the report is indistinguishable from one that passed. Definitions are checked too — a determining equality whose sides are both already known is a redundancy check, which is exactly the over-determined case `Flatten` reports, and a failing one is the inconsistency finding. `IsComplete` stays about *values*; `AllRelationshipsHold` is the separate question. **The side-labeling question is resolved:** an outcome stores positional `Lhs`/`Rhs` and exposes `Subject`/`Criterion` as *derived* views, null where the relationship has no asymmetry — "actual vs. expected" holds for a check against a criterion but lies about a corroboration of two peer estimates. The verdict taxonomy falls out of the same distinction: an outcome is a violation iff it has a criterion, an inconsistency iff it does not — and having a criterion is exactly being a `Requirement`, so the two derivations coincide.
- [x] **Confidence ladder, structural half** (PR #41) — under uncertainty a comparison has several nested answers, and the arithmetic producing any produces all. Ten of the thirteen operators became a fixed-tier read off `OrderingLadder` (`Possible`/`Nominal`/`Certain`, a chain, with `Achieved`) or `ContainmentLadder` (`Overlaps`/`NominalWithin`/`NominalAndUpper`/`NominalAndLower`/`WhollyWithin`). The greater-than family is the ordering ladder with operands swapped, so one evaluator serves four operators. Containment's middle rungs form a **lattice, not a chain**, so it deliberately has no single ordered `Achieved` — that would force a precedence between "cannot overshoot" and "cannot undershoot". Three operators stay off: `EqualityOperator` (injected strategy, nothing to tier) and `UpperBoundsLessThan`/`LowerBoundsGreaterThan` (they compare a derived *statistic* of each side, and are not mirror images of each other). No behavior change and no wire change. **Key finding:** `WhollyWithinTolerance` is strict on both bounds while every rung below is not, so identical intervals satisfy every rung but the top — that costs nothing, because ladder soundness only needs implications running downward, and "fixing" it would change the one boundary case that genuinely arises.
- [x] Add `ExponentialExpression(argument: IExpression)` — unary expression computing `e^x`; requires argument to be dimensionless; result is dimensionless; uncertainty: `RelativeUncertainty(exp(x)) ≈ |x| · RelativeUncertainty(x)`. *(Done; dimensionless requirement enforced on construction/assignment.)*
- [x] Add `NaturalLogExpression(argument: IExpression)` — unary expression computing `ln(x)`; requires argument to be dimensionless and positive; result is dimensionless; uncertainty: `AbsoluteUncertainty(ln(x)) ≈ RelativeUncertainty(x)`; primary motivation is Arrhenius equations (`k = A · exp(-Eₐ / (R·T))`). *(Done. Note: result at `x = 1` is 0, whose relative error is undefined in the current relative-only uncertainty representation, so `Value` throws there — see below.)*
- [x] Add `SqrtExpression(argument: IExpression)` — unary expression computing `√x`; requires all dimensional exponents of the argument to be even integers (so that the result has integer-exponent dimensions); result dimensionality has each exponent halved (e.g. `√(m²·s⁻²)` → `m·s⁻¹`); argument value must be non-negative; uncertainty: `RelativeUncertainty(√x) = ½ · RelativeUncertainty(x)`; pulled forward from M5 `PowerExpression` because it is needed for Torricelli-law expressions (`Q = Cᵈ·a·√(2gh)`) in the ODE tank-draining use case. *(Done; reuses `Measurand.ToRoot(2)`.)*

*Note: results at/near zero (e.g. `ln(1)`) once threw under the relative-only uncertainty model; fixed by the absolute-error representation — see [Known Bugs](#known-bugs).*

---

### Milestone 4 — Solver *(coarse structural analysis only; the rest moved to D)*

- [x] **Structural analysis, coarse half** — `flat.Decompose()` → `Decomposition`, the Dulmage–Mendelsohn split into `Underdetermined` / `WellDetermined` / `Overdetermined`, each a `SystemBlock` of unknowns × the equations incident on them. A maximum bipartite matching (Kuhn's, iterative) over the incidence already carried on `Equation.Unknowns`, then two alternating-path reachability sweeps out from what the matching could not pair. Detects the structurally singular DoF-0 case the count cannot, and names the subset that can be solved while the rest of the system is stuck. **The count survives unchanged**, because the decomposition turned out to be its *breakdown* rather than a rival: each part's local DoF is a plain subtraction and the three sum to `FlatSystem.DegreesOfFreedom` — a `+1` and a `−1` cancelling is exactly the hidden-singularity case, so the count was aggregating, not lying. `UnknownsWithNoEquation` is subsumed as the cheapest slice (an unknown no equation touches is always in the underdetermined part; the converse does not hold). `Decompose` is structural only: it says an equation is available for an unknown, never that it can be inverted for it.

---

### Milestone 4.5 — Authoring and a front-end ✅ *(open items moved to B and C)*

Goal: let a non-engineer build and run a model, which means something to build it *with*.

**Design principle:** the domain types refuse to exist half-built, and that is what makes the evaluated graph safe to absorb, cache and walk. An editor cannot honor those rules between keystrokes. So authoring gets a **second, mutable model** that is freely invalid and builds into an `ExpressionSystem`; `Expression` itself stays frozen. Same resolution as the tolerant-deserialization item under M5 — the flexibility belongs in the mode, not in the types.

**Architecture decisions made 2026-10-11:**

- **The client owns the draft; the server stays stateless.** The browser holds a draft and posts it whole to validate or build. No draft store, no sessions, no concurrency control, no patch protocol. Fine for single-user editing, and the thing to revisit only when collaboration or undo/redo is actually wanted.
- **A draft gets its own wire DTOs, every field optional** — not loosened snapshots. A draft answers "what has been supplied so far", which is a different question from a snapshot's "what data defines this node", and the serializer relies on snapshot completeness being what makes a dangling reference an error rather than a hole.
- **Sequencing is builder → wire shape → API → UI**, builder first and on its own, because its shape is what everything above it is built against.

- [x] **`Calcusystem.Authoring`** — `SystemDraft` / `ExpressionDraft` / `RelationshipDraft`, all mutable with every field optional, and `DraftProblem` reporting what stands in the way. Nothing throws for a modelling mistake; `Build()` returns `Either<ExpressionSystem>`, failing with a `Failure<IReadOnlyList<DraftProblem>>` that carries every problem rather than one message naming the first, and `Validate()` reads its answer off a `Build()` so the two cannot disagree. Ids are minted before a node is valid and carry through the build, so a front-end's references survive it. Building runs two passes: *shape* checked here (kind chosen, slots filled, arity, references resolvable, no cycles, no duplicate ids), then *rules* asked of the domain by constructing each node in dependency order and relaying whatever it refuses — deliberately **not** restating the domain's rules, which would be two things answering one question. Construction is repeated passes rather than a topological sort, since what is left when progress stops is exactly what could not be ordered; a mark spread from the failures separates cascades (silent) from cycles (reported). This relies on every expression type settling its dimensional rules at construction; `SqrtExpression` deferred its exponent check to the first read of `Dimensionality`, which authoring worked around with a probe before the check was moved into the constructor where the rest of them live. `ExpressionDraftType` is one flat set where the snapshots group kinds by arity, with a coverage test holding the two in step.
- [x] **`Either<TValue>` in `Calcusystem.Core`** — a value or a `Failure` (a transport-free `FailureReason` plus a message), with `Match` / `Map` / `Then` / `Switch` / `ValueOr`, and implicit conversions both ways so a method returning one reads like a method returning the value. Grew from a `Maybe`/`Success`/`Failure` sketch; the rework fixed three things. The sketch put the payload and the error message on a **shared interface**, so `failure.Result` compiled and threw at runtime — the exception the type exists to replace; now the value lives only on `Succeeded<T>` and the failure only on `Failed<T>`, and there is deliberately no `Value`/`IsSuccess`/`IsFailure` on the base, since either would be answerable on a failure and reached by a cast. It carried `HttpStatusCode` in the basement, making the domain pick between `400` and `422` and forcing every non-HTTP consumer to speak HTTP; the reason is now a domain enum and the mapping belongs to the outermost layer. And `IsFailure` was derived from the status code rather than from the case, which is the usual two-answers-to-one-question shape. **This revises `Core`'s scope boundary**, which said the assembly holds nothing with real behavior: shared vocabulary may carry the behavior that makes it usable, the test being whether that behavior is about the vocabulary itself rather than any layer's subject. `Core.Test` is new, covering `Either` and the README snippet.
  - Known gap, documented: the hierarchy is closed by convention, not by the compiler. The parameterless constructor is `private protected`, but a record's copy constructor cannot be narrower than `protected` while the record is unsealed (CS8878) and an outside type can reach it explicitly. Verified. This is why `Match` is abstract rather than a `switch` over the two known cases — `Match` stays total whatever exists.
  - Settled: `SystemDraft.Build` returns `Either<ExpressionSystem>`. A failure carrying a *list* was the open question, and the answer is `Failure<TDetail>` — a subtype rather than a second type parameter on `Either`, so a caller that only relays or reports a failure needs to know nothing about the detail, and relaying preserves the instance so a list attached deep in a call stack survives to the top. Its cost is documented where it bites: a signature returning `Either<T>` does not advertise what its failures carry, and the layer that needs the list recovers it by a type test. `Calcusystem.Api` is that layer.
- [x] **A wire shape for a draft** — `Dtos.SystemDraft` / `ExpressionDraft` / `RelationshipDraft` in `Calcusystem.Serialization`, every field optional, mapped both ways by a static `DraftMapper` that needs no context, no ordering and no resolver, because a draft is already flat and tolerates a reference to nothing. Every half-built state survives unchanged, including a value whose dimensionality disagrees with its node's — which an editor can reach and the build reports, so `ValueDimensionality` exists to stop the trip mending it. What is still refused is a payload that cannot be *held* as a draft: two nodes in one list claiming one id would displace each other, and a malformed dimensionality would read as quietly dimensionless. An id shared by an expression and a relationship is held and reported as `DuplicateId` instead. The flat uncertainty and provenance DTOs moved into codecs shared with the system mappers, so one payload fragment cannot come to mean two things.
- [x] **An HTTP surface** — `Calcusystem.Api`, an ASP.NET Core controller app over `Authoring`, `Serialization` and `Analysis`. `POST /api/drafts/{validation,build,decomposition,calculation}` each take a whole draft and keep nothing; `GET /api/vocabulary` reads every kind, role, rule and dimension off the enums so a front-end holds no copy of them. `Http/FailureTranslation` is the only place in the codebase that names a status code, and is total over `FailureReason` so a reason added to the enum cannot quietly become a 500. A problem document carries `reason` (the domain enum, since several reasons share a code) and `problems` (every `DraftProblem`, recovered by a type test for `Failure<IReadOnlyList<DraftProblem>>` — an editor has to mark up each node at fault). `validation` answers 200 with its problems, because an unfinished model is the answer rather than a failed request, where the build routes answer 422. Field names are served as written and enums as names, so a client can save what it was served. `Api.Test` runs over real HTTP through `WebApplicationFactory`, and compares the README's own payloads against what is served.
  - **The `Analysis` entry-point reorganization did not block this, and is not resolved by it.** `Workflows/DraftWorkflow` is the only type in the assembly that names `Calcusystem.Analysis`, and it is four one-line methods, so changing how that layer is entered lands in one file rather than across a controller surface.
  - **`GET /api/units`** (added for the editor's unit pickers) serves every unit grouped by dimensionality, each carrying `UnitOfMeasure`'s own `KmsConversionFactor` and `ZeroOffset` under those names, so a client's two conversion functions are `ConvertFromKmsValue` and `ConvertToKmsValue` spelled the same way and the two sides read against each other. Those members were `internal`, to discourage hand-rolled conversions in favor of the objects; the catalogue first derived them by probing the public methods instead, which the owner judged more complexity than the visibility was worth — the sugar is what steers callers, so they are public now. What travels is the formula rather than the numbers, and an inverted factor is what that invites, which looks right at exactly 1; `UnitCatalogueTests` applies both directions back for every unit served, at values either side of zero. A band converts by the factor alone, since the offset would turn ±1 K into −457.87 °F.
  - Not built, deliberately: an **override on an analysis request** (an editor sets a value on the node and posts the draft again, so nothing needed one yet), and **units on the wire** — values are KMS doubles with an encoded dimensionality, and displaying 4.409 lb needs the unit catalogue served.
- [x] **A front-end** — `web/`, React + TypeScript + Vite over `Calcusystem.Api`. Variables, formulas and relationships, with problems shown on the node that holds them, degrees of freedom and the decomposition beside them, and the computed values with their uncertainty bands. The draft is posted whole after it stops changing and lives in `localStorage`, which is the "client owns it" decision made literal.
  - **The client's types are generated, not written.** `dotnet build` writes the OpenAPI document to `Api/Api.json`; `npm run dev` and `npm run build` regenerate `src/api/schema.d.ts` from it first, so a renamed DTO field is a TypeScript error rather than a runtime surprise. The alternative — a parallel set of interfaces — is two answers to one question, and the one this codebase keeps removing.
  - **The document had to be made true first.** It is described from the *HTTP* JSON options where a controller answers from the *MVC* ones, so it said camelCase and integer enums while the server served PascalCase and names; `Http/WireJson` now configures both. Element nullability does not survive `List<string?>` either, and a null operand is the whole representation of an unfilled slot, so `Http/WireSchema` corrects it. `OpenApiDocumentTests` holds all three.
  - **A new variable has no dimensionality, not a dimensionless one.** The editor's first version defaulted to dimensionless, which is exactly the guess the domain refuses to make — it would silently build a different model than the one intended. It now shows `— not chosen —` and the server reports what is missing until the author answers.
  - The menus are served from `/api/vocabulary` rather than held in the client, so a kind added to a domain enum is offered to an author with nothing in `web/` touched.

---

## Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Internal representation | KMS (kg-m-s) | Normalizing to SI base units avoids conversion bugs at operation time |
| Magnitude vs Delta | Unified into a single `Measurand` type | Point-vs-delta physical semantics (e.g. "lengths can't be negative, temperature changes can") is a modeling concern, not a value-type concern — the two-class split added complexity without enough payoff |
| Error propagation | RSS (uncorrelated) default, direct sum (correlated) available | Standard engineering uncertainty practice |
| Solver abstraction | Interface-based, swappable | Different problem domains may call for symbolic, numeric, or constraint solvers |
| Provenance | Composition, not inheritance: `Variable.Provenance` / `IBinaryOperator.Provenance` is an optional `IProvenance` (kinds: measured, reference, design, model) created via `ProvenanceFactory` | The four categories have no behavioral differences — only kind-specific audit metadata — so subtyping would encode a distinction with no behavior behind it. Public kinds with internal ctors keep construction funnelled through the factory; serialization lives in `Calcusystem.Serialization` like everything else (a flat `Dtos.Provenance` mirroring how `IUncertainty` is handled). Provenance is orthogonal to physical semantics and never affects evaluation |
| Definitions vs. Constraints | Definitions compute unknowns; Constraints check values | Conservation laws and constitutive equations belong in `Definitions`; tolerance checks belong in `Constraints` |
| OffsetUnitOfMeasure | Inheritance from UnitOfMeasure | Acceptable for now; temperature is the only offset case and it works |

---

## Open Questions

- **How correlation is stated.** A design is sketched under *Expression correlation as a pairwise property* in [E1](#e--uncertainty-accuracy); the parts not yet decided are listed there.

## Resolved Design Questions

- **`UncertaintyCorrelation` namespace** — stays in `Measurement`. Uncertainty propagation is a first-class concern of the layer, not a concern to be exiled elsewhere. The namespace name may be slightly narrow but the placement is correct.
- **Scope of `ExpressionSystem`** — one coherent *model* (one equation-of-state, one heat exchanger, one reactor). A full process flowsheet is assembled by *composing* `ExpressionSystem` instances with explicit variable mappings between their ports (see M5 composition feature). This makes the scope question answerable: a system knows its own boundary variables and nothing beyond them.
- **Definitions vs. instances** — resolved by the variable provenance taxonomy (M3) and the Definitions/Constraints semantic model (M3). See Key Design Decisions.
