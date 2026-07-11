# Measurement

The foundation layer of Calcusystem. Provides physical quantities with units, dimensions, and measurement uncertainty as first-class concerns. All other assemblies depend on this one; it has no Calcusystem dependencies of its own.

---

## The central invariant: KMS normalization

**All values are stored internally in kg-m-s (SI base units).** Units are only relevant at the boundary — when constructing a quantity from a user-supplied value, or when reading a value back out in a specific unit. All arithmetic, comparison, and uncertainty propagation operates on KMS values directly. This eliminates an entire class of conversion bugs.

```csharp
var force = new Magnitude(1.0, Force.PoundForce);  // user supplies lbf
force.In(Force.Newton);   // 4.448… — conversion happens at output only
force.KmsValue;           // 4.448… — internal representation is always SI
```

---

## Type hierarchy

Listed from user-facing at the top to foundational primitive at the bottom.

```
Magnitude               — concrete; non-negative (length, mass, absolute temperature, pressure…)
Delta                   — concrete; signed (temperature change, displacement, force component…)
PrecisionQuantity       — abstract; adds IUncertainty; static propagation methods (Sum, Product, Quotient)
PhysicalQuantity        — abstract; wraps Quantity; unit-aware In()/TryIn() and validity checks
Quantity                — struct; raw KMS value + Dimensionality; internal currency; no uncertainty

UnitOfMeasure           — symbol + Dimensionality + KMS conversion factor (constructed via UnitFactory)
OffsetUnitOfMeasure     — extends UnitOfMeasure with a fixed zero-point offset; see note below

Dimensionality          — struct; maps FundamentalDimension → integer exponent; supports algebra
```

**`OffsetUnitOfMeasure`** stores a fixed zero-point offset baked in at construction time — not a live ambient reading. It is used for two physical domains:

- **Temperature scales** (°C, °F) where 0 °C ≠ 0 K
- **Gauge pressure** (psig, barg) where the zero is nominal atmospheric pressure (101 325 Pa), not a measured ambient value. If the actual ambient pressure matters, the caller is responsible for the correction.

`OffsetUnitOfMeasure` also exposes a `DeltaUnit` property — the corresponding non-offset unit for expressing *changes* without re-adding the offset (e.g. `Δ°C` for a temperature difference).

**`Magnitude` vs `Delta`** is a semantic distinction, not just a sign check. A `Magnitude` represents a *size* — something that is physically non-negative. A `Delta` represents a *change* or *difference* — something that can be negative. Arithmetic preserves this:

- `Magnitude + Delta → Delta` (a length offset by a displacement is still a length, but arithmetic returns Delta to allow sign)
- `Magnitude - Magnitude → Delta` (the difference between two lengths can be negative)
- `Magnitude` is implicitly convertible to `Delta`; explicit cast the other way

**`Dimensionality`** is a `readonly struct` holding a `Dictionary<FundamentalDimension, int>`. Zero-exponent entries are automatically stripped. The nine fundamental dimensions are: `Mass`, `Length`, `Time`, `Temperature`, `ElectricCurrent`, `AmountOfMatter`, `LuminousIntensity`, `Angle`, `Currency`. Algebra is supported directly:

```csharp
var velocity = Dimensionality.Length / Dimensionality.Time;  // L·t⁻¹
var energy   = Dimensionality.Mass * Dimensionality.Length * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
var speed    = velocity * 2;    // L²·t⁻²  (exponent scaling)
var root     = energy / 2;      // L·t⁻¹   (integer root; throws NondiscreteDimensionalityException if exponents aren't divisible)
```

---

## Uncertainty system

Defined in `Measurement/Uncertainty/`. The uncertainty interval around a nominal KMS value `v` is `[v − LowerAbsoluteError(v), v + UpperAbsoluteError(v)]`.

| Type | Interface | Description |
| --- | --- | --- |
| `GaussianUncertainty(relativeError)` | `ISymmetricUncertainty` | Symmetric; absolute error = `relativeError × |v|` |
| `AsymmetricUncertainty(upperRel, lowerRel)` | `IUncertainty` | Independent upper/lower relative errors |
| `BoundedUncertainty(upperKms, lowerKms)` | `IUncertainty` | Independent upper/lower absolute KMS errors |

**`ISymmetricUncertainty`** extends `IUncertainty` and adds `AbsoluteError(v)`, with default interface implementations that satisfy `UpperAbsoluteError` and `LowerAbsoluteError`. Only `GaussianUncertainty` implements this.

`PrecisionQuantity` exposes:
- `KmsUpperAbsoluteError` / `KmsLowerAbsoluteError` — directional errors; use these in operators and checks
- `KmsAbsoluteError` — `Max(upper, lower)`; conservative single value for propagation formulas
- `RelativeError` — `KmsAbsoluteError / |KmsValue|`; conservative for propagation
- `Uncertainty` — the raw `IUncertainty` instance; preserved through negation and `Reciprocal()`

---

## Error propagation

`PrecisionQuantity` provides static factory methods for propagated results:

```csharp
// Sum: result is a Delta regardless of input types
Delta result = PrecisionQuantity.Sum(ErrorPropagationMethod.Uncorrelated, a, b, c);

// Product and Quotient
Delta product  = PrecisionQuantity.Product(ErrorPropagationMethod.Uncorrelated, a, b);
Delta quotient = PrecisionQuantity.Quotient(ErrorPropagationMethod.Uncorrelated, numerator, denominator);
```

`Magnitude` also exposes convenience instance methods (`Plus`, `Minus`, `Times`, `DividedBy`) that return the physically correct type (`Magnitude` or `Delta`).

**`ErrorPropagationMethod`:**
- `Uncorrelated` (default) — RSS: `σ_total = √(σ₁² + σ₂² + …)`. Assumes independent error sources.
- `Correlated` — direct sum: `σ_total = σ₁ + σ₂ + …`. Conservative worst-case; use when errors share a common source.

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

`ReflectiveUnitList<T>` discovers all `public static UnitOfMeasure` fields on the subclass at runtime. This means `Lists.UnitTypes` picks up every unit class automatically — no manual registration.

**`UnitFactory` patterns:**

| Pattern | Use |
| --- | --- |
| `UnitFactory.Create("sym", dimensionality, kmsConversionFactor)` | Base/fundamental unit (kmsConversionFactor = 1 for SI base) |
| `UnitFactory.Create("sym", scale, baseUnit)` | Scaled variant of an existing unit |
| `UnitFactory.Create("sym", (unit, exp), (unit, exp), …)` | Composite derived unit |
| `UnitFactory.Create("sym", kmsConversionFactor, baseUnit, zeroOffset)` | Offset unit (`OffsetUnitOfMeasure`); temperature and gauge pressure |

**Available unit classes (40+):** Acceleration, Angle, AngularMomentum, AngularVelocity, Area, Density, Dimensionless, DynamicViscosity, ElectricCapacitance, ElectricCharge, ElectricConductance, ElectricCurrent, ElectricInductance, ElectricPotential, ElectricResistance, Energy, Force, Frequency, HeatTransferCoefficient, Jerk, KinematicViscosity, Length, LuminousIntensity, MagneticFlux, MagneticFluxDensity, Mass, MassFlow, MolecularMass, Moles, MomentOfInertia, Momentum, Power, Pressure, SpecificEnergy, SpecificHeatCapacity, Speed, SurfaceTension, Temperature, ThermalConductivity, Time, Torque, Volume, VolumetricFlow.

Note: `Torque` has dimension `M·L²·Θ·t⁻²` (angle in numerator), distinct from `Energy` (`M·L²·t⁻²`). This is intentional — torque and energy are semantically different even though they are dimensionally equivalent in many systems.

---

## Deserialization

`Measurement.Factories.Deserializer` provides a public entry point for reconstructing `Magnitude` and `Delta` instances from raw KMS values and relative error values — the representation used in serialization DTOs. It delegates to internal `Deserialize` static methods on each concrete type.

> **Known placement issue:** This deserialization entry point lives in `Measurement` rather than in `Calcusystem.Serialization`. The reconstruction logic is co-located with the types it reconstructs, but the factory arguably belongs in the serialization assembly.

---

## Scope boundaries

**What belongs here:** physical quantities, units, dimensionality algebra, uncertainty types, error propagation.

**What does NOT belong here:**

- Expression trees or variables that represent unknowns → `DimensionedExpression`
- Binary operators (equality, tolerance, inequality) → `DimensionedExpression`
- Serialization DTOs or mappers → `Calcusystem.Serialization`
- Evaluation engine, solver → future assemblies
