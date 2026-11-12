# Calcusystem.DimensionedExpression.Exceptions

What the expression layer throws when a model is put together wrongly.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `CyclicExpressionGraphException` | class | A walk found a node that is reached from its own operands. |
| `InvalidNameException` | sealed class | A node was given a name that `Names.NodeName` refuses. |
| `DuplicateNameException` | sealed class | A system was given a node whose name another of its nodes has. |
| `InvalidFixedValueException` | sealed class | A fixed value was given a value its kind does not accept. |

## Start here

```csharp
new Variable(" m", Dimensionality.Mass);   // InvalidNameException: whitespace at an end

system.Add(new Variable("m", Dimensionality.Mass));
system.Add(new Variable("m", Dimensionality.Mass));   // DuplicateNameException, and nothing is added
```

## Guarantees

- **`InvalidNameException` is thrown where the node is built**, never later. Its message is what
  `NodeName.Problem` answers for that name.
- **`InvalidFixedValueException` is thrown where the node is built.** Its message is what
  `FixedValueRule.Problem` answers for that kind and value.
- **`DuplicateNameException` leaves the system as it was.** `Name` is the name two nodes claim.

## Surprises

- **Dimensional refusals are not here.** A sum of a mass and a length throws
  `IncompatibleDimensionsException`, which belongs to `Calcusystem.Measurement.Exceptions`.
- **`InvalidNameException` is an `ArgumentException`; `DuplicateNameException` is an
  `InvalidOperationException`.** The first is a bad value on its own. The second is a valid name that the
  system's state does not allow.

## What does not belong here

- The rule for a name → `Names/` (`NodeName`)
- The rule for a fixed value's value → `Expressions/` (`FixedValueRule`)
- What a draft reports for the same mistakes, without throwing → `Calcusystem.Authoring` (`DraftProblemKind`)

## Related

`Names/` · `Systems/` · `Expressions/`.
See the [assembly README](../README.md) for names and for why a system holds them unique.
