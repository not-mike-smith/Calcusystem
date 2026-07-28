# DimensionedExpression

The expression layer of Calcusystem. Builds trees of dimensioned variables and formulas — a system of equations whose leaves are measured/known values and whose interior nodes compute derived values with uncertainty propagation. Constraints and definitions are expressed as binary operators over those expressions.

Depends only on `Measurement` (for `Measurand`, `Dimensionality`, `ErrorPropagationMethod`). It has no dependency on serialization, evaluation, or solving.

---

## The central idea: lazy, dimension-checked expression trees

Every node in the tree is an `IExpression`. A node knows its `Dimensionality` *structurally* — always available, even before any values are supplied — but produces a `Measurand? Value` **only once every leaf it depends on has been given a value**. Until then `Value` is `null` and `IsFullyDescribed` is `false`.

```csharp
var mass  = new Variable("m", Dimensionality.Mass);
var accel = new Variable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

var force = new ProductExpression();
force.AddFactor(mass);
force.AddFactor(accel);

force.Dimensionality;      // M·L·t⁻²  — known immediately
force.IsFullyDescribed;    // false
force.Value;               // null
force.DegreesOfFreedom();  // 2  — two unbound leaves

mass.Value  = Mass.Kilogram.Quantity(2).Measurand(GaussianUncertainty.FromRelErr(0.01));
accel.Value = /* … */;

force.IsFullyDescribed;    // true once both are set
force.Value;               // a Measurand (value + propagated uncertainty), computed on demand
```

`Value` is recomputed on each access from the current children — there is no caching and no separate "evaluate" step. Arithmetic and uncertainty propagation are delegated entirely to `Measurand` (see the Measurement README); this layer only assembles the tree and walks it.

Two consequences worth internalizing:

- **Dimensionality is total; Value is partial.** Ask for `Dimensionality` any time. Only dereference `Value` after checking `IsFullyDescribed` (or guard for `null`) — the `Value!` null-forgiving usage inside the library is always gated by that check.
- **`DegreesOfFreedom()` counts unbound leaves.** A fully-described tree has 0. This is the intended gate for the future evaluation/solver work (DoF 0 → evaluate, 1 → solvable, >1 → underdetermined).

---

## Type hierarchy

### Expression interfaces (`Interfaces/IExpression.cs`)

| Interface | Extends | Adds |
| --- | --- | --- |
| `IExpression` | | `Id`, `IsDirectlyMutable`, `IsFullyDescribed`, `Dimensionality`, `Measurand? Value` (get), `DegreesOfFreedom()` |
| `IDirectExpression` | `IExpression` | re-declares `Value` with a **setter** — a mutable leaf |
| `IComputedExpression` | `IExpression` | `ErrorPropagation { get; set; }` — the `ErrorPropagationMethod` used when combining children |

### Expression node types (`Expressions/`)

| Class | Implements | Role |
| --- | --- | --- |
| `Variable` | `IDirectExpression` | Leaf. Holds a settable `Measurand? Value`; setting a value of the wrong dimensionality throws `IncompatibleDimensionsException`. `DegreesOfFreedom()` = 0 if set, else 1. Constructible with just a dimensionality (unbound) or with an initial `Measurand`. |
| `SumExpression` | `IComputedExpression` | n-ary `+` over `Addends`. All addends must share a dimensionality (enforced on `AddAddend`); constructor can seed a fixed dimensionality for an empty sum. |
| `ProductExpression` | `IComputedExpression` | n-ary `×` over `Factors`. Dimensionality is the product of its factors'. |
| `QuotientExpression` | `IComputedExpression` | `Numerator / Denominator` (both `required`). |
| `NegatedExpression` | `IExpression` | Unary negation wrapper over any `IExpression` (its `Operand`). Not directly mutable. |
| `ReciprocalExpression` | `IExpression` | Unary `1/x` wrapper over any `IExpression`; reciprocates the dimensionality. |

Composite nodes (`Sum`/`Product`/`Quotient`) derive from `ComputedExpressionBase` (which supplies `Id`, `IsDirectlyMutable => false`, and the `ErrorPropagation` property); each still implements `Value`/`Dimensionality`/`IsFullyDescribed`/`DegreesOfFreedom` itself. `DegreesOfFreedom()` on a composite is the sum of its children's.

### Binary operators (`BinaryOperators/`)

All operators implement `IBinaryOperator` (`Lhs`/`Rhs` expressions, `IsCommutative`, `bool? IsSatisfied()`, `AreBothSidesFullyDescribed`) via `BinaryOperatorBase` and its `CommutativeOperatorBase` / `NonCommutativeOperatorBase` splits. **`IsSatisfied()` returns `null` when either side is not fully described** — a three-valued result (`true` / `false` / `unknown`), not a bare bool.

There are three families — equality, tolerance (compatibility within uncertainty), and inequality (ordering, three strictness levels per direction). **The full taxonomy — every class, its symbol, commutativity, and exact interval condition — lives in [`BinaryOperators/OPERATORS.md`](BinaryOperators/OPERATORS.md).** Read that rather than the individual operator files.

One construction wrinkle: **`EqualityOperator` is the only operator with a dependency** — it takes an `IEqualityEstimating` (the strategy deciding when two `Measurand`s count as equal) as a constructor argument. Every other operator is constructed purely through `required` init properties:

```csharp
var op = new WhollyWithinToleranceOperator { Id = Constants.CREATE_NEW, Lhs = measured, Rhs = spec };
var eq = new EqualityOperator(estimator)   { Id = Constants.CREATE_NEW, Lhs = a,        Rhs = b   };
```

---

## Identity: `IdBase` and `Constants.CREATE_NEW`

Every expression, operator, and system carries a string `Id` via `IdBase`. Passing the sentinel `Constants.CREATE_NEW` (the default on most constructors) generates a fresh GUID; passing an explicit id preserves it (this is what deserialization relies on to rebuild references). A null/whitespace id throws.

---

## `ExpressionSystem` (`Systems/ExpressionSystem.cs`)

The container for one coherent model. Create it via the factory (auto-generated id):

```csharp
var system = ExpressionSystem.Create("Newton's second law", "F = m·a");
```

It holds four lists plus a `Name`/`Description`:

| Member | Type | Purpose |
| --- | --- | --- |
| `DirectExpressions` | `List<Variable>` | the mutable leaf variables |
| `DerivedExpressions` | `List<IExpression>` | computed nodes built over those leaves |
| `Definitions` | `List<IBinaryOperator>` | always-true relationships used to *compute* unknowns (conservation laws, constitutive equations) |
| `Constraints` | `List<IBinaryOperator>` | tolerance/ordering checks evaluated against values (pass / fail / unknown) |

`GetAllExpressions()` returns direct + derived. The scope of one `ExpressionSystem` is a single model (one equation of state, one heat exchanger); composing multiple systems into a flowsheet is a future (Milestone 5) concern.

The `Definitions` vs. `Constraints` distinction is semantic, not enforced by type — both are `List<IBinaryOperator>`. The convention: if an operator exists to derive a value, it's a definition; if it exists to check one, it's a constraint.

---

## Serialization

There is **none in this assembly** — no DTOs, no mappers, no persistence. Serialization lives in `Calcusystem.Serialization`, which references this project and maps these types to/from DTOs (using explicit `Id`s to rebuild the reference graph, and injecting the `IEqualityEstimating` needed by `EqualityOperator`). If you are round-tripping an `ExpressionSystem`, that is the assembly to reach for.

---

## Scope boundaries

**What belongs here:** the `IExpression` tree and its node types, binary operators, `ExpressionSystem`, and the interfaces above.

**What does NOT belong here:**

- Physical quantities, units, dimensional algebra, uncertainty types, error propagation math → `Measurement`
- Serialization DTOs and mappers → `Calcusystem.Serialization`
- The actual evaluation walk, constraint reporting, and solving → future assemblies (this layer provides `Value`, `IsFullyDescribed`, and `DegreesOfFreedom` as the primitives they will build on, but performs no orchestration itself)
