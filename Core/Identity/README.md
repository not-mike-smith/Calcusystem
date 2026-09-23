# Calcusystem.Core.Identity

A stable string identity that survives persistence, and the sentinel for minting a fresh one.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `IdBase` | abstract class | The usual `IIdentified` implementation: validates an id and interprets the sentinel. |
| `Constants` | static class | Holds `CREATE_NEW_ID`, the sentinel meaning "mint a fresh identity". |

## Start here

Derive from `IdBase` and pass the caller's id straight through:

```csharp
public class Thing : IdBase
{
    public Thing(string id = Constants.CREATE_NEW_ID) : base(id) { }
}

new Thing();                 // a fresh GUID
new Thing("mass_in");        // adopts "mass_in" verbatim
```

Adopting an id verbatim is what lets a rebuilt graph restore the references between its nodes,
so every reconstruction path passes the stored id rather than defaulting.

## Guarantees

- **`CREATE_NEW_ID` produces a GUID. Any other non-blank string is adopted unchanged.**
- **Null or blank throws.** An object with no identity cannot be referred to, so there is no
  useful default to fall back on.
- **Equality and hashing are by id**, not by reference or by field values.

## Surprises

- **`IIdentified` means "has a stable identity", not "can be referred to by id".** Those come
  apart: a provenance carries an id so that it round-trips faithfully, but it is owned inline
  by a single node and nothing ever names it. Do not read the stronger meaning into the
  interface.

## What does not belong here

- The `IIdentified` contract itself → `Interfaces/`
- Resolving an id back into an object → `Interfaces/INodeResolver`
- Anything with domain behavior. This holds identity and nothing else.

## Related

`Interfaces/` (`IIdentified`, `INodeResolver`).
See the [assembly README](../README.md) for why identity lives in `Core` rather than in the
expression layer.
