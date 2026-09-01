# Binary Operator Taxonomy

The fourteen operators are **declarations**, not implementations: each one names the comparisons it asserts, and the comparing happens in exactly one place.

Every operator supplies `IReadOnlyList<ComparisonRule> Rules`. The base class ANDs them: `IsSatisfiedGiven(lhs, rhs)` → `bool?`. Resolving the two sides is also the base class's job — `IsSatisfied(overrides?, propagator?)` → `bool?` computes both operands and delegates, answering `null` when either does not resolve.

Interval notation: for a `Measurand` *v*, its uncertainty interval is
`[v.KmsValue − v.KmsLowerAbsoluteError, v.KmsValue + v.KmsUpperAbsoluteError]`.
Below, *a* is the Lhs nominal value and `aL`/`aU` its bounds; *b*, `bL`, `bU` likewise for Rhs.

Where the Commutative column says ✗, **`Lhs` is the value under test and `Rhs` is the bound**. That convention is what `IBinaryOperator.Subject` / `Criterion` report — see the [assembly README](../README.md).

---

## `ComparisonRule` — the atom

```csharp
readonly record struct ComparisonRule(Landmark Lhs, ComparisonType Type, Landmark Rhs)
```

One landmark of the subject against one landmark of the criterion, at a stated strictness. Nine landmark pairs × seven masks = 63 distinct rules, every one of which an operator may assert.

`Type` is a **mask of acceptable `ComparisonResult` outcomes**, not a relation. `ComparisonResult` is single-bit (`Equal` `0b100`, `LessThan` `0b010`, `GreaterThan` `0b001`), so evaluation is `(result & type) != 0`. `≤` is a union rather than a relation of its own, and negation is complement.

`Incomparable` is `0b000` and so satisfies **no** mask, including `Any`. A rule that cannot be answered returns `null`, never `false` — see *Three-valued verdicts* below.

`rule.Mirrored` swaps both landmarks and reverses the relation, so `rule.Mirrored` holds for `(a, b)` exactly when `rule` holds for `(b, a)`. Operators do **not** use it — each states its own rule outright — but `OrderingLadder.RuleFor` does, to derive one direction from the other.

### Notation

The symbol is **generated** from the rule, which is the test of whether the notation is systematic rather than decorative. The bar picks the statistic and the corner opens toward the operator:

| Landmark | Left glyph | Right glyph |
| --- | --- | --- |
| `UpperBound` | `⌜` | `⌝` |
| `Nominal` | `·` | `·` |
| `LowerBound` | `⌞` | `⌟` |

So `⌜<⌟` reads "my ceiling is below your floor". The six ordering operators declare their symbols by hand and a test asserts the generated ones match, so the alphabet cannot drift from the operators it describes.

Compound operators keep hand-written symbols: `·=}` is a *band*, not a comparison, and spelling it as its two rules would lose what the notation exists to convey.

---

## Three-valued verdicts

`IsSatisfiedGiven` returns `bool?`. `null` means the comparison has no answer — different dimensions, a `NaN`, or two same-signed infinities — and is distinct from `false`, which means the relationship was evaluated and does not hold. A calculation reports it as **undetermined**, alongside relationships whose operands never resolved.

This matters more than it looks. An engineer told a requirement *failed* goes looking for a design problem; what is actually wrong is that the check could not be run.

Conjunction is Kleene, and `false` beats `null`: one rule definitively violated settles the operator whatever the others could not answer.

`ExpressionSystem.Add(IBinaryOperator)` throws `IncompatibleDimensionsException` rather than letting a cross-dimensional relationship into a model at all. Evaluation stays defensive anyway, for models assembled elsewhere.

---

## The two ladders

Both are **classifiers, not evaluators**. Neither computes anything until asked, and operators do not go through either: every operator declares its own rules, and a ladder places them afterwards. They used to be record structs that evaluated every rung up front — the ordering one did three comparisons, containment did ten — and nothing outside the tests ever called them.

### `OrderingLadder` — how strongly is Lhs ordered against Rhs?

A rung is a **direction and a strength**, both named. Direction used to be a convention — everything was a less-than unless a declaration said `.Mirrored` — which meant you had to already know the convention to read an operator.

| Tier | Condition (`Below`) | Symbol | Named operator |
| --- | --- | --- | --- |
| `Possible` | `aL < bU` — some pair of points is ordered | `⌞<⌝` | *none* |
| `Nominal` | `a < b` — the reported values are | `·<·` | `NominallyLessThan` |
| `Certain` | `aU < bL` — every pair is | `⌜<⌟` | `DefinitelyLessThan` |

A clean chain: `Certain` ⟹ `Nominal` ⟹ `Possible`, each following from a nominal value lying inside its own interval. `Contradicted` is what is left when `Possible` fails — **a result, not a rung**, so no rule tests it and `RuleFor` rejects it.

```csharp
ComparisonRule RuleFor(OrderingDirection, OrderingConfidence)   // the rule that tests a rung
OrderingRung?  RungOf(this ComparisonRule)                      // which rung a rule is, if any
OrderingConfidence? AchievedTier(lhs, rhs, OrderingDirection)   // strongest tier reached, on demand
bool? Reaches(lhs, rhs, OrderingRung)                           // one rung, one comparison
```

`RuleFor` defines the `Below` rules and derives `Above` by mirroring, so mirroring still happens once — but where a direction was explicitly asked for, rather than silently inside an operator. `AchievedTier` reads top-down and stops as soon as it knows, so a certainly-ordered pair costs one comparison rather than three, and an unanswered rung ends the walk.

`RungOf` returning `null` is the honest answer for the comparisons genuinely off the ladder, and is what now stops a rung and the operator named after it drifting apart — a stronger check than asserting an operator was handed the ladder's own constant.

`Possible` is the tier no named operator ever asked for, and the reason the ladder is worth keeping at all. A modeller who writes `·<·` and gets `false` cannot otherwise tell "comfortably the other way round" from "a hair's breadth away, and the uncertainty covers it".

### `ContainmentLadder` — how far inside Rhs's band does Lhs sit?

A rung is a **set** of comparisons rather than one, so discovery works on rule sets.

| Rung | Condition | Named operator |
| --- | --- | --- |
| `Overlaps` | `aU ≥ bL ∧ aL ≤ bU` — the values are not incompatible | `AnyToleranceOverlap` `≈` |
| `NominalWithin` | `bL ≤ a ≤ bU` | `WithinBindingTolerance` `·=}` |
| `NominalAndUpperWithin` | …and `aU ≤ bU` | `PointAndUpperBoundWithinTolerance` `·⌜=}` |
| `NominalAndLowerWithin` | …and `aL ≥ bL` | `PointAndLowerBoundWithinTolerance` `·⌞=}` |
| `WhollyWithin` | `aL > bL ∧ aU < bU` | `WhollyWithinTolerance` `[=}` |

```csharp
IReadOnlyList<ComparisonRule> RulesFor(ContainmentRung)          // the rules that test a rung
ContainmentRung? RungOf(IReadOnlyList<ComparisonRule>)           // which rung a rule set is, if any
bool? Reaches(lhs, rhs, ContainmentRung)                         // one rung, on demand
```

`Reaches` evaluates only the rung asked for. `RungOf` matches on the rules themselves rather than on what they mean, which is enough to keep an operator and its rung from drifting — semantic matching is possible and considerably more machinery, and nothing needs it yet. `MutuallyWithinTolerance` correctly comes back `null`: it is nominal containment asked in **both** directions, a quantifier over the ladder rather than a rung of it.

The middle rungs form a **lattice, not a chain**: a value's upper and lower bounds are independently checkable, so neither middle rung implies the other. There is deliberately no "achieved rung" here as there is for ordering — inventing one would force a precedence between "cannot overshoot" and "cannot undershoot", which are different engineering questions.

Implications run downward: `WhollyWithin` ⟹ both middle rungs ⟹ `NominalWithin` ⟹ `Overlaps`.

**The converse fails at the top, deliberately.** `WhollyWithin` is *strict* on both bounds while every rung below it is not, so two identical intervals satisfy every rung except the last — an interval is not *strictly* inside a copy of itself. The dot-prefixed operators place a **point** in a *closed* band; `[=}` places an **interval** inside an *open* one, which is a different claim and why it alone opens with a bracket. This is the one exact-boundary case that really arises, since checking a value against a spec built from the same figures is ordinary, and it is pinned by a test.

`MutuallyWithinTolerance` `≃` is `NominalWithin` in **both directions** — the rung's rules plus their mirrors. Note it is `·=}` doubled, **not** `[=}` doubled, which would give identical intervals and be far stricter.

This ladder is also where a *single* rung genuinely drops out on its own: two unbounded uncertainties leave ceiling-against-ceiling undecidable while the reported values still answer.

---

## Off the ladder

| Class | Symbol | Commutative | Condition |
| --- | --- | --- | --- |
| `UpperBoundsLessThanOperator` | `⌜<⌝` | ✗ | `aU < bU` — ceiling against ceiling |
| `LowerBoundsGreaterThanOperator` | `⌞>⌟` | ✗ | `aL > bL` — floor against floor |

These compare a derived **statistic** of each side rather than asking how the quantities stand to one another, so neither is a tier of either ladder and neither can be reached by strengthening or weakening one. They are also *not* mirror images of each other: one compares ceilings and the other floors.

---

## Equality

`EqualityOperator` takes an `AgreementRule` saying how strictly "equal" is read. It is the only operator whose `SolvingRole` can be `Equation` or `Coherence` — every other operator here yields an interval rather than a point, so no value can be derived from one, and all of them are `Requirement`.

| `AgreementRule` | Symbol | Rules |
| --- | --- | --- |
| `Nominal` | `==` | `a = b` — the reported values are the same number |
| `Mutual` | `≃=` | `NominalWithin` both ways |
| `Overlapping` | `≈=` | `Overlaps` |

The trailing `=` marks the equality family; the leading glyph names how loosely agreement is read. The looser two coincide with `≃` and `≈`, deliberately: those state the condition as a requirement, while an equality can carry a solver's weight.

**The rule is state, not a strategy.** Equality previously took an injected `IEqualityEstimating`, so the wire carried "this is an equality" and nothing about what equality *meant* — the reader supplied the semantics, and two readers could reach opposite verdicts from identical bytes. A strategy cannot be serialized; an enum can. Reconstruction refuses an equality whose state names no rule rather than guessing one.

---

## The general form

`SimpleComparison` asserts one `ComparisonRule` chosen at construction, so it spells any of the 63 rules with three bytes of state. `·<⌟` — "my reported value must stay below your guaranteed floor" — is an ordinary conservative acceptance criterion with no named operator.

It **deliberately overlaps** the named types: configured with `·<·` it is `NominallyLessThan` in every respect including its symbol. That is an identity, not a collision — the two assert the same rule — which is why the symbol-uniqueness test excepts it. The named types stay because they are the ergonomic spelling and because the wire identifies operators by kind.

---

## The full fourteen

| Class | Symbol | Commutative | Asserts |
| --- | --- | --- | --- |
| `EqualityOperator` | `==` / `≃=` / `≈=` | ✓ | its `AgreementRule` |
| `MutuallyWithinToleranceOperator` | `≃` | ✓ | `NominalWithin` both ways |
| `AnyToleranceOverlapOperator` | `≈` | ✓ | `Overlaps` |
| `WhollyWithinToleranceOperator` | `[=}` | ✗ | `WhollyWithin` |
| `WithinBindingToleranceOperator` | `·=}` | ✗ | `NominalWithin` |
| `PointAndUpperBoundWithinToleranceOperator` | `·⌜=}` | ✗ | `NominalAndUpperWithin` |
| `PointAndLowerBoundWithinToleranceOperator` | `·⌞=}` | ✗ | `NominalAndLowerWithin` |
| `DefinitelyLessThanOperator` | `⌜<⌟` | ✗ | `aU < bL` — ladder `Below`/`Certain` |
| `NominallyLessThanOperator` | `·<·` | ✗ | `a < b` — ladder `Below`/`Nominal` |
| `DefinitelyGreaterThanOperator` | `⌞>⌝` | ✗ | `aL > bU` — ladder `Above`/`Certain` |
| `NominallyGreaterThanOperator` | `·>·` | ✗ | `a > b` — ladder `Above`/`Nominal` |
| `UpperBoundsLessThanOperator` | `⌜<⌝` | ✗ | its own rule |
| `LowerBoundsGreaterThanOperator` | `⌞>⌟` | ✗ | its own rule |
| `SimpleComparison` | *generated* | ✗ | any one rule |

No `≤` / `≥` variants of the *ordering* tiers exist. Comparison is tolerance-aware — `MeasurandComparer` calls two values equal when they differ by less than the measurements can resolve — so a non-strict ordering would differ from a strict one only on values already judged the same. Containment is the exception, and the reason is above: identical intervals really do arise.

---

## Choosing the right operator

```
Two quantities should be the same?        → EqualityOperator, with the AgreementRule you mean
Compatible within their errors?           → MutuallyWithinTolerance / AnyToleranceOverlap
One interval contained by another?        → WhollyWithinTolerance / WithinBindingTolerance
In range, and can't drift out of it?      → PointAndUpperBound / PointAndLowerBound
Strict ordering, no overlap?              → DefinitelyLessThan / DefinitelyGreaterThan
Ordering at a worst-case bound?           → UpperBoundsLessThan / LowerBoundsGreaterThan
Ordering of reported values only?         → NominallyLessThan / NominallyGreaterThan
Anything else, including across bounds?   → SimpleComparison
```

The named operators are worth keeping as vocabulary: `AnyToleranceOverlap` says what it means far better than "the bottom rung of the containment ladder". What changed is that each one now *declares* its condition instead of writing it, and no operator decides what "less than" means.
