# Calcusystem Project Plan

## Vision

An engineering calculation platform where physical quantities carry their units, dimensions, and measurement uncertainty as first-class concerns. Engineers describe a system of variables, formulas, and tolerances; the platform evaluates what can be computed, propagates uncertainty, checks constraints, and — eventually — solves for unknowns.

---

## Current State (as of June 2026)

Three functional layers exist with serialization support:

- **Measurement** — Physical quantities with KMS-normalized units, dimensional algebra, a unified `Measurand` value type, and uncertainty tracking. Mostly solid, a few rough edges.
- **DimensionedExpression** — Expression tree for building equation systems. Direct and derived variables, tolerance-based constraint operators. Several stubs remain.
- **Calcusystem.Serialization** — DTO layer with dependency-ordered deserialization. Functional.

All projects target older framework versions (netcoreapp3.1 / net7.0) and need upgrading.

*The longer-term product and ecosystem vision for Calcusystem lives in a companion private repository and is out of scope here.*

---

## Immediate Next Steps

Tasks that are valuable but are not obvious prerequisites for the next milestone. Empty by default; populated when work surfaces that doesn't fit cleanly into a milestone.

- [x] **Per-assembly READMEs** (PR #16) — Added `README.md` to the three library assemblies (`Measurement/`, `DimensionedExpression/`, `Calcusystem.Serialization/`), each covering responsibility, key interfaces/types, invariants (e.g. all values are KMS-normalized), dependencies, and explicit scope boundaries. Goal met: README + interface files are enough to *use* a project without its implementation in context. *Test-project READMEs are the remaining piece — see the outstanding item below.*
- [x] **Interface docstring comments** (PR #16) — All public interfaces across the three assemblies now carry XML `<summary>` / `<remarks>` / `<param>` comments on the interface and every member, articulating the contract: `IUncertainty`, `ISymmetricUncertainty`, `IErrorPropagator` (Measurement); `IExpression`, `IDirectExpression`, `IComputedExpression`, `IBinaryOperator`, `IEqualityEstimating` (DimensionedExpression); `ISerializedObject` (Serialization). Measurement additionally documents the `Quantity`/`Dimensionality` structs, `FundamentalDimension`, `UnitOfMeasure`/`OffsetUnitOfMeasure`, and the `UncertaintyFromNominalValue` delegate (flagged in the Measurement README). This completes the LLM-context strategy: README for orientation, interface docstrings for contract, implementation only when modifying.
- [x] **Project README** (PR #16) — Top-level `README.md` filled out with overview/motivation, the reading model (README + `Interfaces/` to *use*; implementation only to *modify*), the assembly structure, a verified quick-start, and build/test/contributing notes.
- [ ] **Test-project READMEs** — Add a `README.md` to `Measurement.Test/`, `DimensionedExpression.Test/`, and `Calcusystem.Serialization.Test/`. These follow a different structure than the library READMEs (purpose, coverage, test helpers/patterns, how to run) — to be established when tackled.
- [ ] **Factory-construct the binary operators** — mirror the provenance pattern for `IBinaryOperator`'s concrete types: keep the classes public but make their construction flow through a single factory (public types, internal ctors). Improves discoverability of the operator taxonomy and keeps the deserializer's operator switch aligned with one place. Surfaced during the provenance work.

*Incidental fixes made during the documentation pass (PR #16), each with a regression test:* `Measurand.TryAdd`/`TrySubtract` no longer throw on dimension mismatch (return a NaN-valued `Measurand`); `DeserializingMapper.GetExpression` was resolving the parent DTO's own id instead of the requested child id, breaking deserialization of every derived expression/operator (first `Calcusystem.Serialization.Test` project added). Also renamed `NegatedVariable` → `NegatedExpression` (inner property → `Operand`).

---

## Known Bugs

- [x] **Uncertainty at and near zero — implementation.** Fixed: uncertainty now stores its error as *either* a relative fraction or an absolute KMS value, tagged by `UncertaintyKind` (`Relative`/`Absolute`; `Interval` reserved). Absolute storage needs no divide-by-value, so a zero-valued result carries a meaningful error; `RelativeError(0)` returns `+∞` instead of throwing. Sums/differences propagate to an absolute-error result (no dividing by the summed value); products still compose relative errors. `ln(1)` and canceling sums no longer throw (regression tests in `Measurement.Test` and the updated `NaturalLogExpression` test). Serialization carries the `kind`.
- [ ] **Uncertainty crossing zero for magnitudes — semantic (deferred to the evaluation layer).** When an error interval spans zero, for a **magnitude** (a physically non-negative quantity) a lower bound below zero is not physical. Decision made: **stay signed, don't clamp** in the uncertainty layer; instead surface a *diagnostic/flag* at report/constraint time when a quantity expected to be non-negative has an interval crossing zero. This needs the "expected non-negative" signal, which lives at the modeling layer — implement alongside the evaluation engine's result model.

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

- [x] Rename `ICalculatedExpression` / `CalculatedExpressionBase` → `IComputedExpression` / `ComputedExpressionBase` throughout — "calculated" is ambiguous; "computed" avoids collision with "derivative" once ODE relationships are added in M5. *(Done; README caveat dropped.)*
- [x] Capture *provenance semantics* for leaf variables — orthogonal to *physical semantics* (point vs. signed delta/difference, a modeling concern the `Magnitude`/`Delta` removal left to context). **Design changed from the original "Variable subtypes" plan:** the four provenance categories have no behavioral differences, only kind-specific audit metadata, so they are modeled by **composition, not inheritance** — `Variable.Provenance` (and `IBinaryOperator.Provenance`, so Definitions/Constraints can carry citations too) is an optional `IProvenance` (null = untracked), created through the single `ProvenanceFactory`. `IProvenance` exposes `Id` (it round-trips like any node) and `Summary()` (for UI). The concrete kinds are public (so the serializer can map them) with internal ctors (factory-only construction). Serialization lives in `Calcusystem.Serialization` like everything else — a flat `Dtos.Provenance` (Type discriminator + union of fields) nested in the `SingleVariable`/`BinaryOperator` DTOs, mapped via `Map`/`MapProvenance` and reconstructed through the factory. *(Done, including serialization round-trip.)* The four kinds:
  - `Measured` — an instrument or sensor reading; uncertainty characterises instrument calibration and repeatability; metadata: instrument id, calibration date
  - `Reference` — a literature or tabulated value (thermodynamic property, material property, physical constant); uncertainty from the source's stated precision or treated as exact; metadata: citation, URL, year (same idea as `ConversionSource` for unit factors)
  - `Design` — an engineer-specified value, not measured or from literature; exact or carries a manufacturing/specification tolerance via `AsymmetricUncertainty.FromAbsErr`; metadata: spec/drawing reference
  - `Model` — an empirically fitted constant within a constitutive relationship (e.g. discharge coefficient `Cᵈ`, Nusselt correlation coefficients); uncertainty from the fitting process; distinct from `Reference` because it is model-specific, not a physical property; metadata: model name, fitting reference
- [ ] Resolve the `Definitions` / `Constraints` / instances semantic model: `Definitions` are always-true relationships used to *compute* unknowns (conservation laws, constitutive equations); `Constraints` are tolerance checks run against computed or measured values (pass/fail); the provenance annotation on `Variable` above replaces the informal notion of "instances"

**Evaluation engine:**

- [ ] Graph walk: for each expression, if all dependencies are set, compute its value and propagate uncertainty
- [ ] Run all constraints (`Definitions` and `Constraints` lists) and report pass/fail with actual vs. expected values
- [ ] Surface a clean result model (which expressions resolved, which constraints passed/failed, which variables are still missing)
- [ ] Add conversion factor provenance to `UnitOfMeasure` — a structured `ConversionSource` record carrying the standard name (e.g. "NIST SP 811"), URL, and year for non-trivial factors like lb→kg or BTU→J. Include provenance in the serialization DTOs so exported calculations carry a full audit trail of where their conversion factors came from.
- [x] Add `ExponentialExpression(argument: IExpression)` — unary expression computing `e^x`; requires argument to be dimensionless; result is dimensionless; uncertainty: `RelativeError(exp(x)) ≈ |x| · RelativeError(x)`. *(Done; dimensionless requirement enforced on construction/assignment.)*
- [x] Add `NaturalLogExpression(argument: IExpression)` — unary expression computing `ln(x)`; requires argument to be dimensionless and positive; result is dimensionless; uncertainty: `AbsoluteError(ln(x)) ≈ RelativeError(x)`; primary motivation is Arrhenius equations (`k = A · exp(-Eₐ / (R·T))`). *(Done. Note: result at `x = 1` is 0, whose relative error is undefined in the current relative-only uncertainty representation, so `Value` throws there — see below.)*
- [x] Add `SqrtExpression(argument: IExpression)` — unary expression computing `√x`; requires all dimensional exponents of the argument to be even integers (so that the result has integer-exponent dimensions); result dimensionality has each exponent halved (e.g. `√(m²·s⁻²)` → `m·s⁻¹`); argument value must be non-negative; uncertainty: `RelativeError(√x) = ½ · RelativeError(x)`; pulled forward from M5 `PowerExpression` because it is needed for Torricelli-law expressions (`Q = Cᵈ·a·√(2gh)`) in the ODE tank-draining use case. *(Done; reuses `Measurand.ToRoot(2)`.)*

*Note: results at/near zero (e.g. `ln(1)`) once threw under the relative-only uncertainty model; fixed by the absolute-error representation — see [Known Bugs](#known-bugs).*

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
| Magnitude vs Delta | Unified into a single `Measurand` type | Point-vs-delta physical semantics (e.g. "lengths can't be negative, temperature changes can") is a modeling concern, not a value-type concern — the two-class split added complexity without enough payoff |
| Error propagation | RSS (uncorrelated) default, direct sum (correlated) available | Standard engineering uncertainty practice |
| Solver abstraction | Interface-based, swappable | Different problem domains may call for symbolic, numeric, or constraint solvers |
| Provenance | Composition, not inheritance: `Variable.Provenance` / `IBinaryOperator.Provenance` is an optional `IProvenance` (kinds: measured, reference, design, model) created via `ProvenanceFactory` | The four categories have no behavioral differences — only kind-specific audit metadata — so subtyping would encode a distinction with no behavior behind it. Public kinds with internal ctors keep construction funnelled through the factory; serialization lives in `Calcusystem.Serialization` like everything else (a flat `Dtos.Provenance` mirroring how `IUncertainty` is handled). Provenance is orthogonal to physical semantics and never affects evaluation |
| Definitions vs. Constraints | Definitions compute unknowns; Constraints check values | Conservation laws and constitutive equations belong in `Definitions`; tolerance checks belong in `Constraints` |
| OffsetUnitOfMeasure | Inheritance from UnitOfMeasure | Acceptable for now; temperature is the only offset case and it works |

---

## Open Questions

None — all resolved; see Key Design Decisions.

## Resolved Design Questions

- **`ErrorPropagationMethod` namespace** — stays in `Measurement`. Uncertainty propagation is a first-class concern of the layer, not a concern to be exiled elsewhere. The namespace name may be slightly narrow but the placement is correct.
- **Scope of `ExpressionSystem`** — one coherent *model* (one equation-of-state, one heat exchanger, one reactor). A full process flowsheet is assembled by *composing* `ExpressionSystem` instances with explicit variable mappings between their ports (see M5 composition feature). This makes the scope question answerable: a system knows its own boundary variables and nothing beyond them.
- **Definitions vs. instances** — resolved by the variable provenance taxonomy (M3) and the Definitions/Constraints semantic model (M3). See Key Design Decisions.
