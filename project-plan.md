# Calcusystem Project Plan

## Vision

An engineering calculation platform where physical quantities carry their units, dimensions, and measurement uncertainty as first-class concerns. Engineers describe a system of variables, formulas, and tolerances; the platform evaluates what can be computed, propagates uncertainty, checks constraints, and — eventually — solves for unknowns.

---

## Current State (as of June 2026)

Three functional layers exist with serialization support:

- **Measurement** — Physical quantities with KMS-normalized units, dimensional algebra, `Magnitude`/`Delta` types, and uncertainty tracking. Mostly solid, a few rough edges.
- **DimensionedExpression** — Expression tree for building equation systems. Direct and derived variables, tolerance-based constraint operators. Several stubs remain.
- **Calcusystem.Serialization** — DTO layer with dependency-ordered deserialization. Functional.

All projects target older framework versions (netcoreapp3.1 / net7.0) and need upgrading.

---

## Milestones

### Milestone 1 — Clean Foundation *(current)*

Goal: get the codebase into a clean, consistent state before building new features.

- [ ] Upgrade all projects to .NET 10
- [ ] Fix `IsSubnormal()` bug in `BaseQuantity` (calls `IsNormal()` instead)
- [ ] Rename `BaseQuantity` to `PhysicalQuantity` (self-described as a bad name)
- [ ] Fix `Magnitude.TryAdd(Magnitude)` missing error propagation
- [ ] Fill out missing unit types: `Pressure`, `ElectricPotential`, `ElectricResistance`, `AngularMomentum`, `MomentOfInertia`
- [ ] Fix `Gradian` bug in `Angle.cs`: `UnitFactory.Create("grad", 400, Revolution)` should be `1d/400` (as written, 1 gradian = 400 revolutions, which is ~2513 rad)
- [ ] Add `readonly` to all static fields in `Angle.cs` (missing unlike every other unit class)
- [ ] Add nominal gauge pressure units (`PsiG`, `BarG`, `KPaG`) to `Pressure` as `OffsetUnitOfMeasure` with a 101325 Pa offset, same pattern as `Celsius`. Add a comment that true gauge pressure (offset from actual ambient) is an expression relationship, not a unit.

---

### Milestone 2 — Complete the Expression Layer

Goal: close the gaps in `DimensionedExpression` so the expression system is fully usable.

- [ ] Implement `DegreesOfFreedom()` as a recursive graph walk counting unbound direct variables reachable from an expression (answers "how many inputs does this node still need?")
- [ ] Decide what `EqualityOperator` means and implement `IsSatisfied()` — either collapse it into `MutuallyWithinToleranceOperator`, or wire a DI-injectable epsilon via `IEqualityEstimating`
- [ ] Add a factory for `ExpressionSystem` (noted as TODO in code)
- [ ] Document / rename the cryptic tolerance operator symbols (`=}`, `[=}`, `[≓}`, `[≒}`)
- [ ] Extract uncertainty representation to an `IUncertainty` interface, replacing the raw `_relativeError: double?` field in `PrecisionQuantity`. Concrete implementations: `GaussianUncertainty` (current behavior), `AsymmetricUncertainty(upper, lower)`, `BoundedUncertainty(lower, upper)`. Monte Carlo propagation is deferred to Milestone 4.

---

### Milestone 3 — Evaluation Engine *(the payoff)*

Goal: given a populated `ExpressionSystem`, compute everything that can be computed and report constraint satisfaction.

- [ ] Graph walk: for each expression, if all dependencies are set, compute its value and propagate uncertainty
- [ ] Run all constraints (`Definitions` and `Constraints` lists) and report pass/fail with actual vs. expected values
- [ ] Surface a clean result model (which expressions resolved, which constraints passed/failed, which variables are still missing)

---

### Milestone 4 — Solver

Goal: given a system with some unknowns, determine if it is solvable and solve it.

**Design principle:** A robust abstraction layer sits between `ExpressionSystem` and any concrete solver, so different solver strategies can be plugged in (e.g. symbolic, numeric, linear algebraic).

- [ ] Define solver interface: takes an `ExpressionSystem`, returns a solution or a structured "unsolvable" result with explanation
- [ ] `DegreesOfFreedom()` (from Milestone 2) becomes the gate: DoF == 0 → evaluate; DoF == 1 → solve; DoF > 1 → report which variables are needed
- [ ] Implement a basic solver for product/quotient/sum relationships (the linear and multiplicative cases are tractable without a CAS)
- [ ] Leave the door open for a symbolic or numeric solver as a future plugin

---

## Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Internal representation | KMS (kg-m-s) | Normalizing to SI base units avoids conversion bugs at operation time |
| Magnitude vs Delta | Two separate types | Enforces physical semantics: lengths can't be negative, temperature *changes* can |
| Error propagation | RSS (uncorrelated) default, direct sum (correlated) available | Standard engineering uncertainty practice |
| Solver abstraction | Interface-based, swappable | Different problem domains may call for symbolic, numeric, or constraint solvers |
| OffsetUnitOfMeasure | Inheritance from UnitOfMeasure | Acceptable for now; temperature is the only offset case and it works |

---

## Open Questions

- Should `ErrorPropagationMethod` eventually move out of `Measurement` into a shared or expression-layer namespace? Currently `PrecisionQuantity` uses it directly, which makes moving it require reversing a dependency.
- What is the intended scope of `ExpressionSystem`? Single equation, full calculation sheet, or something in between?
- Should there be a distinction between *definitions* (always-true equalities, like `F = m * a`) and *instances* (a specific calculation with given values)?
