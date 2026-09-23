# Calcusystem.DimensionedExpression.BinaryOperators

Relationships between two expressions: what each asserts, and the single atom all fourteen are
built from.

## What's here

| Type | Kind | Symbol | Role |
| --- | --- | --- | --- |
| `ComparisonRule` | readonly record struct | | One landmark against another, at one strictness. The atom. |
| `EqualityOperator` | class | `·==·` `{·==·}` `{>=<}` | Values agree, to the strictness its `AgreementRule` names. |
| `AnyToleranceOverlapOperator` | class | `{><}` | The intervals overlap at all. |
| `MutuallyWithinToleranceOperator` | class | `{·=·}` | Each value lies within the other's band. |
| `WhollyWithinToleranceOperator` | class | `[=}` | One interval lies strictly inside the other. |
| `WithinBindingToleranceOperator` | class | `·=}` | The value lies within the criterion's band. |
| `PointAndUpperBoundWithinToleranceOperator` | class | `·⌜=}` | …and cannot overshoot it. |
| `PointAndLowerBoundWithinToleranceOperator` | class | `·⌞=}` | …and cannot undershoot it. |
| Six ordering operators | classes | `⌜<⌟` `⌜<⌝` `·<·` `⌞>⌝` `⌞>⌟` `·>·` | In `InequalityOperators.cs`. |
| `SimpleComparison` | class | generated | The general form: any one rule. |
| `OrderingLadder` | static class | | Classifies a rule as a direction and a confidence. |
| `ContainmentLadder` | static class | | Classifies a rule set as a containment rung. |
| `BinaryOperatorBase` | abstract class | | ANDs the rules; `CommutativeOperatorBase` and `NonCommutativeOperatorBase` extend it. |
| `BinaryOperatorFactory` | static class | | Rebuilds an operator from its snapshot. |

## Start here

```csharp
using Calcusystem.DimensionedExpression.BinaryOperators;

// a named operator. Id, Lhs and Rhs are required on every one of them
var check = new WithinBindingToleranceOperator { Id = "T-101 in spec", Lhs = measured, Rhs = spec };

// an equality, which must state how strictly it reads "equal" and what it does for the solver
var eq = new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
{
    Id = "mass balance", Lhs = massIn, Rhs = massOut,
};

// anything else: one rule, spelled out
var conservative = new SimpleComparison(
    new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.LowerBound))
{
    Id = "below guaranteed floor", Lhs = reported, Rhs = guaranteed,
};

check.IsSatisfiedGiven(lhsValue, rhsValue);   // bool? — null means no answer
check.Symbol;                                  // ·=}
```

## Guarantees

- **Operators declare; they do not compare.** Every one supplies an
  `IReadOnlyList<ComparisonRule>` and the base ANDs them. `MeasurandComparer` is the only place
  a numeric comparison happens, so no operator can disagree with another about what "less
  than" means.
- **Conjunction is Kleene, and `false` beats `null`.** One rule definitively violated settles
  the whole relationship, whatever the others could not answer.
- **`rule.Mirrored` holds for `(a, b)` exactly when `rule` holds for `(b, a)`.** It swaps both
  landmarks and reverses the relation, which is valid because `MeasurandComparer` is
  antisymmetric.
- **A symbol is a mirror-palindrome if and only if the operator is commutative.** Reverse the
  characters *and* map each to its mirror; the result equals the original exactly for the
  commutative ones. This is asserted by a test, not by convention.
- **Both ladders are classifiers.** They compute nothing until asked: `RuleFor`, `RulesFor`,
  `RungOf`, `AchievedTier`, `Reaches`. No operator routes through one.

## Surprises

- **`SimpleComparison` deliberately collides with the named types' symbols.** Configured with
  the nominal-against-nominal rule it *is* `NominallyLessThanOperator` in every respect — an
  identity, not a collision — which is why the symbol-uniqueness test excepts it.
- **`MustBe.Impossible` is refused at construction; `MustBe.Comparable` is not.** A rule
  satisfied by nothing reports as a violation on every calculation, which is a finding the
  model never asserted. `Comparable` looks like the same mistake but is not vacuous under the
  three-valued seam — it asserts both landmarks are well-defined quantities.
- **Compound symbols are not generated.** `·=}` is a band marker, not a spelled-out
  conjunction; rendering it as its two rules would lose what the notation conveys. Only
  `SimpleComparison` generates its symbol end to end.
- **Only `EqualityOperator` can be anything but a `Requirement`.** An ordering or tolerance
  relation confines a value to an interval, and no solver turns an interval into a point.
- **`ContainmentRung` is a lattice.** `WhollyWithin` is strict on both bounds while every other
  rung is not, so two identical intervals satisfy every rung except the last.

## What does not belong here

- The comparison itself → `Calcusystem.Measurement.Comparison`
- `MustBe`, `Landmark`, `ComparisonResult` → `Calcusystem.Measurement.Enums`
- `SolvingRole`, `AgreementRule`, the rung enums → `Enums/`

## Related

[`OPERATORS.md`](OPERATORS.md) — the full taxonomy, every symbol, and the exact interval
condition each operator tests.
`Interfaces/` · `Enums/` · `Snapshots/` · `Calcusystem.Measurement.Comparison`.

---

## Appendix: two shapes that look like oversights

Both of these were argued in doc comments, where a reader hovering over a member does not need
them. They are recorded here instead.

### A `ComparisonRule` is not an `IBinaryOperator`, deliberately

A rule carries no identity, no operands and no provenance — an operator holds rules, a rule
holds nothing. Composing operators out of child operators would duplicate all three and force
the wire format to carry them twice, for a structure that is never shared and never referenced.

It follows that a rule is a pure value: two rules spelling the same comparison are equal, and
one can be declared inline without minting anything.

### `Rules` is public

It would work as a private detail — the base class is the only thing that evaluates it. It is
public because it is the operator's own account of what it checks, and **a report naming
*which* comparison failed needs the terms, not just the verdict.** An operator that hid its
rules could report that a tolerance check failed but not that the subject's ceiling was the
side that fell outside.
