# Binary Operator Taxonomy

Every operator supplies one predicate, `IsSatisfiedGiven(lhs, rhs)` → `bool` — the **Condition** column below, and nothing else. Resolving the two sides is the base class's job: `IsSatisfied(overrides?, propagator?)` → `bool?` computes both operands and delegates, answering `null` when either does not resolve.

Interval notation: for a `Measurand` *v*, its uncertainty interval is
`[v.KmsValue − v.KmsLowerAbsoluteError, v.KmsValue + v.KmsUpperAbsoluteError]`.

Where the Commutative column says ✗, **`Lhs` is the value under test and `Rhs` is the bound**. That convention is what `IBinaryOperator.Subject` / `Criterion` report — see the [assembly README](../README.md).

---

## Equality

| Class | Symbol | Commutative | Condition |
| --- | --- | --- | --- |
| `EqualityOperator` | `==` | ✓ | `IEqualityEstimating.AreEqual(Lhs, Rhs)` — injected strategy. The only operator whose `SolvingRole` can be `Equation` or `Coherence` (see the DimensionedExpression README); every other operator on this page yields an interval rather than a point, so no value can be derived from it, and all of them are `Requirement`. |

---

## Tolerance operators

These ask "are two measurements compatible within their uncertainties?" rather than
asserting an ordering.

| Class | Symbol | Commutative | Condition |
| --- | --- | --- | --- |
| `MutuallyWithinToleranceOperator` | `≃` | ✓ | Each nominal value falls inside the other's interval: `Lhs ∈ Rhs.interval` AND `Rhs ∈ Lhs.interval` |
| `AnyToleranceOverlapOperator` | `≈` | ✓ | The two intervals overlap at all: `smaller.Upper > bigger.Lower` |
| `WhollyWithinToleranceOperator` | `[=}` | ✗ | Lhs interval is strictly inside Rhs interval: `Lhs.Lower > Rhs.Lower` AND `Lhs.Upper < Rhs.Upper` |
| `WithinBindingToleranceOperator` | `=}` | ✗ | Lhs nominal value is inside Rhs interval: `Rhs.Lower < Lhs.KmsValue < Rhs.Upper` |
| `PointAndUpperBoundWithinToleranceOperator` | `[≓}` | ✗ | Lhs nominal value and upper bound are both inside Rhs interval |
| `PointAndLowerBoundWithinToleranceOperator` | `[≒}` | ✗ | Lhs nominal value and lower bound are both inside Rhs interval |

---

## Inequality operators

These assert an ordering between two measurements, with three levels of strictness
for each direction. All are non-commutative (Lhs = value under test, Rhs = bound).

No `≤` / `≥` variants exist — floating-point equality is unreachable in practice.

### Less-than (`<`)

| Class | Symbol | Condition |
| --- | --- | --- |
| `DefinitelyLessThanOperator` | `<<` | `Lhs.Upper < Rhs.Lower` — entire intervals non-overlapping; Lhs is certainly less |
| `UpperBoundsLessThanOperator` | `<^` | `Lhs.Upper < Rhs.Upper` — ceiling of Lhs below ceiling of Rhs; intervals may overlap |
| `NominallyLessThanOperator` | `<~` | `Lhs.KmsValue < Rhs.KmsValue` — nominal values only; uncertainty ignored |

### Greater-than (`>`)

| Class | Symbol | Condition |
| --- | --- | --- |
| `DefinitelyGreaterThanOperator` | `>>` | `Lhs.Lower > Rhs.Upper` — entire intervals non-overlapping; Lhs is certainly greater |
| `LowerBoundsGreaterThanOperator` | `>v` | `Lhs.Lower > Rhs.Lower` — floor of Lhs above floor of Rhs; intervals may overlap |
| `NominallyGreaterThanOperator` | `>~` | `Lhs.KmsValue > Rhs.KmsValue` — nominal values only; uncertainty ignored |

---

## Choosing the right operator

```
Need exact equality?                 → EqualityOperator
Need compatibility within errors?    → MutuallyWithinTolerance / AnyToleranceOverlap
Need one interval contained by another?  → WhollyWithinTolerance / WithinBindingTolerance
Need one interval ≈ but anchored?    → PointAndUpperBound / PointAndLowerBound
Need strict ordering (no overlap)?   → DefinitelyLessThan / DefinitelyGreaterThan
Need ordering at worst-case bound?   → UpperBoundsLessThan / LowerBoundsGreaterThan
Need ordering of nominal values only? → NominallyLessThan / NominallyGreaterThan
```
