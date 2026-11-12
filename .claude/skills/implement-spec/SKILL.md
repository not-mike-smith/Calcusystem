---
name: implement-spec
description: Implement one slice of a feature-set spec in specs/, contracts first, then tests, then implementation, then review — with checkpoints for the user. Use only when the user asks to implement a spec or a slice of one.
argument-hint: <spec path> [slice number]
disable-model-invocation: true
---

# Implement a spec

Implement `$ARGUMENTS`: a spec in `specs/`, and optionally which of its slices. With no slice given, ask which one. **Work one slice at a time**, on its own branch and pull request; a spec's slices are written to merge separately.

Commit points are marked **⟶ commit**. At each one: commit with a message in this repository's style, and push. `master` is protected — never commit to it.

**The pull request is a draft until the implementation is complete.** Create it as a draft at the first commit (`gh pr create --draft`), and mark it ready at the end of step 10 (`gh pr ready`). CI skips drafts, so it runs from that point on; to check a draft earlier, run it by hand with `gh workflow run ci.yml --ref <branch>`. If `gh pr create` or `gh pr ready` fails on a GitHub API error unrelated to the change, fall back to `gh api` (the REST `pulls` endpoint, or the GraphQL `markPullRequestReadyForReview` mutation).

---

## 1. Read

1. The spec in full: its slices, *Decisions* and *Open questions*. **Stop if the spec has open questions** that touch this slice, and ask them.
2. The feature set in `project-plan.md`, and every **Known Bug** and retroactive spec (`specs/retroactive/`) the slice touches. A Known Bug in the code being changed is either fixed in this slice or named in the PR as left alone.
3. The READMEs of every folder the slice touches, then the code, then its tests.
4. `DOCUMENTATION.md`, before writing any doc comment or README text.

## 2. Ask

Raise everything unclear **at once**, numbered, each grounded in the code it is about and with a lean where there is one. The `/spec-review` skill's lenses are the checklist. **Wait for the answers.** Record each answer in the spec's *Decisions* before continuing, so the spec stays the source of truth.

Create the branch now, named for the slice (for example `spec-a-1-names`).

## 3. Draft space

Create `draft/` at the repository root. It is in `.gitignore`; check that it still is, and never commit it. It holds work for the user to review while the code changes:

| File | Holds |
| --- | --- |
| `draft/contracts.md` | The contracts, as they stand (step 4) |
| `draft/decisions.md` | Every decision taken without the user, with its reason, so they can be reviewed after the fact |
| `draft/issues.md` | Problems found during implementation (step 10) |

`draft/` is local. Anything another agent or a reviewer needs goes in the **pull request description**, which is shared.

## 4. Contracts

Write only the boundaries and intent: interfaces, types, and method signatures whose bodies throw `NotImplementedException` (in TypeScript, `throw new Error("not implemented")`). **They must compile**, so tests can be written against them in step 7. No implementation yet.

Record their gist in `draft/contracts.md`. Do not wait for approval to continue.

## 5. Check the contracts

Each question is a reason to rework (step 6):

1. **Scope:** will they let every part of the slice, and its acceptance criteria, be done?
2. **Decisions:** do they honor every decision in the spec, including the ones made in step 2?
3. **Place:** is each in the right assembly and namespace by the layer rules in `README.md`? Folder equals namespace; no loose files at an assembly root; no `AssemblyName` or `RootNamespace` in a `.csproj`.
4. **Vocabulary:** do names use the spec's words and `DOCUMENTATION.md` §3's fixed vocabulary?
5. **Duplication:** does anything re-implement what already exists?
6. **Layers:** is any abstraction unnecessary?
7. **Purity:** could an impure function reasonably be pure, or a non-idempotent one idempotent?
8. **Failure:** is each failure reported the codebase's way? A modelling mistake is data (a problem, an `Either`), and only a programming mistake throws.
9. **Ripple:** does a change cross into DTOs, the API's generated web types (`web/src/api/schema.d.ts`), the editor, or stored documents? Each crossing is part of the slice.
10. **Concurrency:** if performance matters here, do the contracts allow safe parallel use?

## 6. Rework

Rework the contracts and return to step 5 until nothing is left. Update `draft/contracts.md` as they change, and record each decision in `draft/decisions.md`.

**Stop for the user only when** a decision would change the strategy substantially and no option clearly wins. Otherwise decide, record it, and continue.

**⟶ commit** — the contracts compile, and the draft pull request now exists. Its description summarizes the contracts.

## 7. Tests

1. **Unit tests against the contracts.** They fail now, for the right reason: `NotImplementedException`, not a compile error or a wrong assertion.
2. **Tests that prove the spec is complete,** where possible: an integration test per acceptance criterion. `Api.Test` runs over real HTTP; `Examples.Test` pins worked values.
3. **Name the rule** a test holds where the spec numbers its rules (for example `AU-23`), in a comment or the test name.
4. **README snippets** the slice adds get a case in that assembly's `ReadmeSnippets.cs`.

**⟶ commit** — a hand-off point if several agents share the spec. The pull request description says what is done and what is next.

## 8. Doc comments

Write doc comments on the contracts, following `DOCUMENTATION.md`: the relevance test, what a hover needs, concise. `dotnet build` fails on a broken `cref`, so build after.

## 9. Implement

Write the implementations until every test passes. Run the whole suite (`dotnet test`), and `cd web && npm run build` if the slice touches the web.

**Do not change a test to make it pass.** If a test is wrong, fix it, and say why in `draft/issues.md` and the pull request.

## 10. Record issues

Anything that went wrong, surprised, or was worked around goes in `draft/issues.md`, and the parts a reviewer needs go in the pull request description.

Then bring the documents up to date:

- **READMEs** of every folder the slice changed: *Guarantees*, *Surprises*, and the folder's contents.
- **Retroactive specs** whose rules this slice changed, including *Superseded by Spec …* rules that now apply, and Known Bugs it fixed.
- **`project-plan.md`:** tick what this slice finished.

**⟶ commit**, then **mark the pull request ready for review.** CI runs now. If it fails, fix it before going on.

## 11. Review for completeness and bugs

1. **Completeness:** go through the spec's slice and acceptance criteria; for each, name the test that proves it. Anything unproven gets a test or is reported.
2. **Bugs:** review the implementation for subtle bugs. Run `/code-review` on the branch at the level the user prefers, and fix what it confirms.

## 12. Review for quality

Run `/simplify` for code smells and idiomatic cleanups, and review performance yourself.

- **Fix** obvious issues with idiomatic solutions.
- **Report** everything else to the user as concerns, each with the option to add it to `project-plan.md` as future work.

**⟶ commit**, and check that CI passes. The pull request description ends with: the spec and slice, what was done, the decisions taken without the user, the issues found, and the concerns.
