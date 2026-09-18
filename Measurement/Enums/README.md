# Calcusystem.Measurement.Enums

The closed sets this assembly names.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Landmark` | enum | Which statistic of a measurand a comparison reads: `Nominal`, `UpperBound`, `LowerBound`. |
| `ComparisonResult` | `[Flags]` enum | What comparing two landmarks produced. Single-bit: `Equal`, `LessThan`, `GreaterThan`, `Incomparable`. |
| `MustBe` | `[Flags]` enum | Which `ComparisonResult`s satisfy a rule. `MustBe.LessThan`, `MustBe.EqualTo`, … |
| `UncertaintyType` | enum | Which uncertainty a snapshot rebuilds into: `Symmetric`, `Asymmetric`. |
| `UncertaintyCorrelation` | enum | Whether two operands' uncertainties move together: `Uncorrelated`, `Correlated`. |

## Guarantees

- **`ComparisonResult` members occupy distinct single bits, and `MustBe` masks them.**
  Evaluation is `(result & mustBe) != 0`, so a union like `LessThanOrEqualTo` needs no special
  case and negation is complement.
- **`Incomparable` is zero, so it satisfies no mask** — including `MustBe.Comparable`. An
  unanswerable comparison is undetermined rather than failed.

## Surprises

- **`MustBe.Comparable` is not a tautology.** It accepts every determinate result, so under the
  three-valued seam it answers `true` when the two landmarks can be compared and `null` when
  they cannot — an assertion that both are well-defined quantities.
- **`MustBe.Impossible` is satisfied by nothing**, and is the enum's zero, so it is also what an
  uninitialised field reads as. `SimpleComparison` refuses it at construction.
- **`UncertaintyCorrelation` is not a propagation method.** It states a fact about the
  quantities — two readings off one instrument share its calibration uncertainty. The numerical
  method is `IUncertaintyPropagator`, and the two are passed together.

## What does not belong here

- Anything with behaviour. `MustBe` is evaluated by `ComparisonRule`, not here.
- Comparison itself → `Comparison/`

## Related

`Comparison/` (`MeasurandComparer`) · `Uncertainties/` · `Snapshots/` ·
`Calcusystem.DimensionedExpression.BinaryOperators` (`ComparisonRule`, which pairs two
`Landmark`s with a `MustBe`).
