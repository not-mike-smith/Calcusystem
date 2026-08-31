# Binary Operator Taxonomy

Under uncertainty a comparison does not have one answer — it has several nested ones, and the arithmetic that produces any of them produces all of them. So the thirteen operators are **fixed-tier queries over two shared evaluations**, not thirteen independent implementations.

Every operator supplies one predicate, `IsSatisfiedGiven(lhs, rhs)` → `bool`, and for ten of them that predicate is a single read off a ladder. Resolving the two sides is the base class's job: `IsSatisfied(overrides?, propagator?)` → `bool?` computes both operands and delegates, answering `null` when either does not resolve.

Interval notation: for a `Measurand` *v*, its uncertainty interval is
`[v.KmsValue − v.KmsLowerAbsoluteError, v.KmsValue + v.KmsUpperAbsoluteError]`.
Below, *a* is the Lhs nominal value and `aL`/`aU` its bounds; *b*, `bL`, `bU` likewise for Rhs.

Where the Commutative column says ✗, **`Lhs` is the value under test and `Rhs` is the bound**. That convention is what `IBinaryOperator.Subject` / `Criterion` report — see the [assembly README](../README.md).

---

## The two ladders

### `OrderingLadder` — is Lhs below Rhs?

A clean chain: each tier implies the one before it.

| Tier | Condition | Named operator |
| --- | --- | --- |
| `Possible` | `aL < bU` — some pair of points is ordered | *none* |
| `Nominal` | `a < b` — the reported values are | `NominallyLessThan` `<~` |
| `Certain` | `aU < bL` — every pair is | `DefinitelyLessThan` `<<` |

`Achieved` reports the strongest tier reached, and `Reaches(tier)` asks for at least one.

**The greater-than family is this ladder with the operands swapped** — `a > b` is asking about `(b, a)` — so one evaluator serves four operators.

`Possible` is the tier no named operator ever asked for, and the clearest illustration of what the collapse buys. A modeller who writes `<~` and gets `false` today cannot tell "comfortably the other way round" from "a hair's breadth away, and the uncertainty covers it".

### `ContainmentLadder` — is Lhs inside Rhs's band?

The same idea, but the middle rungs form a **lattice, not a chain**: a value's upper and lower bounds are independently checkable, so neither middle rung implies the other. There is deliberately no single ordered `Achieved` here — inventing one would force a precedence between "cannot overshoot" and "cannot undershoot", which are different engineering questions.

| Rung | Condition | Named operator |
| --- | --- | --- |
| `Overlaps` | `aU ≥ bL ∧ bU ≥ aL` — the values are not incompatible | `AnyToleranceOverlap` `≈` |
| `NominalWithin` | `bL ≤ a ≤ bU` | `WithinBindingTolerance` `=}` |
| `NominalAndUpperWithin` | …and `aU ≤ bU` | `PointAndUpperBoundWithinTolerance` `⌈=}` |
| `NominalAndLowerWithin` | …and `aL ≥ bL` | `PointAndLowerBoundWithinTolerance` `⌊=}` |
| `WhollyWithin` | `aL > bL ∧ aU < bU` | `WhollyWithinTolerance` `[=}` |

Implications run downward: `WhollyWithin` ⟹ both middle rungs ⟹ `NominalWithin` ⟹ `Overlaps`.

**The converse fails at the top, deliberately.** `WhollyWithin` is *strict* on both bounds while every rung below it is not, so two identical intervals satisfy every rung except the last — an interval is not *strictly* inside a copy of itself. This is the one exact-boundary case that really arises, since checking a value against a spec built from the same figures is ordinary, and it is pinned by a test.

`MutuallyWithinTolerance` `≃` is `NominalWithin` applied in **both directions** — a quantifier variation on the ladder rather than a rung of its own, which is why it has no unique arithmetic left. It is derivable (`a =} b` **and** `b =} a`) and could be dropped as a primitive; kept for now as vocabulary. Note it is `=}` doubled, **not** `[=}` — doubling `[=}` gives identical intervals, which is far stricter.

---

## Off the ladder

| Class | Symbol | Commutative | Condition |
| --- | --- | --- | --- |
| `EqualityOperator` | `==` | ✓ | `IEqualityEstimating.AreEqual(Lhs, Rhs)` — an injected strategy, so there is no fixed interval condition to tier. The only operator whose `SolvingRole` can be `Equation` or `Coherence`; every other operator on this page yields an interval rather than a point, so no value can be derived from it, and all of them are `Requirement`. |
| `UpperBoundsLessThanOperator` | `<^` | ✗ | `aU < bU` — ceiling against ceiling |
| `LowerBoundsGreaterThanOperator` | `>v` | ✗ | `aL > bL` — floor against floor |

The last two compare a derived **statistic** of each side rather than asking how the quantities stand to one another, so neither is a tier of either ladder and neither can be reached by strengthening or weakening one. They are also *not* mirror images of each other: one compares ceilings and the other floors, so neither is the other read with the operands swapped.

---

## The full thirteen

| Class | Symbol | Commutative | Reads |
| --- | --- | --- | --- |
| `EqualityOperator` | `==` | ✓ | injected strategy |
| `MutuallyWithinToleranceOperator` | `≃` | ✓ | `NominalWithin` both ways |
| `AnyToleranceOverlapOperator` | `≈` | ✓ | `Overlaps` |
| `WhollyWithinToleranceOperator` | `[=}` | ✗ | `WhollyWithin` |
| `WithinBindingToleranceOperator` | `=}` | ✗ | `NominalWithin` |
| `PointAndUpperBoundWithinToleranceOperator` | `⌈=}` | ✗ | `NominalAndUpperWithin` |
| `PointAndLowerBoundWithinToleranceOperator` | `⌊=}` | ✗ | `NominalAndLowerWithin` |
| `DefinitelyLessThanOperator` | `<<` | ✗ | ordering `Certain` |
| `NominallyLessThanOperator` | `<~` | ✗ | ordering `Nominal` |
| `DefinitelyGreaterThanOperator` | `>>` | ✗ | ordering `Certain`, swapped |
| `NominallyGreaterThanOperator` | `>~` | ✗ | ordering `Nominal`, swapped |
| `UpperBoundsLessThanOperator` | `<^` | ✗ | own comparison |
| `LowerBoundsGreaterThanOperator` | `>v` | ✗ | own comparison |

No `≤` / `≥` ordering variants exist — exact floating-point coincidence is unreachable in practice, so a non-strict ordering would differ from a strict one only on inputs that do not arise. Containment is the exception, and the reason is above: identical intervals *do* arise.

---

## Choosing the right operator

```
Need exact equality?                     → EqualityOperator
Need compatibility within errors?        → MutuallyWithinTolerance / AnyToleranceOverlap
Need one interval contained by another?  → WhollyWithinTolerance / WithinBindingTolerance
Need one interval ≈ but anchored?        → PointAndUpperBound / PointAndLowerBound
Need strict ordering (no overlap)?       → DefinitelyLessThan / DefinitelyGreaterThan
Need ordering at worst-case bound?       → UpperBoundsLessThan / LowerBoundsGreaterThan
Need ordering of nominal values only?    → NominallyLessThan / NominallyGreaterThan
```

The named operators are worth keeping as vocabulary: `AnyToleranceOverlap` says what it means far better than "the bottom rung of the containment ladder". What changed is that the arithmetic happens once, in one place, instead of thirteen times.
