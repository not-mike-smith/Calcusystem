# Calcusystem.DimensionedExpression.Enums

The closed sets this layer names: what a relationship does, how strictly it reads equality,
where a comparison sits on a ladder, and which type a snapshot rebuilds into.

## What's here

**Modelling choices** — a modeller picks these, and they change what the model asserts.

| Type | Role |
| --- | --- |
| `SolvingRole` | What a relationship does to the problem: `Requirement`, `Equation`, `Coherence`. |
| `AgreementRule` | How strictly an equality reads "equal": `Nominal`, `Mutual`, `Overlapping`. |

**Ladder positions** — names for where a comparison sits, used to classify rather than to configure.

| Type | Role |
| --- | --- |
| `OrderingDirection` | Which way an ordering claim runs: `Below`, `Above`. |
| `OrderingConfidence` | How strongly it holds: `Contradicted`, `Possible`, `Nominal`, `Certain`. |
| `ContainmentRung` | How much of a subject lies inside a band: `Overlaps` … `WhollyWithin`. |

**Reconstruction discriminators** — which concrete type a snapshot rebuilds into. Not a modelling choice.

| Type | Role |
| --- | --- |
| `BinaryOperatorType` | Which of the fourteen operators. |
| `UnaryExpressionType` | `Reciprocal`, `Negated`, `Sqrt`, `Exponential`, `NaturalLog`. |
| `BinaryExpressionType` | `Quotient`. |
| `NaryExpressionType` | `Product`, `Sum`. |
| `ProvenanceType` | `Measured`, `Reference`, `Design`, `Model`. |

## Guarantees

- **No member of `SolvingRole` or `AgreementRule` is zero.** The default of the underlying type
  is therefore not a valid value, which makes an unsupplied one detectable rather than silently
  defaulting to something plausible.
- **A discriminator maps to a wire string through `WireNames`, not by its own name.** Renaming
  a member here does not break stored payloads.

## Surprises

- **`OrderingConfidence.Contradicted` is not a rung.** It is what is left when the weakest rung
  fails, so no rule tests it and `OrderingLadder.RuleFor` rejects it.
- **`ContainmentRung` is a lattice, not a chain.** `NominalAndUpperWithin` and
  `NominalAndLowerWithin` are incomparable — either can hold without the other — which is why
  there is no "achieved rung" for containment as there is for ordering.
- **`SolvingRole` is about structure, not enforcement.** Whether a relationship is enforced or
  merely reported is a search policy belonging to whoever asks for a solve, and deliberately is
  not here.
- **`AgreementRule` is a value, not a strategy.** Equality once took an injected estimator,
  which meant the wire said "this is an equality" and nothing about what equality *meant*. A
  strategy cannot be serialized; an enum can.

## What does not belong here

- Evaluation. A ladder classifies; `ComparisonRule` evaluates.
- Wire strings → `Calcusystem.Serialization.Mappers.WireNames`

## Related

`BinaryOperators/` · `Expressions/` · `Provenance/` · `Snapshots/`.
See [`BinaryOperators/OPERATORS.md`](../BinaryOperators/OPERATORS.md) for the full operator
taxonomy.
