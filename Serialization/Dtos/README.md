# Calcusystem.Serialization.Dtos

The wire shape of a saved system: flat, id-referenced, and holding no behavior.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `ExpressionSystem` | class | The document root. Holds all lists of expressions and relationships. |
| `SingleVariable` | class | A leaf, with its value, dimensionality, and provenance. |
| `SingleDerivedVariable` | class | A one-argument expression, by the id of its inner node. |
| `PairDerivedVariable` | class | A two-argument expression, by two ids. |
| `ListDerivedVariable` | class | An n-ary expression, by an ordered list of ids. |
| `BinaryOperator` | class | A relationship, by the ids of its two operands. |
| `Provenance` | class | The union of every provenance kind's metadata. |

## Start here

You do not construct these by hand. `Mappers/SerializingMapper` produces them and
`DeserializingMapper` consumes them; a serializer turns the root into JSON.

```csharp
var dto  = new SerializingMapper().Map(system);
var json = JsonSerializer.Serialize(dto);
```

## Guarantees

- **Every DTO carries `Id` and `Type`.** `Id` is what other DTOs reference; `Type` names the
  concrete domain class to rebuild.
- **The document is flat.** Children are named by id, never nested, so a node shared by three
  parents is written once and the sharing survives a round trip.
- **Grouping is by arity, not by operation.** One list per shape — single, pair, list — because
  the kinds differ in what they compute, not in what must be stored to rebuild them.
- **Property names are the JSON field names.** There is no naming policy in between, so
  renaming a property here is a wire break.

## Surprises

- **A DTO named `ExpressionSystem` is not the domain `ExpressionSystem`.** Both names are live
  in this assembly, which is why the mappers spell the wire one `Dtos.ExpressionSystem` and
  import the domain one. Keep that habit in any new code here.
- **`Provenance` is the union of all four kinds' fields**, discriminated by `Type`, and only the
  fields belonging to that kind are populated. The kinds differ in metadata, not behavior, so
  a flat shape costs nothing and keeps the seam non-generic.
- **Not everything is `required`.** A field added after payloads were already written is
  deliberately optional, so that older documents still load with the default that matches what
  they meant.

## What does not belong here

- Mapping logic → `Mappers/`
- Domain invariants. A DTO validates nothing; a rebuild is where refusal happens.
- The encoding of a dimensionality → `Mappers/DimensionalityCodec`

## Related

`Mappers/` · `Interfaces/` (`ISerializedObject`).
See the [assembly README](../README.md) for the round-trip contract and the list of changes
that break stored payloads.
