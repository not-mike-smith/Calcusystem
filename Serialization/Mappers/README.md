# Calcusystem.Serialization.Mappers

Turns a live `ExpressionSystem` into DTOs and back. This is the whole of the assembly's
behaviour; everything else here is shape.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `SerializingMapper` | class | Domain → DTOs. Reads snapshots only. |
| `DeserializingMapper` | class | DTOs → domain, rebuilding the graph in dependency order. |
| `DeserializationContext` | class | The `INodeResolver` a rebuild uses to turn ids back into objects. |
| `WireNames` | static class | Maps snapshot discriminators to and from the `Type` strings on the wire. |
| `DimensionalityCodec` | static class | Encodes a dimensionality as `"M1,L1,T-2"`, and back. |

## Start here

```csharp
using Calcusystem.Serialization.Mappers;

var dto = new SerializingMapper().Map(system);

var context = new DeserializationContext();
var restored = new DeserializingMapper(context).Map(dto);
```

The `DeserializationContext` is supplied rather than created internally so that a caller
rebuilding several documents can share one, and so the resolver is inspectable when a rebuild
fails.

## Guarantees

- **Serializing reads nothing but snapshots.** It never touches an expression's children, an
  operator's operands, or a value's internals, so a domain type can change its private shape
  without breaking the wire.
- **The encoding of a dimensionality is canonical.** Snapshot pairs come out in a fixed
  dimension order, so dimensionally-equal values always produce the identical string — safe to
  diff, compare, or hash.
- **A rebuild refuses rather than guesses.** An equality with no agreement rule, or a simple
  comparison with no rule, throws; a guessed reading is exactly the ambiguity that storing
  them removed.
- **An unresolvable graph reports what was wrong with it.** `UnresolvableGraphException`
  carries the unbuilt, missing, and cyclic ids separately.

## Surprises

- **`WireNames` maps to concrete class names, not enum member names**, and is deliberately not
  a mirror. If a domain class is renamed, the string here should *not* follow it — this is the
  one place a migration can be written, and making it track the code automatically would
  remove that.
- **`DimensionalityCodec` lives here, not in `Measurement`.** Which identity to key on, how to
  lay the pairs out, and what to do with a payload written before a symbol was renamed are all
  format decisions. `Measurement` supplies only the structural snapshot.
- **The fundamental-dimension symbols are the contract.** Renaming one invalidates previously
  written data, and migrating it is this layer's job. The symbol set is distinct even ignoring
  case, so a stray case conversion downstream cannot rewrite one dimension into another.

## What does not belong here

- The DTO shapes → `Dtos/`
- What state defines a domain object → that object's assembly, as a snapshot
- Byte encoding. This layer maps objects; a serializer turns the DTOs into JSON.

## Related

`Dtos/` · `Exceptions/` · `Interfaces/` (`ISerializedObject`) ·
`Calcusystem.Core.Interfaces` (`INodeResolver`).
See the [assembly README](../README.md) for the round-trip contract and what breaks stored
payloads.
