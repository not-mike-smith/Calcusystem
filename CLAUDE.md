# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

.NET 10 solution (`Calcusystem.sln`) plus a separate Node front-end in `web/`.

```bash
dotnet build                                   # whole solution; also writes Api/Api.json (OpenAPI, gitignored)
dotnet test                                    # all xUnit suites
dotnet test Analysis.Test                      # one project
dotnet test --filter "FullyQualifiedName~DecompositionTests"   # one class / test
dotnet run --project Api                       # API on http://localhost:5080
dotnet run --project Examples                  # worked engineering examples, printed

cd web && npm install && npm run dev           # editor on http://localhost:5173 (proxies /api to :5080)
cd web && npm run build                        # tsc --noEmit + vite build
cd web && npm run patterns-check               # PATTERNS.md, language.ts and the gallery must agree
cd web && npm run render-check                 # prints each building block as text (not a test)
```

CI (`.github/workflows/ci.yml`) runs the build, every test suite, the editor's build and `patterns-check`, and fails if `web/src/api/schema.d.ts` differs from what `dotnet build` generates. **A change to the API's DTOs must commit the regenerated types**, by running `npm run build` in `web/` after `dotnet build`.

The build fails on broken doc comments: `Directory.Build.props` turns unresolved `cref`s and malformed XML doc (CS1570–CS1584, CS1734) into errors. Renaming a type means fixing every `<see cref>` to it.

## Architecture

Layered assemblies, bottom-up; each has a matching `*.Test` xUnit project (FluentAssertions):

```text
Core ─▶ Measurement ─▶ DimensionedExpression ─┬▶ Analysis ──────┐
                                              ├▶ Authoring ─────┼▶ Api ─▶ web/ (React+TS+Vite)
                                              └▶ Serialization ─┘
Examples (depends on Analysis; depended on by nothing)
```

Serialization also depends on Authoring (it maps `SystemDraft` as well as `ExpressionSystem`).

1. **Measurement** — everything is stored in KMS (SI base) units; units exist only at the input/output boundary. `Measurand` = `Quantity` (KMS value + `Dimensionality`) + uncertainty. Adding mismatched dimensions throws; `Try*` variants return NaN instead.
2. **DimensionedExpression** — `IExpression` trees (`Variable` leaves, computed nodes), `IBinaryOperator` relationships (equality/tolerance/ordering), and the `ExpressionSystem` container. A node's `ComputeIfFullyDescribed()` re-walks and caches nothing by design.
3. **Analysis** — `system.Flatten()` (degrees of freedom), `.Decompose()` (solvable vs underdetermined parts), `system.Calculate()` (computes each node once — the way to compute over a whole system). The solver (Milestone 4) will live here; nothing yet inverts an equation.
4. **Authoring** — `SystemDraft`: a model being edited, freely invalid, never throws; reports `DraftProblem`s and builds into an `ExpressionSystem` when coherent.
5. **Serialization** — object mapping to/from flat, id-referenced DTOs (not byte encoding).
6. **Api** — the only layer aware of HTTP. Stateless: the client posts the whole draft each time. `Api.Test` runs over real HTTP via `WebApplicationFactory`.
7. **web/** — not in the solution. `src/api/schema.d.ts` is **generated** (`dotnet build` → `Api/Api.json` → openapi-typescript, run by `predev`/`prebuild`); never edit it by hand. Menus/vocabulary come from `/api/vocabulary`, not hardcoded. Draft lives in `localStorage`.

## Conventions

- **Folder = namespace.** No loose `.cs` at an assembly root (except `Program.cs` in `Api`/`Examples`). A new project is `Foo/Foo.csproj`; `Directory.Build.props` makes it `Calcusystem.Foo` — do not set `AssemblyName`/`RootNamespace` in the `.csproj`.
- **READMEs are the API docs.** Each assembly and each multi-file folder has a `README.md` (sections: What's here / Start here / Guarantees / Surprises / What does not belong here / Related). Read the relevant README before reading implementation; update it when behavior changes.
- **README code is tested.** Each `*.Test/ReadmeSnippets.cs` compiles/posts the snippets shown in that assembly's README — change one, change the other.
- **Doc comments and READMEs follow `DOCUMENTATION.md`**: the relevance test for doc comments, no history/roadmap in comments (roadmap goes in `project-plan.md`), fixed vocabulary (e.g. *determined*), US spelling, no counts that rot.
- Public interfaces carry XML docstrings on the interface and each member stating the contract.
- `project-plan.md` holds milestones, design decisions, known bugs, and open questions.
