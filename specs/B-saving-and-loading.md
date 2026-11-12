# Spec B — Saving and loading

**Status:** Draft, 2026-11-11. Loose: it fixes the goal, the shape, and the decisions. It does not fix signatures.
**Plan:** [feature set B](../project-plan.md#b--saving-and-loading).
**Depends on:** the retroactive [draft-document spec](retroactive/draft-document.md), whose behavior B keeps; Spec A's changes to the draft document, which land first. **Unblocks:** C1 (saving and opening in the editor), G1 (composing saved systems).

---

## 1. Problem

A model exists only in one browser's `localStorage`, or in C# code. It cannot be saved to a file, sent to someone, or opened again elsewhere. The draft document has no version, so the first change to its shape would make every saved copy unreadable without warning. The [Serialization README](../Serialization/README.md#payload-compatibility) says as much: no version field, no back-compatibility, "revisit when there is a stored corpus". B creates that corpus, so B is the revisit.

## 2. Goals

1. A draft can be saved to a file and opened from one, in the editor and without a browser.
2. **Every saved document carries a version,** and a reader knows what to do with any version it is given.
3. An older document is brought up to date when it is read, without its owner doing anything, wherever that is possible.
4. A document that cannot be read is refused with every reason at once, each saying where in the document it is.
5. `localStorage` holds the same document as a file.

## 3. Non-goals

- **Preserving anything written before B.** B introduces version 1.0.0.
- **A server-side store.** The server stays stateless; the client owns its documents.
- **Built-system documents.** Kept possible (section 4.1), not built.
- **Editor polish.** B adds a minimal save and open control; C1 owns the rest.
- **Formula text** as a saved form. That is H.
- **The file extension and media type.** They carry the project's new name, so they wait for the rename chore. Until then, `.json`.

---

## 4. Behavior

### 4.1 The document

A saved document is an **envelope** around the content:

```json
{
  "Kind": "Draft",
  "Version": "1.0.0",
  "Draft": { "Id": "…", "Name": "Orifice drain time", "Expressions": [ … ], "Relationships": [ … ] }
}
```

1. `Kind` says what the document holds. `Draft` is the only kind B defines. `System` is reserved for a built-system document, so one can be added without changing how a reader starts.
2. `Version` is the document format's SemVer version (section 4.2). It versions the format, not the application.
3. **There is room for metadata** beside `Kind` and `Version`, such as when and by what a document was saved. B defines none; a field added later is a patch change.
4. The content follows the draft-document rules, with Spec A's changes. Its **behavior** is what the retroactive spec makes normative: absent means not supplied, enums are written by name, ids and half-built states survive the round trip, and so on.
5. The JSON conventions belong to `Serialization` (draft-document DF6): PascalCase on write, property names matched without regard to case on read, and values case-sensitive.

### 4.2 Versions

The level of a change is decided by **who has to do the work** of bringing an older document forward:

| Level | The change | Bringing an older document forward | Example |
| --- | --- | --- | --- |
| **Patch** | Fully backward compatible: every older document already means the same thing under the new version. | Nothing to do. | Adding an optional field whose absence already meant the right thing. |
| **Minor** | Older documents need clean-up that `Serialization` can do by itself, with no information it does not have. | A migration step in `Serialization`, run on read. | Renaming a field; splitting one field into two. |
| **Major** | Bringing a document forward needs its owner's input, or code outside `Serialization`. | Not done on read. The reader refuses, and points to the command-line tool. | Changing what a field means when the old meaning cannot be told from the document. |

**The aim is never to increment the major version.** A change that would be major should first be redesigned as a minor one, for example by adding a new field beside the old one instead of changing the old one's meaning.

**What a reader accepts:**

1. **The same major version, and no newer than the reader's own:** read, after running every minor migration step between the two, in order. Patch differences need no step.
2. **Newer than the reader, at any level:** refused (`NewerVersion`). An older reader refuses unknown properties (draft-document DD-35), so even a newer patch may hold something it cannot read. A user who knows the risk can edit the version and try.
   - **Once drafts report warnings, a newer *patch* opens with a warning** naming the properties it ignores. A newer minor or major is still refused. What a save then writes is in section 4.4.
3. **A different major version:** refused (`MajorVersion`), with a message naming the command-line tool.
4. **No version, or not SemVer:** refused (`VersionMissing`).

**A writer always writes the current version.** Reading and then saving an older document upgrades it.

### 4.3 Reading

```text
text ──parse──▶ JSON ──envelope──▶ kind, version ──migrate──▶ current JSON ──map──▶ SystemDraft
                         │                │                                  │
                         └────────── refusals, collected ────────────────────┘
```

1. **Migration runs during deserialization,** on the JSON, one minor step at a time. A step works on JSON rather than on DTO classes, because the older shapes no longer exist as classes.
2. **Reading returns `Either<SystemDraft>`** (draft-document DF2). The failure carries **every** refusal found, each with:
   - a **kind**, from a new enum: `NotJson`, `UnknownKind`, `VersionMissing`, `NewerVersion`, `MajorVersion`, and the content refusals of draft-document DD-30 to DD-38;
   - a **location**, as a JSON path (`$.Draft.Expressions[3].Uncertainty`);
   - a message for people.
3. **A refusal is not a problem.** A refusal means the document cannot be *held* as a draft. A problem means a held draft would not *build*. A document with problems opens normally, and the draft reports them as it always does.
4. **Reading never mends.** Migration steps change shape, never meaning, so a draft that did not build before migrating does not build after it either.

### 4.4 Writing

1. `Serialization` writes a draft as a document at the current version. **A writer always writes its own version,** never a newer one it read.
2. Writing always succeeds for any draft, however half-built.
3. **After opening a newer patch,** saving drops the properties the reader ignored. The loss is made visible where a person chooses to save:
   - opening warns which properties will be dropped;
   - an explicit save in the editor asks once, naming the version it will write and what it drops;
   - saving to `localStorage` does not ask, because the warning was shown at open;
   - the command-line tool's `migrate` does not prompt; it needs a flag to write a document opened from a newer patch.

### 4.5 Where it is used

1. **The API** gets two routes, both stateless:
   - **open:** takes a document's text, and returns the current draft or the refusals;
   - **save:** takes a draft, and returns the document's text.
2. **The editor:**
   - **Save** downloads the document as a file.
   - **Open** uploads a file through the open route, and replaces the current draft.
   - **`localStorage`** holds the document, not a bare draft, and reads it back through the open route when the page loads.
   - **The editor writes the envelope itself,** using the current version the API serves. It is three fields, and calling the save route on every edit would be wasteful. Reading, and so all version logic, stays on the server.
3. **A command-line tool** (section 5) does the same without a browser.

---

## 5. The command-line tool

A small `dotnet` tool over `Serialization`, `Authoring` and A's printer:

| Command | Does |
| --- | --- |
| `open <file>` | Reads the document, and prints the model with A's printer, the draft's problems, or the refusals. |
| `validate <file>` | Exits non-zero if the document is refused or its draft has problems. Usable in CI. |
| `migrate <file>` | Rewrites the document at the current version. |

It is the home for promoting a document across a **major** version when one is ever needed: a migration that asks its owner, or uses code outside `Serialization`.

It is a new project, `Cli/`, beside `Api/`. Its command name waits for the rename chore.

---

## 6. Built-system documents

Not built in B, and kept possible:

1. `Kind: "System"` is reserved (section 4.1).
2. The built-system DTOs (`Dtos.ExpressionSystem` and their mappers) keep working as they do.
3. When G1 needs a saved built system, it gets the same envelope, its own version line, and its own retroactive spec first.

## 7. What B changes in the plan

1. **Tolerant deserialization for model repair** is absorbed. A draft already holds dangling references and reports them, so opening a damaged *draft* document is repair enough. The item can close once B ships.
2. **`SystemDraft.FromSystem`** is not needed by B, because files hold drafts. It moves to G1, where editing a system loaded only in built form needs it. If something needs it sooner, that need will show up as a requirement.

---

## 8. Leans on unspecified behavior

1. The draft document: specified by the [retroactive spec](retroactive/draft-document.md).
2. The draft itself, once read: specified by the [Authoring retroactive spec](retroactive/authoring.md).
3. **The editor's use of `localStorage`** (`web/src/model/draft.ts`): when it writes, what happens when the stored text is unreadable, and that the stored draft is the one shown on load. Unspecified and untested. B changes it, so B's spec of the new behavior (section 4.5) replaces it rather than needing a retroactive one first.

---

## 9. Acceptance

1. Each Example's model saves to a document and opens to an equal draft, which builds the same system.
2. A half-built draft saves and opens exactly as it was.
3. A document one minor version older opens and migrates, through a test-only migration step until a real one exists.
4. A newer document, one of a different major version, one with no version, and one of an unknown kind are each refused with their own kind.
5. A document with several faults is refused with all of them, each with its location.
6. A camelCase document opens as the same draft as its PascalCase twin.
7. The editor's save, open and reload all go through the same document, and the command-line tool opens, validates and migrates one.

## 10. Slices

Each slice can merge on its own. The draft-document Known Bugs fold into the first two.

1. **Conventions.** The JSON conventions move into `Serialization`; property names match without regard to case (DF6).
2. **Reading returns `Either`.** Every refusal collected, with kind and location (DF2), and the content refusals fixed: duplicate ids held (DF1), unknown properties, an uncertainty with no value, a partial rule and a provenance missing a field refused (DF3, DF4, DF5, DF7).
3. **The envelope and version 1.0.0.** Kind and version checks, the migration chain with a test-only step, the writer, and a corpus of saved Example documents per version as test fixtures.
4. **The API routes and the editor:** save, open, and `localStorage` through the document.
5. **The command-line tool.**

---

## 11. Decisions

| Question | Decision |
| --- | --- |
| What a saved file holds | The draft. Built-system documents stay possible. |
| Version scheme | SemVer, by who must do the work: patch needs none, minor is a migration in `Serialization`, major needs the owner or other code. Aim never to increment the major version. |
| When migration runs | During deserialization, one minor step at a time. |
| Opening in the editor | Through an API route; the browser does not migrate. |
| `localStorage` | Holds the same versioned document as a file. |
| Who owns the UI | B: the mechanism and a minimal control. C1: the polish. |
| Documents before B | Not preserved. B introduces 1.0.0. |
| File extension | Waits for the rename chore. |
| A tool without a browser | A command-line tool, also the home for major-version promotion: a new `Cli/` project beside `Api/`. |
| Q1: envelope or flat | The envelope, which also leaves room for metadata. |
| Q2: newer patches | Once warnings exist, a newer patch opens with a warning. |
| Q3: who writes the envelope in the browser | The editor, with the version the API serves. Reading stays on the server. |
| Q4: where the tool lives | `Cli/`, beside `Api/`. Its command name waits for the rename. |
| Q5: `FromSystem` | Moves to G1. |
| Q6: testing migrations | A test-only step, and saved Example documents per version as fixtures. |
| Q7: saving after opening a newer patch | The writer writes its own version, dropping the ignored properties. Opening warns; an explicit save asks once; `localStorage` and `migrate` do not ask, and `migrate` needs a flag. |

## 12. Open questions

None.
