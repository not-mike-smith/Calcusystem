---
name: spec-review
description: Review a feature-set spec in specs/ as if about to write code against it — raise clarifying questions, architectural concerns, and say whether there is enough to draft interfaces and contracts. Use when the user asks for a spec review, or to check a spec is ready to build from.
argument-hint: <path to spec, e.g. specs/A-building-a-model.md>
---

# Spec review

Review the spec at `$ARGUMENTS` (if none is given, ask which file in `specs/`) as the engineer who has to implement it next. The output is questions and findings for the user to decide on. **Do not edit the spec** unless the user asks; record decisions only after they answer.

## 1. Read before judging

1. The spec, in full, including its *Decisions* and *Open questions*.
2. Its feature set in `project-plan.md`: the goal, the dependencies, and what other feature sets expect from it.
3. **The code it touches.** For every type, member, enum, DTO, route or editor file the spec names or implies, open it and check what exists today. A finding that is not grounded in the code is a guess; say so if it is one.
4. The READMEs of the folders involved, especially their *Guarantees* and *Surprises*.

## 2. What to look for

Work through each lens. Skip a lens that finds nothing; do not pad.

1. **Contradictions** — two parts of the spec, or the spec and a recorded decision, that cannot both hold.
2. **Missing concepts** — something the spec relies on that does not exist in the code and is not listed as work (a type, a field, a problem kind, a lookup).
3. **Mismatches with the code** — the spec assumes a shape the code does not have (a property that is an instance member, a field that is settable, an interface whose meaning conflicts).
4. **Edge cases** — empty, zero, one, negative, duplicate, missing, out of order, and Unicode text. Ask what each should do.
5. **Mutability and timing** — what can change after construction, and whether an invariant (uniqueness, membership) can be broken afterward. Whether a check is atomic, or can fail halfway.
6. **Where things live** — which assembly and namespace each new piece belongs in, by the layer rules in the root `README.md`. Name clashes with existing types.
7. **Ripple** — every layer a change crosses: domain, snapshots, drafts, serialization DTOs, API reports, the generated web types, the editor, `localStorage` drafts, Examples, `ReadmeSnippets`.
8. **Interaction with other feature sets** — whether a decision here constrains or pre-decides another feature set. Say which one.
9. **Leans on unspecified behavior** — existing behavior the spec assumes but nothing specifies. Per the plan's *Retroactive specs* chore, if there is a lot of it, say that the retroactive spec should come first.
10. **Inside-out design** — whether the spec asks the person using the feature to think about the code model's internals. Flag it; that is what split H out of A.

## 3. Output

Answer in the conversation, not in a file. Hierarchical and numbered so the user can answer by number.

1. **Gaps in the spec** — contradictions and missing concepts. These block implementation.
2. **Clarifying questions** — grouped by topic. Each one names the code it is grounded in and gives a *lean* (a recommended answer with its reason) where you have one.
3. **Architectural changes** — proposals that move responsibilities, add a concept, or change a layer. A small table of options with *for* and *against* when there is a real choice.
4. **Readiness for contracts** — per slice in the spec: *ready*, *ready given the leans*, or *blocked on* specific question numbers. Offer to draft the contracts for the ready slices.

Keep each item to a few lines. Lead with the findings that change the most code.
