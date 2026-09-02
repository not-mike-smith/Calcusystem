# Calcusystem.Analysis.Outcomes

What asking a question about a system produced: whether it is well-posed, what it computes to,
and how each of its relationships fared.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `FlatSystem` | sealed record | The system reduced to unknowns × equations, and what that says about solvability. |
| `Equation` | sealed record | One determining relationship, paired with the unknowns it touches. |
| `Calculation` | sealed record | Every value the system produced in one run, plus what it could not produce. |
| `RelationshipOutcome` | sealed record | The verdict on one relationship, and the values it was judged on. |

Nothing here is constructed directly. Both entry points are extension methods in `Extensions/`:

```csharp
using Calcusystem.Analysis.Extensions;

var flat = system.Flatten();
flat.DegreesOfFreedom;   // unknowns − equations that touch at least one unknown
flat.Determination;      // Underdetermined / ExactlyDetermined / Overdetermined

var calc = system.Calculate();
calc.ValueOf(force);     // the Measurand, or null if it did not resolve
calc.MissingValues;      // the unset variables holding the rest back
calc.Violations;         // requirements that failed against a criterion
```

## Guarantees

- **A calculation is pure in `(system, overrides)`.** Trial values can be probed without
  writing them into the model, and independent calculations parallelise.
- **Each node is computed exactly once per run.** A sub-expression shared by three parents
  costs one evaluation, not three. This is why `Calculate` is preferred over calling
  `ComputeIfFullyDescribed()` on individual nodes.
- **Every relationship gets exactly one `RelationshipOutcome`**, including ones that could
  not be judged.
- **A verdict is three-valued.** `IsSatisfied` is `bool?`; `null` means the comparison had
  no answer, and reaches a report as *undetermined* — never as a violation.
- **Outcomes are a record of one run.** They hold the values judged at the time and do not
  follow later assignments to the variables involved.
- **Completeness and correctness are separate questions.** `IsComplete` is about values
  only. A calculation in which a requirement failed is complete and has a finding.

## Surprises

- **A failed check is not one thing.** `IsViolation` is a relationship that failed *against
  a criterion* — a measurement outside its spec. `IsInconsistency` is one that failed with
  no criterion, meaning both sides were computed and they disagree. The first is a finding
  about the world, the second about the model.
- **An equation with no unknowns removes no degree of freedom.** It is still evaluated and
  still reported; it appears in `RedundantEquations`. Vacuity and determination are
  orthogonal.
- **A square system can still be ill-posed.** `DegreesOfFreedom` of zero is an aggregate.
  `UnknownsWithNoEquation` can be non-empty at the same time, paired with a redundancy
  elsewhere, which is why it is surfaced separately from the count.
- **Only equalities can determine anything.** Ordering and tolerance relationships confine a
  value to an interval, and no solver turns an interval into a point, so they never count
  toward `Equations`.

## What does not belong here

- The walk that produces these → `Extensions/`
- `Determination`, the enum → `Enums/`
- Anything that mutates the system. Every type here is a read of one moment.

## Related

`Extensions/` (`Flatten`, `Calculate`) · `Enums/` (`Determination`) ·
`Calcusystem.DimensionedExpression.Systems` (`ExpressionSystem`, the input to both).

See the [assembly README](../README.md) for degrees of freedom and what "well-posed" means here.

---

## Appendix: Result and Outcome

The two words are close enough in ordinary English to be used interchangeably, so this
codebase fixes them apart:

> **Result** — the immediate answer a computation returns, carrying nothing but the answer.
> `ComparisonResult` is the whole of it: `LessThan`, `Equal`, `GreaterThan`, `Incomparable`.
>
> **Outcome** — a recorded judgement, carrying the context it was judged on.
> `RelationshipOutcome` holds the verdict *and* the relationship and the two values, so a
> report can say why.

The test is whether the type could be re-derived from its inputs later. A `Result` could;
it is a pure function of its operands. An `Outcome` could not, because the values it judged
may since have changed — which is exactly why it keeps them.

This is also why the folder is `Outcomes/` and not `Results/`: all four types here are the
second kind.

*Report* was considered and rejected for the second sense: it is both a noun and a verb,
and every term in the pattern language is a noun or nothing.
