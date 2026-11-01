# Calcusystem.Api.Controllers

The routes. Each one reads a request, hands it to a workflow, and serves what comes back.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `DraftsController` | sealed class | `POST /api/drafts/{validation,build,decomposition,calculation}` |
| `VocabularyController` | sealed class | `GET /api/vocabulary` |
| `UnitsController` | sealed class | `GET /api/units` |

## Start here

Every action is one line, and they are all the same line:

```csharp
[HttpPost("calculation")]
public IActionResult Calculate([FromBody] Dtos.SystemDraft draft) =>
    this.Respond(DraftIntake.Read(draft).Then(workflow.Calculate));
```

`DraftIntake.Read` gives an `Either`, `Then` runs the work only if reading succeeded, and `Respond`
serves a 200 or a problem document. Nothing branches on a status code here.

## Guarantees

- **A controller holds no state and decides nothing about a model.** If an action needs to grow a
  second statement, what it needs probably belongs in `Workflows/`.
- **`validation` always answers 200.** Problems are the answer, not a refusal. The build routes
  answer 422 instead, because there a draft that cannot be built is a failed request.
- **Every response a route can give is declared**, with `ProducesResponseType`, so the set is
  readable without tracing `Respond`.

## Surprises

- **`Dtos` is an alias here.** `using Dtos = Calcusystem.Serialization.Dtos;` — `SystemDraft` names
  both a draft and its wire shape, and the alias keeps which one is meant visible at the call site.
  A plain `using` of the namespace would not even compile from this one.
- **Nothing is `async`.** Building and calculating a posted draft are synchronous and in-memory;
  there is no I/O to await. That changes the day a model is loaded from storage.

## What does not belong here

- Deciding what is wrong with a model → `Calcusystem.Authoring`
- Choosing a status code → `Http/FailureTranslation`
- Shaping an outcome for a view → `Reports/`
- Naming `Calcusystem.Analysis` → `Workflows/DraftWorkflow`, deliberately the only place

## Related

`Http/` · `Workflows/` · `Reports/`.
See the [assembly README](../README.md) for why the server keeps nothing between requests, and for
what each route answers with.
