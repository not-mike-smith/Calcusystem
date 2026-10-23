# Calcusystem

An engineering calculation platform where physical quantities carry their **units, dimensions, and measurement uncertainty** as first-class concerns. You describe a system of variables, formulas, and tolerances; the platform tracks dimensions, propagates uncertainty, and reports which constraints hold — with evaluation and solving on the roadmap.

The goal is calculations that can't silently go wrong: adding a length to a mass throws, converting units happens only at the boundary, and every derived value carries the uncertainty of the measurements it came from.

---

## How to read this codebase

The code is organized so you rarely need to read implementation. **If a folder has a `README.md`, you should not have to read anything else in it to use that namespace.** You only need the implementation files to _modify_ them.

Every folder is a namespace and every namespace is a folder — so a folder listing is a table of contents, and no assembly has loose `.cs` files at its root. The one exception is `Api/Program.cs`: top-level statements have no namespace to be a folder of, and putting the entry point in one would suggest otherwise.

So, depending on your task:

- **Using an assembly**: read its `README.md` for the invariants that span the whole layer, then the `README.md` of the folder you need. Each folder README lists what is there, where to start, what you can rely on, and where a reasonable expectation turns out to be false.
- **Modifying an assembly**: additionally read the implementation files for the types you're changing.

A folder holding a single file has no README — reading the file is the shorter answer.

---

## Project structure

Library assemblies stacked bottom-up, with the web app at the top, and a matching test project for each. Every assembly and namespace is `Calcusystem.*`; the directory it lives in is the short name (`Core/`, `Measurement/`, …). See [naming](#naming-directories-assemblies-and-namespaces).

| Assembly / namespace | Directory | Depends on | What it does |
| --- | --- | --- | --- |
| `Calcusystem.Core` | [`Core/`](Core/README.md) | — | The basement: shared identity (`IIdentified`, `IdBase`), the persistence seams (`ISnapshotting`, `ISnapshottingNode`, `INodeResolver`), and `Either<T>` for an operation that may fail without throwing. |
| `Calcusystem.Measurement` | [`Measurement/`](Measurement/README.md) | `Calcusystem.Core` | Physical quantities with KMS-normalized units, dimensional algebra, a unified `Measurand` value type, and uncertainty propagation. The foundation. |
| `Calcusystem.DimensionedExpression` | [`DimensionedExpression/`](DimensionedExpression/README.md) | `Measurement` (+ `Core`) | Trees of dimensioned variables and formulas (`IExpression`), binary operators for equality/tolerance/ordering constraints, and the `ExpressionSystem` container. |
| `Calcusystem.Serialization` | [`Serialization/`](Serialization/README.md) | `DimensionedExpression`, `Authoring` | Maps an `ExpressionSystem` or a `SystemDraft` to/from flat, id-referenced DTOs for persistence (object mapping, not byte encoding). |
| `Calcusystem.Analysis` | [`Analysis/`](Analysis/README.md) | `DimensionedExpression` | Asks whether a system is well-posed and what it computes to: flattens it to unknowns × equations, reports degrees of freedom, decomposes it into the parts that can be solved, and calculates it. Where the solver will live. |
| `Calcusystem.Authoring` | [`Authoring/`](Authoring/README.md) | `DimensionedExpression` | Holds a system while someone is still building it — freely invalid, nothing thrown — and builds it into an `ExpressionSystem` once coherent. The seam an editing front-end works against. |
| `Calcusystem.Api` | [`Api/`](Api/README.md) | `Authoring`, `Serialization`, `Analysis` | The HTTP surface. Serves a browser what it can ask about a draft it holds, and the only layer that knows HTTP exists. |

Above them sits [`web/`](web/README.md), the editor — React + TypeScript + Vite, talking to `Calcusystem.Api`. It is not a .NET project, so it is lower-case and absent from the solution. Its types are **generated** from the API's OpenAPI document rather than written, so the client's idea of the wire cannot drift from the server's.

Each assembly has a matching `*.Test` project holding its xUnit suite. `Api.Test` runs its suite over real HTTP through `WebApplicationFactory`, because model binding, the JSON options and the status mapping are most of what that layer is. `Core.Test` covers only the two things there with behavior — `IdBase` and `Either/`; its interfaces are exercised through the layers that implement them.

### Naming: directories, assemblies, and namespaces

One rule, enforced in one place:

| | Example |
| --- | --- |
| Directory and `.csproj` | `Measurement/Measurement.csproj` |
| Assembly and root namespace | `Calcusystem.Measurement` |

[`Directory.Build.props`](Directory.Build.props) derives the second from the first, so adding a project needs no per-project configuration — create `Foo/Foo.csproj` and it ships as `Calcusystem.Foo`.

The split exists because the two names answer to different audiences. On disk, the prefix is pure repetition — every directory would carry it. To a consumer it is the opposite: `Measurement` is far too generic a name to occupy a global namespace or drop a `Measurement.dll` on someone's output path, and a library should own exactly one root.

---

## Quick start

Compute with units and uncertainty (the `Measurement` layer):

```csharp
using Calcusystem.Measurement.Primitives;   // Quantity, Measurand, Dimensionality
using Calcusystem.Measurement.Extensions;   // Percent(), Fraction(), Units()
using Calcusystem.Measurement.Units;        // Mass, Force, Length, …

// 2 kg ± 1% — supply and read values in whatever unit you like; storage is always KMS
var mass = Mass.Kilogram.Quantity(2).WithUncertainty(1.0.Percent());
mass.In(Mass.Pound);   // ≈ 4.409 lb
mass.RelativeUncertainty;    // 0.01

// arithmetic enforces dimensions and propagates uncertainty
var accel = new Quantity(9.81, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time))
    .WithUncertainty(0.5.Percent());

var force = mass.Times(accel);   // dimension M·L·T⁻²; uncertainty combines in quadrature
```

Assemble a reusable formula whose leaves get filled in later (the `DimensionedExpression` layer):

```csharp
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Primitives;

var m = new Variable("m", Dimensionality.Mass);
var a = new Variable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

var f = new ProductExpression([m, a]);

f.Dimensionality;      // M·L·T⁻²  — known before any value is supplied
f.UnsetVariables();     // [m, a]  — the distinct unbound leaves
f.IsFullyDescribed;    // false until both leaves are set
```

Whether a whole system can be solved, and what it currently computes to, are answered one layer up:

```csharp
using Calcusystem.Analysis.Extensions;

var flat = system.Flatten();
flat.DegreesOfFreedom;  // unknowns − determining equations
flat.Determination;     // Underdetermined / ExactlyDetermined / Overdetermined

var parts = flat.Decompose();
parts.IsWellPosed;          // every unknown has an equation, and every equation an unknown
parts.WellDetermined;       // the part that can be solved
parts.Underdetermined;      // the part that cannot — which the count alone can net away

var calc = system.Calculate();
calc.ValueOf(f);        // 6 kg·m·s⁻² — each node computed exactly once
calc.MissingValues;     // the unbound variables holding the rest back
```

`Calculate` is the way to compute over a whole system: a node's own `ComputeIfFullyDescribed()` re-walks to the leaves on every call and caches nothing, by design.

A model being written by a person is held as a `SystemDraft` instead, which is freely invalid and reports what stands between it and a built system (the `Authoring` layer). `Calcusystem.Api` serves that over HTTP, and `web/` is the editor over it:

```bash
dotnet run --project Api      # http://localhost:5080
cd web && npm install && npm run dev   # http://localhost:5173
```

Build a model, watch the dimensional errors appear and clear as you type, supply values, and see what is computed, what is violated, and what is still missing.

See each assembly's README for the full surface.

---

## Build and test

Built on .NET 10.

```bash
dotnet build          # build the whole solution
dotnet test           # run all test suites
```

The editor is a separate toolchain, and needs Node:

```bash
cd web && npm install && npm run build
```

---

## Status and roadmap

The measurement, expression, serialization, and analysis layers are functional: a system can be flattened, decomposed into its solvable parts, calculated, and have its relationships judged. A model can be authored in the browser, posted over HTTP, and answered with what it computes and what is wrong with it. Solving is the current milestone. The full milestone plan, design decisions, and open questions live in [`project-plan.md`](project-plan.md).

---

## Contributing conventions

- **Every assembly has a `README.md`** at its root, covering purpose, cross-cutting invariants, dependencies, and explicit scope boundaries (what does _not_ belong there).
- **Every multi-file folder has one too**, covering just that namespace: What's here / Start here / Guarantees / Surprises / What does not belong here / Related.
- **A new project is `Foo/Foo.csproj`** — short directory, no prefix. `Directory.Build.props` gives it the `Calcusystem.Foo` assembly and root namespace; do not set either in the `.csproj`.
- **Public interfaces carry XML docstrings** on the interface and each member, articulating the contract — this is what lets a reader use a layer without opening its implementation.
- **Doc comments and READMEs follow [`DOCUMENTATION.md`](DOCUMENTATION.md)** — what belongs on a hover, what belongs in a README, and the vocabulary both use.
- **Tests live in the matching `*.Test` project** and reference only the layers they cover.
- Prefer small, focused commits; keep behavior changes and documentation legible in the diff.
