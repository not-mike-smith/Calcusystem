# Calcusystem.Api.Http

Where Calcusystem meets HTTP. Reading a posted draft, and turning a failure into a response.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `DraftIntake` | static class | A posted DTO as a draft, or `Invalid` if it describes none. |
| `FailureTranslation` | static class | `FailureReason` → status code, and a `Failure` → problem document. |
| `WireJson` | static class | How this service spells JSON, applied to both option sets. |
| `WireSchema` | static class | What the OpenAPI generator cannot see about the wire. |

## Start here

```csharp
return this.Respond(DraftIntake.Read(draft).Then(workflow.Build));
```

`Respond` is an extension on `ControllerBase`: it serves the value as a 200, or the failure as an
`application/problem+json` document with the status its reason maps to.

## Guarantees

- **This is the only place in Calcusystem that names a status code.** Everything below answers with
  `Either<T>` and a `FailureReason`, which is what keeps `Analysis` from having to speak HTTP.
- **`StatusFor` is total over `FailureReason`.** A member with no case throws rather than defaulting,
  so a reason added to the enum cannot quietly become a 500. `FailureTranslationTests` holds that.
- **A problem document carries `reason` and, where there is one, `problems`.** `reason` is the domain
  enum as a string — several reasons share a status code, and an enum is what a client can branch on.
  `problems` is every `DraftProblem` behind an unbuildable draft.
- **Intake refuses almost nothing.** A modelling mistake belongs in the problems a build reports. Only
  a payload that cannot be *held* as a draft is a 400.

## Surprises

- **`DraftIntake` catches four exception types.** `FormatException`, `InvalidOperationException`,
  `NotImplementedException` and `ArgumentException` are what the draft mapper raises for a malformed
  payload. They are caught around that single call, so nothing else's failure can arrive dressed as a
  bad request.
- **The problems list is recovered by a type test**, for `Failure<IReadOnlyList<DraftProblem>>`. A
  signature returning `Either<ExpressionSystem>` does not say what its failures carry — this is the
  layer that knows, which is the documented cost of that shape.
- **`WireJson` is applied twice, deliberately.** Controllers read the MVC options; the OpenAPI document is
  described from the HTTP ones. Configuring only the first leaves the document describing camelCase while the
  server serves PascalCase — and the front-end's types are generated from that document.
- **A failed *read* names no node.** There is nothing for an editor to mark: the payload was not a
  draft, so no node in it is at fault.

## What does not belong here

- Routes → `Controllers/`
- Mapping a draft to or from its wire shape → `Calcusystem.Serialization.Mappers.DraftMapper`
- Shaping an outcome for a view → `Reports/`

## Related

`Controllers/` · `Calcusystem.Core.Either` · `Calcusystem.Core.Enums` (`FailureReason`).
See the [assembly README](../README.md) for the full status table and why the reason is a domain enum.
