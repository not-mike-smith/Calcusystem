# Calcusystem.Serialization.Dtos

The wire shape of a saved system and of a draft of one: flat, id-referenced, and holding no behavior.

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
| `SystemDraft` | class | A draft's document root. Every field optional. |
| `ExpressionDraft` | class | One expression node under construction. |
| `RelationshipDraft` | class | One relationship under construction. |

## Start here

You do not construct these by hand. `Mappers/SerializingMapper` produces them and
`DeserializingMapper` consumes them; a serializer turns the root into JSON.

```csharp
var dto  = new SerializingMapper().Map(system);
var json = JsonSerializer.Serialize(dto);
```

The draft three are produced and consumed by `Mappers/DraftMapper` the same way:

```csharp
var wire = DraftMapper.ToWire(draft);
var json = JsonSerializer.Serialize(wire);
```

## Guarantees

- **Every saved-system DTO carries `Id` and `Type`.** `Id` is what other DTOs reference; `Type`
  names the concrete domain class to rebuild.
- **On a draft DTO, `null` means not supplied yet.** Every field is optional, including the ids,
  and a node with no kind chosen is a document the server accepts.
- **The document is flat.** Children are named by id, never nested, so a node shared by three
  parents is written once and the sharing survives a round trip.
- **Grouping is by arity, not by operation.** One list per shape — single, pair, list — because
  the kinds differ in what they compute, not in what must be stored to rebuild them.
- **Property names are the JSON field names.** There is no naming policy in between, so
  renaming a property here is a wire break.

## Surprises

- **A DTO named `ExpressionSystem` is not the domain `ExpressionSystem`.** Four names here are
  live in two assemblies at once — that one plus `SystemDraft`, `ExpressionDraft` and
  `RelationshipDraft` — which is why the mappers spell the wire ones `Dtos.SystemDraft` and
  import the domain ones. Keep that habit in any new code here.
- **`Provenance` is the union of all four kinds' fields**, discriminated by `Type`, and only the
  fields belonging to that kind are populated. The kinds differ in metadata, not behavior, so
  a flat shape costs nothing and keeps the seam non-generic.
- **Not everything is `required`.** A field added after payloads were already written is
  deliberately optional, so that older documents still load with the default that matches what
  they meant.
- **The draft DTOs do not implement `ISerializedObject`.** It requires a non-null `Id` and
  `Type`, and a draft guarantees neither — the kind is what the author has not chosen yet.
- **`ExpressionDraft` carries two dimensionalities.** `ValueDimensionality` is written only
  where the value disagrees with the node's declared one, which an editor can reach and the
  build reports. Reading it back without that field would mend the model in transit.
- **`ExpressionDraft.OperandIds` holds nulls.** An opened-but-unfilled slot is a `null` entry,
  and the order is the operand order — a quotient divides the first by the second.

## What does not belong here

- Mapping logic → `Mappers/`
- Domain invariants. A DTO validates nothing; a rebuild is where refusal happens.
- The encoding of a dimensionality → `Mappers/DimensionalityCodec`
- Which refusals become which status code → `Calcusystem.Api`

## Related

`Mappers/` · `Interfaces/` (`ISerializedObject`) · `Calcusystem.Authoring.Drafts` (what the
draft three carry).
See the [assembly README](../README.md) for the round-trip contract and the list of changes
that break stored payloads.
