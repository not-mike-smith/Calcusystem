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
- [x] Extract uncertainty to `IUncertainty` interface (`Measurement/Uncertainty/`). Concrete implementations: `GaussianUncertainty` (existing behavior), `AsymmetricUncertainty(upper, lower)` (asymmetric relative errors), `BoundedUncertainty(upper, lower)` (asymmetric absolute KMS errors). Added `ISymmetricUncertainty : IUncertainty` sub-interface with default upper/lower implementations; `GaussianUncertainty` implements it. `IUncertainty` exposes `UpperAbsoluteError`/`LowerAbsoluteError` separately; all five tolerance operators updated to use directional bounds. Monte Carlo propagation deferred to Milestone 4.

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

- [x] Add `DimensionedExpression/BinaryOperators/OPERATORS.md` — a taxonomy table covering all operators (equality, the six tolerance operators from M2, and the six inequality operators above), with a one-line geometric description and the exact interval condition for each
- [x] Unit tests in `DimensionedExpression.Test` covering all six operators (symmetric and asymmetric uncertainty, boundary conditions, null returns for unbound expressions)

---

### Milestone 3 — Evaluation Engine *(the payoff)*

Goal: given a populated `ExpressionSystem`, compute everything that can be computed and report constraint satisfaction.

- [ ] Graph walk: for each expression, if all dependencies are set, compute its value and propagate uncertainty
- [ ] Run all constraints (`Definitions` and `Constraints` lists) and report pass/fail with actual vs. expected values
- [ ] Surface a clean result model (which expressions resolved, which constraints passed/failed, which variables are still missing)
- [ ] Add conversion factor provenance to `UnitOfMeasure` — a structured `ConversionSource` record carrying the standard name (e.g. "NIST SP 811"), URL, and year for non-trivial factors like lb→kg or BTU→J. Include provenance in the serialization DTOs so exported calculations carry a full audit trail of where their conversion factors came from.
- [ ] Add `ExponentialExpression(argument: IExpression)` — unary expression computing `e^x`; requires argument to be dimensionless; result is dimensionless; uncertainty: `RelativeError(exp(x)) ≈ |x| · RelativeError(x)`
- [ ] Add `NaturalLogExpression(argument: IExpression)` — unary expression computing `ln(x)`; requires argument to be dimensionless and positive; result is dimensionless; uncertainty: `AbsoluteError(ln(x)) ≈ RelativeError(x)`; primary motivation is Arrhenius equations (`k = A · exp(-Eₐ / (R·T))`)

---

### Milestone 4 — Solver

Goal: given a system with some unknowns, determine if it is solvable and solve it.

**Design principle:** A robust abstraction layer sits between `ExpressionSystem` and any concrete solver, so different solver strategies can be plugged in (e.g. symbolic, numeric, linear algebraic).

- [ ] Define solver interface: takes an `ExpressionSystem`, returns a solution or a structured "unsolvable" result with explanation
- [ ] `DegreesOfFreedom()` (from Milestone 2) becomes the gate: DoF == 0 → evaluate; DoF == 1 → solve; DoF > 1 → report which variables are needed
- [ ] Implement a basic solver for product/quotient/sum relationships (the linear and multiplicative cases are tractable without a CAS)
- [ ] Leave the door open for a symbolic or numeric solver as a future plugin

---

### Milestone 5 — Wishlist *(scope not yet committed)*

These features are worth designing for but intentionally deferred until M4 is solid.

- [ ] **Complex number support** — A `ComplexExpression` type holding `Re : IExpression` and `Im : IExpression` children, supporting complex arithmetic (add, multiply, divide, conjugate). Exposes `.Magnitude()` → `sqrt(Re² + Im²)` and `.Phase()` → `atan2(Im, Re)` as regular `IExpression` nodes. Never promotes directly to `PhysicalQuantity`; callers must extract a real component. Primary motivation: AC circuit analysis with phasor impedance.
- [ ] **Binary exponentiation** — `PowerExpression(base: IExpression, exponent: IExpression)`. Exponent must be dimensionless (and ideally a rational constant for dimensional analysis to remain tractable). Dimensionality of result = base dimensionality raised to the exponent. Uncertainty: standard power-rule propagation. Complements `ExponentialExpression` (`e^x`) from M3 for general `x^n` expressions.

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
