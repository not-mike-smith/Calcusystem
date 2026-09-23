# Calcusystem.Serialization.Exceptions

What a rebuild throws when a document cannot be turned back into a graph.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `ReferencedNodeNotFoundException` | class | One DTO names an id that is not in the document. |
| `UnresolvableGraphException` | class | The document cannot be rebuilt in any order. |

## Start here

Both carry the diagnosis, not just a message:

```csharp
catch (UnresolvableGraphException ex)
{
    ex.UnbuiltIds;   // nodes that never became buildable
    ex.MissingIds;   // ids referenced but absent from the document
    ex.CyclicIds;    // nodes that depend on each other
}

catch (ReferencedNodeNotFoundException ex)
{
    ex.IdOfMissingNode;   // the id that could not be resolved
    ex.ReferencingDto;    // the DTO that named it
}
```

## Guarantees

- **The three id lists are separate because the fixes differ.** A missing id means the document
  is incomplete; a cycle means it is malformed; an unbuilt node with neither means a dependency
  was not reached. Collapsing them into one list would leave a caller unable to tell which.
- **Failure is total.** A rebuild that throws produces no partial system, so a caller never
  holds a half-built graph.

## Surprises

- **These signal a defect in the data, not in the caller.** A domain type is not asked to paper
  over an inconsistent document, so the exception surfaces rather than a null or a default.

## What does not belong here

- Dimension mismatches → `Calcusystem.Measurement.Exceptions`
- Cycles created while *building* a system in memory →
  `Calcusystem.DimensionedExpression.Exceptions`

## Related

`Mappers/` (where both are thrown) · `Dtos/`.
See the [assembly README](../README.md) for the rebuild order and why it can fail.
