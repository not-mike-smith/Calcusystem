# Calcusystem.Api

The HTTP surface. Serves a browser the four things it can ask about a draft, and the vocabulary to offer an author.

Depends on `Calcusystem.Authoring`, `Calcusystem.Serialization` and `Calcusystem.Analysis`. Nothing depends on it; it is the outermost layer, and the only one that knows HTTP exists.

---

## The shape of the whole thing

**The client owns the draft. The server keeps nothing.** A browser holds a `SystemDraft`, and posts the whole of it to ask a question about it. There is no draft store, no session, no concurrency control and no patch protocol — and so nothing to get out of step with what the author is looking at.

Every endpoint is therefore the same shape: read a posted draft, ask the domain, serve the answer.

```text
Dtos.SystemDraft  ──DraftIntake──▶  SystemDraft  ──DraftWorkflow──▶  outcome  ──Reports──▶  JSON
                      │                                 │
                 400 on a payload                  422 with every
                 that cannot be held               DraftProblem
```

The cost is paid on every request: a draft is posted whole, built from scratch, and thrown away. For a model an engineer is editing by hand that is nothing, and it is the thing to revisit when collaboration or undo/redo is actually wanted — not before.

---

## The endpoints

| Route | Answers with | Also |
| --- | --- | --- |
| `POST /api/drafts/validation` | `DraftProblem[]`, and `[]` when nothing is wrong | — |
| `POST /api/drafts/build` | `Dtos.ExpressionSystem` — the draft in the form a client can save | 422 |
| `POST /api/drafts/decomposition` | `DecompositionReport` — the count, and which unknowns can be solved | 422 |
| `POST /api/drafts/calculation` | `CalculationReport` — values, what is missing, what each relationship did | 422 |
| `GET /api/vocabulary` | `VocabularyReport` — every kind, role, rule and dimension, by wire name | — |
| `GET /api/units` | `UnitCatalogueReport` — every unit, grouped by what it measures | — |

Any of them can answer `400` for a payload that cannot be read as a draft at all.

### Validation answers 200, always

A draft with problems in it is the **answer** to a validation request, not a refusal of one. The request was well formed; the model is unfinished, which is the normal state of a model being written. Serving a 4xx would have an editor display a modelling mistake the way it displays a dropped connection.

The build endpoints are the other way round: there a draft that cannot be built *is* a failed request, so it answers `422` and carries the problems with it.

---

## Using it

```bash
dotnet run --project Api      # http://localhost:5080
```

```bash
curl -X POST http://localhost:5080/api/drafts/calculation \
  -H 'Content-Type: application/json' \
  -d '{
    "Name": "Newton'"'"'s second law",
    "Expressions": [
      { "Id": "m", "Type": "Variable", "Symbol": "m", "Dimensionality": "M1", "KmsValue": 2 },
      { "Id": "a", "Type": "Variable", "Symbol": "a", "Dimensionality": "L1,T-2", "KmsValue": 3 },
      { "Id": "f", "Type": "Product", "OperandIds": ["m", "a"] }
    ]
  }'
```

```json
{
  "IsComplete": true,
  "AllRelationshipsHold": true,
  "Values": [
    { "Id": "m", "Value": { "KmsValue": 2, "Dimensionality": "M1", "KmsUpperAbsoluteUncertainty": 0, "KmsLowerAbsoluteUncertainty": 0 } },
    { "Id": "a", "Value": { "KmsValue": 3, "Dimensionality": "L1,T-2", "KmsUpperAbsoluteUncertainty": 0, "KmsLowerAbsoluteUncertainty": 0 } },
    { "Id": "f", "Value": { "KmsValue": 6, "Dimensionality": "M1,L1,T-2", "KmsUpperAbsoluteUncertainty": 0, "KmsLowerAbsoluteUncertainty": 0 } }
  ],
  "UnresolvedIds": [],
  "MissingValueIds": [],
  "Outcomes": []
}
```

A failed build, on the same payload with a node whose kind nobody chose:

```json
{
  "title": "The request was read but could not be acted on.",
  "status": 422,
  "detail": "One problem stops this draft from building.",
  "instance": "/api/drafts/build",
  "reason": "Unprocessable",
  "problems": [
    { "Kind": "TypeNotChosen", "NodeId": "nothing-yet", "Message": "No kind of expression has been chosen." }
  ]
}
```

---

## What a failure looks like, and why

Everything below this layer answers with `Either<T>` and never throws for a model that is wrong. `Http/FailureTranslation` is the **only** place in Calcusystem that turns one of those into a status code.

| `FailureReason` | Status |
| --- | --- |
| `NotFound` | 404 |
| `Invalid` | 400 |
| `Unprocessable` | 422 |
| `Conflict` | 409 |
| `Unsupported` | 501 |
| `Unexpected` | 500 |

That table is the reason `FailureReason` is a domain enum rather than an `HttpStatusCode`. The domain does not have to choose between `400` and `422` for a half-built model, and `Calcusystem.Analysis` does not have to speak HTTP to report that something failed.

**A problem document carries two things a plain `ProblemDetails` does not.** `reason` is the domain enum as a string, because several reasons share a status code and an enum is what a client can branch on without reading prose. `problems` is every `DraftProblem` behind an unbuildable draft — an editor has to mark up each node at fault, so one message naming the first would send it back to guessing which others there are.

That list arrives by a type test for `Failure<IReadOnlyList<DraftProblem>>`. A signature returning `Either<ExpressionSystem>` does not advertise what its failures carry, which is the documented cost of that shape; this is the layer that knows.

---

## Wire decisions

**Field names are served as they are written** — no naming policy. The same DTO is what a client saves to disk, and a camelCase policy here would give one document two spellings and break "save what you were served".

**Enums travel as names.** A number would renumber an existing payload's meaning the moment a member is inserted, and a name is what a front-end can put straight into a menu. `GET /api/vocabulary` reads those names off the enums themselves, so a kind added to `ExpressionDraftType` is offered to an author without this assembly being touched.

**Nulls are omitted.** On a draft, absent already means *not supplied yet*, and most of a draft being written is absent.

**Values are KMS, in and out.** A dimensionality is encoded (`"M1,L1,T-2"`) and a value is a double in KMS base units. No value on the wire names a unit.

**A unit is served as the two numbers that convert a KMS value into it**, `shown = Slope × kms + Offset`. Every unit in the library is affine in KMS, so those two numbers are the whole of its conversion — and they are *measured* from the unit's own `ConvertFromKmsValue` at 0 and 1 rather than read out of it, which keeps `Measurement` the only place the rule is written. `UnitCatalogueTests` applies them back and compares against the unit itself, for every unit served rather than a sample, so a unit that stopped being affine would fail here rather than disagree quietly in a browser.

**An uncertainty is a difference, so it converts by the slope alone.** Applying the offset to a band turns ±1 K into −457.87 °F. `DeltaSymbol` is what to call the band's unit where that distinction shows.

**The OpenAPI document describes this wire, and is the front-end's source of types.** `dotnet build` writes it to `Api/Api.json`, and development serves it at `/openapi/v1.json`. It is described from the *HTTP* JSON options where a controller answers from the *MVC* ones, so `Http/WireJson` configures both — configuring one would have the document describe a format the server does not serve, and the client's generated types would be wrong in a form its compiler defends. `Http/WireSchema` corrects the one thing the generator cannot see: element nullability does not survive `List<string?>`, and a null operand is how an unfilled slot is written. `OpenApiDocumentTests` holds all of it.

**CORS allows nothing unless configured.** `Cors:AllowedOrigins` in configuration; `appsettings.Development.json` names the local front-end ports. A deployment that says nothing serves no browser but its own.

---

## Scope boundaries

**What belongs here:** routes, status codes, the problem document, CORS and JSON configuration, and the response shapes a view needs.

**What does NOT belong here:**

- Holding a draft or deciding what is wrong with one → `Calcusystem.Authoring`
- Wire shapes that are *saved* — a draft, a built system → `Calcusystem.Serialization`
- Degrees of freedom, decomposition, calculating → `Calcusystem.Analysis`
- Any domain rule. If this assembly ever answers a question about a model itself, the answer belongs below it.

### The one seam onto `Analysis`

`Workflows/DraftWorkflow` is the only type here that names `Calcusystem.Analysis`, and it is four one-line methods. `Analysis` is entered through extension methods today, and that is likely to change; keeping every call to it in one file means the change lands in one file rather than across a controller surface.

---

## Next

- **A front-end.** This surface exists to be consumed; `DecompositionReport` and `CalculationReport` already carry what a view needs to show what is computed, what is violated and what is still missing.
- **A unit named per value.** The catalogue says how to convert; nothing on the wire records which unit an author entered a value in, so reopening a model shows it in whatever the client defaults to.
- **Reopening a saved model**, once `SystemDraft.FromSystem` exists. The route would take a `Dtos.ExpressionSystem` and answer with a draft.
- **An override on an analysis request.** `Analysis` supports values supplied for one analysis only, which nothing here exposes — an editor sets a value on the node and posts the draft again. Worth adding when a what-if view wants to leave the model alone.
- **Serving the front-end from here.** It runs on its own dev server today, proxying `/api`; a deployment would want one origin.

---

## Folders

| Folder | What is in it |
| --- | --- |
| [`Controllers/`](Controllers/README.md) | The routes, and nothing else. |
| [`Http/`](Http/README.md) | Reading a request, and turning a failure into a response. |
| [`Reports/`](Reports/README.md) | The shapes served for an analysis outcome. |
| `Workflows/` | `DraftWorkflow` — what the endpoints do with a draft. One file; see above. |
