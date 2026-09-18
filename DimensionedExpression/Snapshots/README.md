# Calcusystem.DimensionedExpression.Snapshots

What each node and relationship hands out so a graph can be rebuilt: flat, id-referenced, and
grouped by arity.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `ExpressionSystemSnapshot` | readonly record struct | The system: its identity, metadata, and the ids it contains. |
| `VariableSnapshot` | readonly record struct | A leaf: value, dimensionality, symbol, provenance. |
| `UnaryExpressionSnapshot` | readonly record struct | One-argument node: type, id, inner id. |
| `BinaryExpressionSnapshot` | readonly record struct | Two ordered children, plus the correlation assumption. |
| `NaryExpressionSnapshot` | readonly record struct | Ordered child ids, plus the correlation assumption. |
| `BinaryOperatorSnapshot` | readonly record struct | A relationship: both operand ids, role, and any extra state. |
| `ProvenanceSnapshot` | readonly record struct | The union of every provenance kind's metadata. |

## Start here

```csharp
var snapshot = variable.GetSnapshot();
var restored = Variable.FromSnapshot(snapshot);
```

`Variable` is the only one that rebuilds itself. Everything else names other nodes by id, so it
goes through a factory with a resolver:

```csharp
ExpressionFactory.FromSnapshot(unarySnapshot, resolve);
BinaryOperatorFactory.FromSnapshot(operatorSnapshot, resolve);
ProvenanceFactory.FromSnapshot(provenanceSnapshot);
```

## Guarantees

- **Children are named by id, never nested.** A graph is not a tree, so nesting would duplicate
  shared nodes and could not express the sharing at all.
- **Grouping is by arity, not by operation.** The five unary kinds differ in what they compute,
  not in what must be stored, so one record with a `Type` discriminator covers all of them.
- **Reconstruction refuses rather than guesses.** An equality with no agreement rule, or a
  simple comparison with no rule, throws — a guessed reading is the ambiguity that storing them
  removed.
- **`BinaryOperatorSnapshot` stores `SolvingRole`, not a derived boolean.** Flattening
  `Equation` and `Coherence` into one `true` loses a distinction that cannot be recovered on
  load.

## Surprises

- **These are mementos, not DTOs.** No wire type-name, no schema version, no encoding. That is
  what lets the concrete types keep their metadata `internal` — before snapshots existed, those
  properties were public solely so a mapper in another assembly could read them.
- **`BinaryOperatorSnapshot` carries two fields most operators never use.** `Agreement` is set
  only for an equality and `Rule` only for a `SimpleComparison`; both are null otherwise. The
  alternative was a record per operator kind, for two fields.
- **`ProvenanceSnapshot` is the union of all four kinds' fields**, discriminated by `Type`, and
  only the ones belonging to that kind are populated.
- **Only `SolvingRole` survives for equalities.** Every other operator kind is a requirement by
  construction, so reconstruction ignores a stored role for them rather than honouring
  something the type cannot represent.

## What does not belong here

- Wire shapes and `Type` strings → `Calcusystem.Serialization`
- Reconstruction logic → `Expressions/ExpressionFactory`, `BinaryOperators/BinaryOperatorFactory`, `Provenance/ProvenanceFactory`

## Related

`Expressions/` · `BinaryOperators/` · `Provenance/` · `Systems/` · `Enums/` ·
`Calcusystem.Core.Interfaces` (`ISnapshotting`, `ISnapshottingNode`, `INodeResolver`).
