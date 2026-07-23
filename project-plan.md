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

*The longer-term product and ecosystem vision for Calcusystem lives in a companion private repository and is out of scope here.*

---

## Immediate Next Steps

Tasks that are valuable but are not obvious prerequisites for the next milestone. Empty by default; populated when work surfaces that doesn't fit cleanly into a milestone.

- [ ] **Per-assembly READMEs** — Add a `README.md` to the root of each project folder (`Measurement/`, `DimensionedExpression/`, `Calcusystem.Serialization/`, `Measurement.Test/`, `DimensionedExpression.Test/`). Each README should cover: what the project is responsible for, the key interfaces/types it exposes, key invariants (e.g. all values are KMS-normalized), dependencies on other projects, and explicit scope boundaries (what does NOT belong here). Goal: an LLM session can load the README + interface files to understand how to *use* the project without needing implementation files in context; implementation files are only needed when *modifying* the project.
- [ ] **Interface docstring comments** — Add thorough XML `<summary>` / `<remarks>` / `<param>` comments to all public interfaces (`IExpression`, `IDirectExpression`, `ICalculatedExpression`, `IBinaryOperator`, `IUncertainty`, `ISymmetricUncertainty`, and the expression system interfaces). The docstrings should articulate the contract — invariants, expectations, and what implementations must guarantee — not just restate the member name. This completes the LLM-context strategy: README for orientation, interface docstrings for contract, implementation only when modifying.
- [ ] **Project README** — Fill out the top-level `README.md` with: project overview and motivation; the three-layer mental model (load README + interfaces to *use* a module; load implementation only to *modify* it); project structure with one-line descriptions of each assembly; quick-start usage example; contributing notes. Audience is anyone who might use or contribute to the library.

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

**Expression type additions (prerequisite for evaluation and provenance reporting):**

- [ ] Rename `ICalculatedExpression` / `CalculatedExpressionBase` → `IComputedExpression` / `ComputedExpressionBase` throughout — "calculated" is ambiguous; "computed" avoids collision with "derivative" once ODE relationships are added in M5
- [ ] Introduce leaf variable subtypes, each implementing `IDirectExpression`. These capture *provenance semantics* — orthogonal to the existing `Magnitude`/`Delta` axis which captures *physical semantics*:
  - `MeasuredVariable` — an instrument or sensor reading; uncertainty characterises instrument calibration and repeatability; may carry instrument metadata (calibration date, instrument ID)
  - `ReferenceConstant` — a literature or tabulated value (thermodynamic property, material property, physical constant); uncertainty from the source's stated precision or treated as exact; carries provenance/citation (same idea as `ConversionSource` for unit factors)
  - `DesignParameter` — an engineer-specified value, not measured or from literature; exact or carries a manufacturing/specification tolerance via `BoundedUncertainty`
  - `ModelParameter` — an empirically fitted constant within a constitutive relationship (e.g. discharge coefficient `Cᵈ`, Nusselt correlation coefficients); uncertainty from the fitting process; distinct from `ReferenceConstant` because it is model-specific, not a physical property
- [ ] Resolve the `Definitions` / `Constraints` / instances semantic model: `Definitions` are always-true relationships used to *compute* unknowns (conservation laws, constitutive equations); `Constraints` are tolerance checks run against computed or measured values (pass/fail); leaf variable subtypes above replace the informal notion of "instances"

**Evaluation engine:**

- [ ] Graph walk: for each expression, if all dependencies are set, compute its value and propagate uncertainty
- [ ] Run all constraints (`Definitions` and `Constraints` lists) and report pass/fail with actual vs. expected values
- [ ] Surface a clean result model (which expressions resolved, which constraints passed/failed, which variables are still missing)
- [ ] Add conversion factor provenance to `UnitOfMeasure` — a structured `ConversionSource` record carrying the standard name (e.g. "NIST SP 811"), URL, and year for non-trivial factors like lb→kg or BTU→J. Include provenance in the serialization DTOs so exported calculations carry a full audit trail of where their conversion factors came from.
- [ ] Add `ExponentialExpression(argument: IExpression)` — unary expression computing `e^x`; requires argument to be dimensionless; result is dimensionless; uncertainty: `RelativeError(exp(x)) ≈ |x| · RelativeError(x)`
- [ ] Add `NaturalLogExpression(argument: IExpression)` — unary expression computing `ln(x)`; requires argument to be dimensionless and positive; result is dimensionless; uncertainty: `AbsoluteError(ln(x)) ≈ RelativeError(x)`; primary motivation is Arrhenius equations (`k = A · exp(-Eₐ / (R·T))`)
- [ ] Add `SqrtExpression(argument: IExpression)` — unary expression computing `√x`; requires all dimensional exponents of the argument to be even integers (so that the result has integer-exponent dimensions); result dimensionality has each exponent halved (e.g. `√(m²·s⁻²)` → `m·s⁻¹`); argument value must be non-negative; uncertainty: `RelativeError(√x) = ½ · RelativeError(x)`; pulled forward from M5 `PowerExpression` because it is needed for Torricelli-law expressions (`Q = Cᵈ·a·√(2gh)`) in the ODE tank-draining use case

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
- [ ] **Integer nth root** — `NthRootExpression(argument: IExpression, n: int)`. Natural generalization of `SqrtExpression` (M3) to arbitrary integer roots. Constraint: all dimensional exponents of the argument must be divisible by `n`, so the result always has integer-exponent dimensions (each exponent divided by `n`); e.g. `∛(m³·s⁻³)` → `m·s⁻¹` is valid, `∛(m²)` is not. Argument value must be non-negative (or handle odd `n` separately for signed values). Uncertainty: `RelativeError(x^(1/n)) = (1/n) · RelativeError(x)`.
- [ ] **Binary exponentiation** — `PowerExpression(base: IExpression, exponent: IExpression)`. Exponent must be dimensionless (and ideally a rational constant for dimensional analysis to remain tractable). Dimensionality of result = base dimensionality raised to the exponent; unlike `NthRootExpression`, integer-exponent results are not guaranteed and are the caller's responsibility. Uncertainty: standard power-rule propagation. Complements `ExponentialExpression` (`e^x`) from M3 for general `x^n` expressions.
- [ ] **`ExpressionSystem` composition** — Named *ports* on each `ExpressionSystem` (the subset of its variables exposed as inputs/outputs), plus a `ComposedExpressionSystem` that connects sub-systems by mapping ports. Any sub-system exposing matching port names can be substituted — enabling e.g. swapping ideal-gas EOS for Peng-Robinson within a larger reactor model without touching the surrounding system. The M4 solver abstraction should be designed port-aware so it can traverse a composed system; composition itself is deferred until post-M4. Granularity rule: one `ExpressionSystem` per coherent model; a full process flowsheet is a `ComposedExpressionSystem`.
- [ ] **Data reconciliation** — A `ReconciledVariable` type that aggregates multiple independent `MeasuredVariable` nodes referring to the same physical quantity and finds the weighted least-squares estimate consistent with all `Definitions` (conservation laws). When redundant measurements disagree beyond their stated uncertainties, reconciliation surfaces the inconsistency rather than silently propagating a single measurement's error. Requires the algebraic solver (M4) to be in place as the constraint backbone.
- [ ] **Dynamic (ODE) relationships** — A `DerivativeRelationship` type linking two variables through a time derivative (`rate = d(quantity)/dt`), enabling lumped-parameter transient models: filling tanks, thermal mass, RC circuits, spring-mass systems. Scope is deliberately restricted to *time as the sole independent variable* (excludes all PDEs, including Navier-Stokes), *initial value problems only* (conditions at `t = 0`; excludes BVPs requiring shooting methods or collocation), and *explicit first-order form* `y' = f(t, y)` (higher-order systems reduce to first-order via state-space substitution; implicit DAEs deferred). Integral relationships (`quantity = ∫rate dt`) are the inverse case handled by the same mechanism. Solving requires a numerical ODE integrator (RK4, Dormand-Prince, or similar) plugged in via the M4 solver abstraction layer.
- [ ] **ODE system diagnostics** — A `SystemDiagnostics` report that runs before the ODE solver and surfaces structured findings so engineers can act on specific information rather than diagnosing solver failures at runtime:
  - *Stiffness*: compute the Jacobian `J = ∂f/∂y` (symbolic from the expression tree or numeric via finite differences); stiffness ratio = `max(|Re(λᵢ)|) / min(|Re(λᵢ)|)` over eigenvalues of `J`; ratio >> 1000 → recommend an implicit solver (BDF, Radau) instead of explicit RK4.
  - *Discontinuities*: walk the expression tree for `abs()`, `min/max`, conditional or lookup-table terms; flag as potential discontinuity sources and recommend event-detection restart logic.
  - *DAE structure*: detect algebraic constraints mixed with differential relationships; compute the DAE index (number of symbolic differentiations needed to recover a pure ODE); index > 1 requires index reduction (Pantelides algorithm) before any standard solver can proceed.
  - *Initial condition consistency*: verify that the `t = 0` values satisfy all algebraic constraints before integration begins.

---

## Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Internal representation | KMS (kg-m-s) | Normalizing to SI base units avoids conversion bugs at operation time |
| Magnitude vs Delta | Two separate types | Enforces physical semantics: lengths can't be negative, temperature *changes* can |
| Error propagation | RSS (uncorrelated) default, direct sum (correlated) available | Standard engineering uncertainty practice |
| Solver abstraction | Interface-based, swappable | Different problem domains may call for symbolic, numeric, or constraint solvers |
| Variable provenance taxonomy | Four leaf subtypes: `MeasuredVariable`, `ReferenceConstant`, `DesignParameter`, `ModelParameter` | Provenance axis is orthogonal to the `Magnitude`/`Delta` physical axis; affects uncertainty characterisation and result reporting without changing evaluation logic |
| Definitions vs. Constraints | Definitions compute unknowns; Constraints check values | Conservation laws and constitutive equations belong in `Definitions`; tolerance checks belong in `Constraints` |
| OffsetUnitOfMeasure | Inheritance from UnitOfMeasure | Acceptable for now; temperature is the only offset case and it works |

---

## Open Questions

None — all resolved; see Key Design Decisions.

## Resolved Design Questions

- **`ErrorPropagationMethod` namespace** — stays in `Measurement`. Uncertainty propagation is a first-class concern of the layer, not a concern to be exiled elsewhere. The namespace name may be slightly narrow but the placement is correct.
- **Scope of `ExpressionSystem`** — one coherent *model* (one equation-of-state, one heat exchanger, one reactor). A full process flowsheet is assembled by *composing* `ExpressionSystem` instances with explicit variable mappings between their ports (see M5 composition feature). This makes the scope question answerable: a system knows its own boundary variables and nothing beyond them.
- **Definitions vs. instances** — resolved by the variable provenance taxonomy (M3) and the Definitions/Constraints semantic model (M3). See Key Design Decisions.
