# Measurement

The foundation layer of Calcusystem. Provides physical quantities with units, dimensions, and measurement uncertainty as first-class concerns. All other assemblies depend on this one; it has no Calcusystem dependencies of its own.

> **Using this assembly:** as with every project, this README plus the interfaces in `Interfaces/` cover what you need to *use* Measurement without reading implementation. Measurement is an exception in one respect — several non-interface types also carry essential contract docstrings worth reading directly: the `Quantity` and `Dimensionality` structs and the `FundamentalDimension` class.

---

## The central invariant: KMS normalization

**All values are stored internally in kg-m-s (SI base units).** Units are only relevant at the boundary — when constructing a quantity from a user-supplied value, or when reading a value back out in a specific unit. All arithmetic, comparison, and uncertainty propagation operates on KMS values directly. This eliminates an entire class of conversion bugs.

```csharp
var force = Force.PoundForce.Quantity(1.0).Measurand(GaussianUncertainty.FromRelErr(0));  // user supplies lbf
force.In(Force.OunceForce);  // 16.0   — conversion happens at output only
force.KmsValue;              // 4.448… — internal representation is always SI
```

---

## Type hierarchy

Listed from user-facing at the top to foundational primitive at the bottom.

| Name | Type | Extends | Description |
| ---- | ---- | ------- | ----------- |
| Measurand | class | | Quantity + IUncertainty |
| IUncertainty | interface | | describes the uncertainty interval around a KMS value |
| ISymmetricUncertainty | interface | IUncertainty | symmetric uncertainty; absolute error = relative error × \|v\| |
| GaussianUncertainty | class | ISymmetricUncertainty | Gaussian uncertainty for Type A errors |
| AsymmetricUncertainty | class | IUncertainty | independent upper/lower relative errors |
| Quantity | struct | | raw KMS value + Dimensionality; internal currency; no uncertainty |
| OffsetUnitOfMeasure | class | UnitOfMeasure | extends UnitOfMeasure with a fixed zero-point offset; see note below |
| UnitOfMeasure | class | | symbol + Dimensionality + KMS conversion factor (constructed via UnitFactory) |
| Dimensionality | struct | | maps FundamentalDimension → integer exponent; supports algebra |

**`Measurand`** operations, beyond the properties in [Uncertainty system](#uncertainty-system) below:

| Category | Members |
| --- | --- |
| Arithmetic | `Plus`/`Minus`/`Times`/`DividedBy` (throw `IncompatibleDimensionsException` on a `Plus`/`Minus` dimension mismatch); `TryAdd`/`TrySubtract` — dimension-tolerant like `Quantity.TryAdd`/`TrySubtract` below, returning a NaN-valued `Measurand` (with zero uncertainty) instead of throwing on mismatch; unary `-`; `Reciprocal()`; `ToPower(int)`/`ToRoot(int)` |
| Convert | `In(UnitOfMeasure)` (throws on dimension mismatch) / `TryIn(UnitOfMeasure)` (returns `NaN` on mismatch); `AbsoluteError(unit)` / `AbsoluteErrorIn(unit)` / `TryAbsoluteErrorIn(unit)` |
| Validity | `IsValid()` (NaN/finite only — see point/delta note below), `IsNaN()`, `IsInfinity()`/`IsPositiveInfinity()`/`IsNegativeInfinity()`, `IsFinite()`, `IsNormal()`/`IsSubnormal()`, `IsNegative()` |

**`Quantity`** is usable on its own for KMS math without uncertainty: `+`/`-`/unary `-`/`*`/`/` operators (`+`/`-` require matching `Dimensionality` and throw `IncompatibleDimensionsException` otherwise; `*`/`/` combine dimensions freely), `ToPower(int)`/`ToRoot(int)`, an explicit `(Quantity)someDouble` cast to a dimensionless quantity, and dimension-tolerant `TryAdd`/`TrySubtract` — these genuinely return a `NaN`-valued `Quantity` instead of throwing when dimensionalities differ, which is the behavior `Measurand`'s `Try*` methods above are meant to mirror.

**`OffsetUnitOfMeasure`** stores a fixed zero-point offset baked in at construction time — not a live ambient reading. It is used for two physical domains:

- **Temperature scales** (°C, °F) where 0 °C ≠ 0 K
- **Gauge pressure** (psig, barg) where the zero is nominal atmospheric pressure (101 325 Pa), not a measured ambient value. If the actual ambient pressure matters, the caller is responsible for the correction.

`OffsetUnitOfMeasure` also exposes a `DeltaUnit` property — the corresponding non-offset unit for expressing *changes* without re-adding the offset (e.g. `Δ°C` for a temperature difference).

**`Dimensionality`** is a `readonly struct` holding a `Dictionary<FundamentalDimension, int>`. Zero-exponent entries are automatically stripped. The nine fundamental dimensions are: `Mass`, `Length`, `Time`, `Temperature`, `ElectricCurrent`, `AmountOfMatter`, `LuminousIntensity`, `Angle`, `Currency`. Algebra is supported directly:

```csharp
var velocity   = Dimensionality.Length / Dimensionality.Time;  // L·t⁻¹
var energy     = Dimensionality.Mass * Dimensionality.Length * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
var specEnergy = velocity * 2;    // L²·t⁻²  (exponent scaling)
var root       = energy / 2;      // L·t⁻¹   (integer root; throws NondiscreteDimensionalityException if exponents aren't divisible)
```

**No point/delta distinction in the value type.** Earlier drafts had separate `Magnitude` (non-negative) and `Delta` (signed difference) classes; these were merged into the single `Measurand` above. Whether a value is a point quantity or a delta/difference is a *modeling* concern (how a variable is used in an expression or system), not a property of the value itself — the two-class split added complexity without enough payoff. Practical implications:

- `Measurand.IsValid()` only checks NaN/finite — it does **not** reject negative values. If a quantity must be non-negative, that's the caller's or the expression layer's responsibility.
- There is no automatic redirection through `OffsetUnitOfMeasure.DeltaUnit`. To represent a temperature *difference*, construct with the delta unit explicitly (`Temperature.DeltaFahrenheit`/`Temperature.DeltaCelsius`) — using the absolute unit (`Temperature.Fahrenheit`/`Temperature.Celsius`) for a difference will silently apply the zero-offset where it shouldn't.

---

## Uncertainty system

Defined in `Measurement/Uncertainty/`. The uncertainty interval around a nominal KMS value `v` is `[v − LowerAbsoluteError(v), v + UpperAbsoluteError(v)]`.

**Relative or absolute storage (`IsStoredAsAbs`).** Each uncertainty stores its error as *either* a relative fraction *or* an absolute KMS value, distinguished by a `bool IsStoredAsAbs`. This is purely a **convention on what the stored `Magnitude` means** — both encode the same error band, and converting between them is just multiplying or dividing by `|v|`. (A genuinely different model such as interval bounds is not another value of this flag; it would be a change in how errors *propagate*, handled at the `IErrorPropagator` level.) Which form is stored is invisible to consumers — you always read absolute or relative error through `IUncertainty` — but it matters at zero: an **absolute error is well-defined when the value is 0; a relative one is not**. `RelativeError(0)` returns `+∞` rather than throwing, which is what lets a sum that cancels to zero, or `ln(1)`, carry a meaningful error.

| Factory | Interface | Stored as |
| --- | --- | --- |
| `GaussianUncertainty.FromRelErr(relativeError)` | `ISymmetricUncertainty` | relative fraction |
| `GaussianUncertainty.FromAbsErr(absoluteError: Quantity)` | `ISymmetricUncertainty` | absolute KMS error |
| `AsymmetricUncertainty.FromRelErr(upperRel, lowerRel)` | `IUncertainty` | relative fractions |
| `AsymmetricUncertainty.FromAbsErr(upperError, lowerError: Quantity)` | `IUncertainty` | absolute KMS errors |

Both types' constructors are private — go through the factories: `FromRelErr` (relative), `FromAbsErr` (absolute — takes a `Quantity` and returns the uncertainty directly, since an absolute error needs no nominal value), or `From(isStoredAsAbs, magnitude…)` for deserialization.

```csharp
var mass = Mass.Kilogram.Quantity(1).Measurand(GaussianUncertainty.FromAbsErr(1.0.Units(Mass.Milligram)));
```

Propagation follows the storage: **sums/differences produce an absolute-error result** (no dividing by the possibly-zero sum), while products compose relative errors. A quantity whose interval crosses zero is left signed — clamping a non-negative "magnitude" at zero is a modeling concern for a higher layer, not baked in here.

**`ISymmetricUncertainty`** extends `IUncertainty` and adds default interface implementations of the directional members (`UpperAbsoluteError`/`LowerAbsoluteError` and their relative equivalents) in terms of the single `AbsoluteError`/`RelativeError`. Only `GaussianUncertainty` implements this.

`Measurand` exposes:

- `KmsUpperAbsoluteError` / `KmsLowerAbsoluteError` — directional errors; use these in operators and checks
- `KmsAbsoluteError` — `Max(upper, lower)`; conservative single value for propagation formulas
- `RelativeError` — `KmsAbsoluteError / |KmsValue|`; conservative for propagation
- `Uncertainty` — the raw `IUncertainty` instance; preserved through negation and `Reciprocal()`

---

## Error propagation

`Measurand` arithmetic (`Plus`, `Minus`, `Times`, `DividedBy`, `ToPower`, `ToRoot`) propagates uncertainty through an `IErrorPropagator` (`Measurement/Interfaces/IErrorPropagator.cs`):

| Method | Used for |
| --- | --- |
| `PropagateErrorThroughSum(method, measurands)` | `Plus` / `Minus` |
| `PropagateErrorThroughProduct(method, measurands)` | `Times` / `DividedBy` |
| `PropagateErrorThroughExponentiation(measurand, exponentNumerator, exponentDenominator)` | `ToPower` / `ToRoot` |

Each takes an `ErrorPropagationMethod`, defaulting to `Uncorrelated`:

| Method | Sum error | Product relative error |
| --- | --- | --- |
| `Uncorrelated` (default) | RSS: `sqrt(Σ absErrᵢ²)` | RSS: `sqrt(Σ relErrᵢ²)` |
| `Correlated` | Direct sum: `Σ absErrᵢ` | Direct sum: `Σ relErrᵢ` |

`Uncorrelated` is the standard assumption for independent errors; `Correlated` is for the rarer case where inputs are known to share an error source (e.g. two readings taken from the same miscalibrated instrument).

**`ConservativeGaussianPropagator`** is the only implementation today and covers the large majority of cases — this is what you get, and what you should assume, unless you have a specific reason to reach for something else:

- It always resolves the output to a symmetric `GaussianUncertainty`, built from each input's *conservative* error (`KmsAbsoluteError` / `RelativeError`, i.e. `Max(upper, lower)`). This means `AsymmetricUncertainty` does not survive arithmetic — asymmetry is only meaningful for values that are never combined via the operations above.
- Monte Carlo propagation that preserves asymmetry through arithmetic is deferred to Milestone 4.

**Why `IErrorPropagator` is an interface at all:** propagation strategy is a model-level decision, not a universal constant — a different context might call for Monte Carlo propagation, or a correlation model that knows two "independent" variables actually share a calibration source. `IErrorPropagator` is the intended seam for that. As it stands, `Measurand.ResolveErrorPropagator()` unconditionally returns `ConservativeGaussianPropagator.Instance` — there is no injection point wired up yet (no constructor parameter, no ambient/DI resolver). Treat the interface as reserved space for that future pluggability, not as something already configurable.

---

## Unit library

Units live in `Measurement/Units/`. Each unit class follows the `ReflectiveUnitList<T>` pattern:

```csharp
public class Force : ReflectiveUnitList<Force>
{
    private Force() { }
    public static readonly Force Units = new();

    public static readonly UnitOfMeasure Newton    = UnitFactory.Create("N", Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time), 1.0);
    public static readonly UnitOfMeasure PoundForce = UnitFactory.Create("lbf", 4.44822, Force.Newton);
}
```

`ReflectiveUnitList<T>` discovers all `public static UnitOfMeasure` fields on the subclass at runtime. This means `Lists.UnitTypes` picks up every unit class automatically — no manual registration. `Lists.UnitTypes` (and each individual `UnitList`) exposes queryable lookups rather than requiring a switch over every unit class: `Force.Units.ByName`/`.BySymbol`/`.All` on a specific list, or `Lists.UnitTypes.ByName`/`.ByDimensionality`/`.All` across every unit class in the assembly — the mechanism to reach for if you need to resolve a unit from a string (e.g. during deserialization) or find every unit sharing a `Dimensionality`.

**`UnitFactory` patterns:**

| Pattern | Use |
| --- | --- |
| `UnitFactory.Create("sym", dimensionality, kmsConversionFactor)` | Base/fundamental unit (kmsConversionFactor = 1 for SI base) |
| `UnitFactory.Create("sym", scale, baseUnit)` | Scaled variant of an existing unit |
| `UnitFactory.Create("sym", (unit, exp), (unit, exp), …)` | Composite derived unit |
| `UnitFactory.Create("sym", kmsConversionFactor, baseUnit, zeroOffset)` | Offset unit (`OffsetUnitOfMeasure`); temperature and gauge pressure |

**Metric prefixes:** `Measurement.Factories.Metric` builds a prefixed unit on the fly instead of requiring each unit class to hand-declare every scaled variant:

```csharp
var kilonewton = Metric.k(Force.Newton);              // or Metric.Kilo.Create(Force.Newton)
var microfarad = Metric.micro(ElectricCapacitance.Farad);
```

Named constants span the full SI range, `Yocto` (10⁻²⁴) to `Yotta` (10²⁴), each with a matching static helper method (`Metric.k`, `Metric.M`, `Metric.G`, `Metric.m`, `Metric.micro`, `Metric.n`, …). Watch for a naming collision: `Metric.M`/`Metric.Mega` is the SI prefix (10⁶), while `Metric.ThousandM`/`Metric.MInRomanNumerals` (10³) and `Metric.MM`/`Metric.MegaMega` (10⁶) instead follow the oilfield convention where Roman-numeral `M` = thousand and `MM` = million. `Mega` and `MegaMega` share the same numeric factor but are not interchangeable — pick whichever convention matches the domain you're modeling.

**Available unit classes (40+):** Acceleration, Angle, AngularMomentum, AngularVelocity, Area, Density, Dimensionless, DynamicViscosity, ElectricCapacitance, ElectricCharge, ElectricConductance, ElectricCurrent, ElectricInductance, ElectricPotential, ElectricResistance, Energy, Force, Frequency, HeatTransferCoefficient, Jerk, KinematicViscosity, Length, LuminousIntensity, MagneticFlux, MagneticFluxDensity, Mass, MassFlow, MolecularMass, Moles, MomentOfInertia, Momentum, Power, Pressure, SpecificEnergy, SpecificHeatCapacity, Speed, SurfaceTension, Temperature, ThermalConductivity, Time, Torque, Volume, VolumetricFlow.

Note: `Torque` has dimension `M·L²·Θ·t⁻²` (angle in numerator), distinct from `Energy` (`M·L²·t⁻²`). This is intentional — torque and energy are semantically different even though they are dimensionally equivalent in many systems.

---

## Exceptions

Defined in `Measurement/Exceptions/`:

| Exception | Thrown by | When |
| --- | --- | --- |
| `IncompatibleDimensionsException` | `Quantity`/`Measurand` `+`/`-`/`Plus`/`Minus`; `In(unit)`/`Measurand.In(unit)` | Dimensionalities don't match (or the target unit's dimensionality doesn't match the value's) |
| `NondiscreteDimensionalityException` | `Dimensionality` `/` (root); `Quantity`/`Measurand` `ToRoot` | A fundamental-dimension exponent isn't evenly divisible by the requested root |

`NegativeMagnitudeException` also exists in this namespace but nothing in the codebase throws it anymore — a leftover from the removed `Magnitude` type; safe to ignore, and a candidate for deletion.

---

## Scope boundaries

**What belongs here:** physical quantities, units, dimensionality algebra, uncertainty types, error propagation.

**What does NOT belong here:**

- Expression trees or variables that represent unknowns → `DimensionedExpression`
- Binary operators (equality, tolerance, inequality) → `DimensionedExpression`
- Serialization DTOs or mappers → `Calcusystem.Serialization`
- Evaluation engine, solver → future assemblies
