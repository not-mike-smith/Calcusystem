# Calcusystem.Measurement

The foundation layer of Calcusystem. Provides physical quantities with units, dimensions, and measurement uncertainty as first-class concerns. All other assemblies depend on this one; it has no Calcusystem dependencies of its own.

> **Using this assembly:** this README covers the invariants that span the whole layer. Each folder carries its own README describing the namespace it holds, which is what you read to *use* that namespace. `Primitives/` is the one to start with — `Quantity`, `Measurand`, `Dimensionality`, and `FundamentalDimension` are the types every other folder is expressed in terms of.

---

## The central invariant: KMS normalization

**All values are stored internally in kg-m-s (SI base units).** Units are only relevant at the boundary — when constructing a quantity from a user-supplied value, or when reading a value back out in a specific unit. All arithmetic, comparison, and uncertainty propagation operates on KMS values directly. This eliminates an entire class of conversion bugs.

```csharp
var force = Force.PoundForce.Quantity(1.0).WithoutUncertainty();  // user supplies lbf
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
| ISymmetricUncertainty | interface | IUncertainty | symmetric uncertainty; absolute = relative × \|v\| |
| SymmetricUncertainty | class | ISymmetricUncertainty | the same magnitude above and below, stored as a relative fraction or an absolute KMS value |
| AsymmetricUncertainty | class | IUncertainty | independent upper and lower magnitudes |
| Quantity | struct | | raw KMS value + Dimensionality; internal currency; no uncertainty |
| OffsetUnitOfMeasure | class | UnitOfMeasure | extends UnitOfMeasure with a fixed zero-point offset; see note below |
| UnitOfMeasure | class | | symbol + Dimensionality + KMS conversion factor (constructed via UnitFactory) |
| Dimensionality | struct | | maps FundamentalDimension → integer exponent; supports algebra |

**`Measurand`** operations, beyond the properties in [Uncertainty system](#uncertainty-system) below:

| Category | Members |
| --- | --- |
| Arithmetic | `Plus`/`Minus`/`Times`/`DividedBy` (throw `IncompatibleDimensionsException` on a `Plus`/`Minus` dimension mismatch); `TryAdd`/`TrySubtract` — dimension-tolerant like `Quantity.TryAdd`/`TrySubtract` below, returning a NaN-valued `Measurand` (with zero uncertainty) instead of throwing on mismatch; unary `-`; `Reciprocal()`; `ToPower(int)`/`ToRoot(int)` |
| Convert | `In(UnitOfMeasure)` (throws on dimension mismatch) / `TryIn(UnitOfMeasure)` (returns `NaN` on mismatch); `AbsoluteUncertainty(unit)` / `AbsoluteUncertaintyIn(unit)` / `TryAbsoluteUncertaintyIn(unit)` |
| Validity | `IsValid()` (NaN/finite only — see point/delta note below), `IsNaN()`, `IsInfinity()`/`IsPositiveInfinity()`/`IsNegativeInfinity()`, `IsFinite()`, `IsNormal()`/`IsSubnormal()`, `IsNegative()` |

**`Quantity`** is usable on its own for KMS math without uncertainty: `+`/`-`/unary `-`/`*`/`/` operators (`+`/`-` require matching `Dimensionality` and throw `IncompatibleDimensionsException` otherwise; `*`/`/` combine dimensions freely), `ToPower(int)`/`ToRoot(int)`, an explicit `(Quantity)someDouble` cast to a dimensionless quantity, and dimension-tolerant `TryAdd`/`TrySubtract` — these genuinely return a `NaN`-valued `Quantity` instead of throwing when dimensionalities differ, which is the behavior `Measurand`'s `Try*` methods above are meant to mirror.

**`OffsetUnitOfMeasure`** stores a fixed zero-point offset baked in at construction time — not a live ambient reading. It is used for two physical domains:

- **Temperature scales** (°C, °F) where 0 °C ≠ 0 K
- **Gauge pressure** (psig, barg) where the zero is nominal atmospheric pressure (101 325 Pa), not a measured ambient value. If the actual ambient pressure matters, the caller is responsible for the correction.

`OffsetUnitOfMeasure` also exposes a `DeltaUnit` property — the corresponding non-offset unit for expressing *changes* without re-adding the offset (e.g. `Δ°C` for a temperature difference).

**`Dimensionality`** is a `readonly struct` holding a `Dictionary<FundamentalDimension, int>`. Zero-exponent entries are automatically stripped. Algebra is supported directly:

```csharp
var velocity   = Dimensionality.Length / Dimensionality.Time;  // L·T⁻¹
var energy     = Dimensionality.Mass * Dimensionality.Length * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
var specEnergy = velocity * 2;    // L²·T⁻²  (exponent scaling)
var root       = energy / 2;      // L·T⁻¹   (integer root; throws NondiscreteDimensionalityException if exponents aren't divisible)
```

The nine fundamental dimensions and their symbols:

| Dimension | Symbol | | Dimension | Symbol |
| --- | --- | --- | --- | --- |
| `Mass` | `M` | | `Temperature` | `Θ` |
| `Length` | `L` | | `Angle` | `A` |
| `Time` | `T` | | `AmountOfMatter` | `N` |
| `ElectricCurrent` | `I` | | `Currency` | `C` |
| `LuminousIntensity` | `J` | | | |

Note `T` is time and `Θ` is temperature — the reverse of the convention some references use.

**Treat the symbols as a published identity, not a display detail.** `Calcusystem.Serialization` keys its stored form on them, so renaming one invalidates previously persisted data (repairing that is its job, not this assembly's). The set is deliberately unambiguous: every symbol differs from every other *even ignoring case*, so no downstream case conversion can collapse two dimensions into one.

**No point/delta distinction in the value type.** Earlier drafts had separate `Magnitude` (non-negative) and `Delta` (signed difference) classes; these were merged into the single `Measurand` above. Whether a value is a point quantity or a delta/difference is a *modeling* concern (how a variable is used in an expression or system), not a property of the value itself — the two-class split added complexity without enough payoff. Practical implications:

- `Measurand.IsValid()` only checks NaN/finite — it does **not** reject negative values. If a quantity must be non-negative, that's the caller's or the expression layer's responsibility.
- There is no automatic redirection through `OffsetUnitOfMeasure.DeltaUnit`. To represent a temperature *difference*, construct with the delta unit explicitly (`Temperature.DeltaFahrenheit`/`Temperature.DeltaCelsius`) — using the absolute unit (`Temperature.Fahrenheit`/`Temperature.Celsius`) for a difference will silently apply the zero-offset where it shouldn't.

---

## Uncertainty system

The uncertainty interval around a nominal KMS value `v` is `[v − LowerAbsoluteUncertainty(v), v + UpperAbsoluteUncertainty(v)]`.

**Relative or absolute storage.** Each uncertainty stores its magnitude as *either* a relative fraction *or* an absolute KMS value, distinguished by an internal `bool IsStoredAsAbs`. This is purely a **convention on what the stored magnitude means** — both encode the same interval, and converting between them is just multiplying or dividing by `|v|`. (A genuinely different model such as interval bounds is not another value of this flag; it would be a change in how uncertainties *propagate*, handled at the `IUncertaintyPropagator` level.) Which form is stored is invisible to consumers — you always read absolute or relative through `IUncertainty` — but it matters at zero: an **absolute magnitude is well-defined when the value is 0; a relative one is not**. `RelativeUncertainty(0)` returns `+∞` rather than throwing, which is what lets a sum that cancels to zero, or `ln(1)`, carry a meaningful uncertainty.

### Building one

The concrete constructors are private. `Uncertainty` is the whole construction vocabulary:

| Factory | Returns | Stored as |
| --- | --- | --- |
| `Uncertainty.Exact()` | `SymmetricUncertainty` | — (none) |
| `Uncertainty.Relative(RelativeUncertainty)` | `SymmetricUncertainty` | relative fraction |
| `Uncertainty.Absolute(Quantity)` | `SymmetricUncertainty` | absolute KMS magnitude |
| `Uncertainty.Relative(upper, lower)` | `AsymmetricUncertainty` | relative fractions |
| `Uncertainty.Absolute(upper, lower)` | `AsymmetricUncertainty` | absolute KMS magnitudes |

The storage flag and the raw magnitude are `internal`, so there is no `(bool, double)` overload to reach for. Rebuilding a *persisted* uncertainty is a separate concern with a separate door — `UncertaintyFactory.FromSnapshot`, see [Persistence](#persistence-snapshots-not-dtos).

**A relative uncertainty is a `RelativeUncertainty`, not a bare `double`.** A number on its own cannot say whether it means a fraction of a value or an amount of it: given a mass in kilograms, `0.001` reads equally well as one gram or as one tenth of a percent. Build one with `Percent()` or `Fraction()`:

```csharp
0.1.Percent()     // 0.001 — one tenth of one percent
0.1.Fraction()    // 0.1   — ten percent
```

### Attaching one to a value

`Quantity` carries shorthands for the common cases, so most code never names an uncertainty type at all:

```csharp
var exact    = Mass.Kilogram.Quantity(1).WithoutUncertainty();
var relative = Mass.Kilogram.Quantity(1).WithUncertainty(0.1.Percent());
var absolute = Mass.Kilogram.Quantity(1).WithUncertainty(1.0.Units(Mass.Gram));

var lopsided = Mass.Kilogram.Quantity(1).WithAsymmetricUncertainty(
    upper: 0.1.Percent(),
    lower: 2.0.Percent());
```

`WithUncertainty` is symmetric and `WithAsymmetricUncertainty` takes both bounds at once — there is no way to supply one bound and not the other, and no way to mix a relative bound with an absolute one, because no overload accepts that. **Pass the asymmetric arguments by name.** Which bound is which is otherwise invisible at the call site, and swapping them produces a plausible-looking interval rather than an obvious fault.

For anything else, `Quantity.Measurand(IUncertainty)` takes an uncertainty built from the table above.

```csharp
var mass = Mass.Kilogram.Quantity(1).WithUncertainty(1.0.Units(Mass.Milligram));
```

Propagation follows the storage: **sums and differences produce an absolute result** (no dividing by the possibly-zero sum), while products compose relative ones. A quantity whose interval crosses zero is left signed — clamping a non-negative "magnitude" at zero is a modeling concern for a higher layer, not baked in here.

**`ISymmetricUncertainty`** extends `IUncertainty` and adds default interface implementations of the directional members (`UpperAbsoluteUncertainty`/`LowerAbsoluteUncertainty` and their relative equivalents) in terms of the single `AbsoluteUncertainty`/`RelativeUncertainty`. Only `SymmetricUncertainty` implements this.

`Measurand` exposes:

- `KmsUpperAbsoluteUncertainty` / `KmsLowerAbsoluteUncertainty` — the directional magnitudes; use these in operators and checks
- `KmsAbsoluteUncertainty` — `Max(upper, lower)`; conservative single value for propagation formulas
- `RelativeUncertainty` — `KmsAbsoluteUncertainty / |KmsValue|`; conservative for propagation
- `Uncertainty` — the raw `IUncertainty` instance; preserved through negation and `Reciprocal()`

---

## Uncertainty propagation

`Measurand` arithmetic (`Plus`, `Minus`, `Times`, `DividedBy`) propagates uncertainty through an `IUncertaintyPropagator` (`Interfaces/IUncertaintyPropagator.cs`):

| Method | Used for |
| --- | --- |
| `PropagateThroughSum(correlation, measurands)` | `Plus` / `Minus` |
| `PropagateThroughProduct(correlation, measurands)` | `Times` / `DividedBy` |

`ToPower` and `ToRoot` do **not** go through the propagator. Raising one value to a power has no second operand to be correlated with, so the whole of it is `IUncertainty.Exponentiated(nominalKmsValue, numerator, denominator)`, alongside the other unary transforms.

Each propagator method takes an `UncertaintyCorrelation`, defaulting to `Uncorrelated`:

| Correlation | Sum, absolute | Product, relative |
| --- | --- | --- |
| `Uncorrelated` (default) | RSS: `sqrt(Σ uᵢ²)` | RSS: `sqrt(Σ uᵢ²)` |
| `Correlated` | Direct sum: `Σ uᵢ` | Direct sum: `Σ uᵢ` |

`Uncorrelated` is the standard assumption for independent measurements; `Correlated` is for the rarer case where inputs are known to share a source of uncertainty (e.g. two readings taken from the same miscalibrated instrument).

**`ConservativeGaussianPropagator`** is the only implementation today and covers the large majority of cases — this is what you get, and what you should assume, unless you have a specific reason to reach for something else:

- When every operand is symmetric it returns a `SymmetricUncertainty`; if any operand is asymmetric it preserves the asymmetry, returning an `AsymmetricUncertainty` built from the directional upper and lower magnitudes. (Unary transforms — negation, reciprocal, exponentiation — likewise preserve asymmetry; they live on `IUncertainty` rather than the propagator.)
- Full Monte Carlo propagation is still deferred to Milestone 4; the current propagator combines magnitudes by RSS or direct sum per the table above.

**Why `IUncertaintyPropagator` is an interface at all:** propagation strategy is a model-level decision, not a universal constant — a different context might call for Monte Carlo propagation, or a correlation model that knows two "independent" variables actually share a calibration source. `IUncertaintyPropagator` is the intended seam for that. As it stands, `Measurand.ResolveUncertaintyPropagator()` unconditionally returns `ConservativeGaussianPropagator.Instance` — there is no injection point wired up yet (no constructor parameter, no ambient/DI resolver). Treat the interface as reserved space for that future pluggability, not as something already configurable.

---

## Unit library

Units live in `Units/`. Each unit class follows the `ReflectiveUnitList<T>` pattern:

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

**The available unit classes are the files in `Units/`** — mechanical, electrical, thermal, and fluid quantities, plus `Dimensionless`. `Lists.UnitTypes.All` enumerates them at runtime, which is the answer that cannot go stale.

Note: `Torque` has dimension `M·L²·A·T⁻²` (angle in numerator), distinct from `Energy` (`M·L²·T⁻²`). This is intentional — torque and energy are semantically different even though they are dimensionally equivalent in many systems.

---

## Exceptions

Defined in `Exceptions/`:

| Exception | Thrown by | When |
| --- | --- | --- |
| `IncompatibleDimensionsException` | `Quantity`/`Measurand` `+`/`-`/`Plus`/`Minus`; `In(unit)`/`Measurand.In(unit)` | Dimensionalities don't match (or the target unit's dimensionality doesn't match the value's) |
| `NondiscreteDimensionalityException` | `Dimensionality` `/` (root); `Quantity`/`Measurand` `ToRoot` | A fundamental-dimension exponent isn't evenly divisible by the requested root |

---

## Persistence: snapshots, not DTOs

Measurement owns **what data defines a value**; it does not own **how that data is encoded, versioned, or migrated**. Those are different questions with different release cadences, and conflating them is what previously put serialization-only members on the public surface. The seam between them is a set of plain records in `Snapshots/`:

| Type | Snapshot | Contents |
| --- | --- | --- |
| `IUncertainty` | `UncertaintySnapshot` | symmetric or asymmetric, storage flag, magnitudes |
| `Quantity` | `QuantitySnapshot` | KMS value + `DimensionalitySnapshot` |
| `Measurand` | `MeasurandSnapshot` | `QuantitySnapshot` + `UncertaintySnapshot` |
| `Dimensionality` | `DimensionalitySnapshot` | exponent of each present fundamental dimension |

These carry no type discriminator, no schema version, and no encoding choices. A persistence layer maps them to whatever wire format it likes and owns any fix-up of older payloads — Measurement never sees a version number.

```csharp
var snapshot = measurand.GetSnapshot();          // hand to Calcusystem.Serialization
var restored = Measurand.FromSnapshot(snapshot); // rebuild
```

`Quantity`, `Measurand`, and `Dimensionality` implement `ISnapshotting<TSelf, TSnapshot>` (from `Calcusystem.Core`), which pairs an instance `GetSnapshot()` with a `static abstract FromSnapshot`.

**`IUncertainty` deliberately does not.** Its concrete type is chosen by *inspecting* the snapshot, so reconstruction cannot be a per-type `static abstract`; it is a static gateway over the closed set instead — `UncertaintyFactory.FromSnapshot(snapshot)`, mirroring how `DimensionedExpression` rebuilds provenance through `ProvenanceFactory`. `IUncertainty.GetSnapshot()` is implemented **explicitly** by both concrete types, so the storage form is reachable through the interface but stays off `SymmetricUncertainty`'s and `AsymmetricUncertainty`'s own public surfaces. `Quantity` and `Measurand` implement `GetSnapshot()` publicly — what defines them is value and dimension, both already public concepts, so there is nothing to protect.

**`DimensionalitySnapshot` carries the exponent pairs**, zero exponents stripped, so an empty map is a dimensionless value. It does *not* carry an encoded string: choosing to write those pairs as `"M1,L1,T-2"` versus a nested object, keying them on symbols versus names, and repairing a payload written before a symbol changed are all format decisions, and they live in `Calcusystem.Serialization` (see `DimensionalityCodec` there). This is also why the snapshot is not `ToString()`, which is a human-readable form (`M·L/T²`) with middots and superscripts that does not round-trip.

```csharp
var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
var pairs = force.GetSnapshot().Pairs;   // { Mass: 1, Length: 1, Time: -2 }, in canonical order
```

A map is affordable because a snapshot lives only for the duration of a serialization pass — it is not something the rest of the library computes with. `GetSnapshot()` yields its pairs in canonical dimension order, so a consumer writing them out gets a stable result for dimensionally-equal values without sorting them itself. `DimensionalitySnapshot` compares its maps set-wise rather than by reference; the compiler-generated equality would otherwise make two snapshots describing the same dimension unequal, and that would propagate into `QuantitySnapshot` and `MeasurandSnapshot`.

---

## Scope boundaries

**What belongs here:** physical quantities, units, dimensionality algebra, uncertainty types, uncertainty propagation, and the snapshots describing them.

**What does NOT belong here:**

- Expression trees or variables that represent unknowns → `DimensionedExpression`
- Binary operators (equality, tolerance, inequality) → `DimensionedExpression`
- Serialization DTOs or mappers → `Calcusystem.Serialization`
- Evaluation engine, solver → future assemblies

The snapshots in `Snapshots/` are not an exception to the third line. A snapshot says *what data defines a value*, which only this assembly can answer; a DTO adds *how that data is labeled, versioned, and encoded*, which is the persistence layer's business. Wire formats, type discriminators, and schema migrations stay out of here.
