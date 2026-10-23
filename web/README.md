# calcusystem-web

The editor. Build a model, see what is wrong with it as you type, and see what it computes.

React + TypeScript + Vite. Talks to `Calcusystem.Api` and holds nothing the server needs to know about.

---

## Running it

Two processes. The API first:

```bash
dotnet run --project ../Api      # http://localhost:5080
npm install && npm run dev       # http://localhost:5173
```

Vite proxies `/api` to the server, so the browser treats the two as one site and CORS never enters into
development. The API's CORS configuration is there for a deployment that really does serve them from two
origins.

---

## The types are generated, not written

`src/api/schema.d.ts` comes from the API's OpenAPI document, which `dotnet build` writes to `Api/Api.json`.
`npm run dev` and `npm run build` regenerate it first, so the client's idea of the wire cannot drift from the
server's while you are working against it.

```text
Api/*.cs  ──dotnet build──▶  Api/Api.json  ──openapi-typescript──▶  src/api/schema.d.ts
```

**Nothing in `src/api/schema.d.ts` is edited by hand.** A field renamed in a DTO becomes a TypeScript error
here, which is the whole point of generating it — the alternative is a second set of interfaces that agree with
the server only until somebody forgets.

A checkout that has not built the API yet uses the checked-in types and says so. They are wrong only if the
wire changed since they were generated.

---

## What it does

- **Variables** — a name, what it measures, and a value if anyone has supplied one.
- **Formulas** — a kind and the nodes it is over. An empty operand slot is a normal state, not an error to
  apologise for.
- **Relationships** — two nodes and an operator, with the verdict beside it once the model runs.
- **Can this be solved?** — degrees of freedom, and which unknowns are determinable, under-determined, or
  reached by nothing at all.
- **Findings** — whether every value resolved, whether every relationship holds, and which unset variables are
  holding the rest back.

---

## How it talks to the server

**The draft is posted whole, shortly after it stops changing.** There is no patch protocol and no session; the
server keeps nothing. `useAnalysis` debounces, fires `calculation` and `decomposition` together, and drops any
reply that is no longer about what is on screen.

**Problems come from the failed build**, not from a third call to `/api/drafts/validation`. `Validate` reads
its own answer off a `Build`, so the two cannot disagree, and one of them is already being asked. The
`validation` route is there for a client that only wants the problems.

**The draft lives in `localStorage`.** The client owns it, so the client is what keeps it.

---

## Decisions worth knowing

**A new variable has no dimensionality, not a dimensionless one.** They are different answers and only one of
them is the author's. The domain refuses to guess a default here because a guess builds a different model than
the one intended, so the editor shows `— not chosen —` and the server says what is missing until the author
answers.

**Deleting a node leaves every reference to it.** That is what the server does, and the references come back as
`ReferenceNotFound` naming the node that holds them. Scrubbing them would silently discard structure the author
built.

**Named dimensionalities are this editor's convenience.** The symbols come from `/api/vocabulary`, so a
dimension added to the domain appears here with nothing changed. The named combinations in
`src/api/dimensionality.ts` — *Force*, *Pressure*, *Energy* — are a menu, not a rule: nothing here decides what
a dimensionality means.

**The menus are served, not hardcoded.** Expression kinds, relationship kinds, roles, agreement rules,
landmarks and comparisons all come from `/api/vocabulary`, so adding a kind to the domain offers it to an
author without this project being touched.

**Values are KMS, in and out.** No unit is named anywhere — a dimensionality is shown as `M·L·T⁻²` and a value
is a number in base units. Showing 4.409 lb needs the unit catalogue served, which the API does not do yet.

---

## Layout

| Path | What is in it |
| --- | --- |
| `src/api/` | The generated schema, the fetch wrappers, and the dimensionality encoding. |
| `src/model/` | The draft as client state, and the hook that asks the server about it. |
| `src/components/` | One component per thing on screen. |
| `src/App.tsx` | The page, and the only place that holds the draft. |

---

## Not built yet

- **Saving and reopening.** `POST /api/drafts/build` already answers with the model in the form that persists;
  reopening one needs `SystemDraft.FromSystem` on the server.
- **Units.** See above.
- **Reordering operands.** The order matters where the kind cares — a quotient divides the first by the second
  — and nothing here moves one yet.
- **Tests.** The server's suites cover every answer this page renders; what is untested is the rendering.
