import type { ExpressionDraft, RelationshipDraft, SystemDraft } from "../api/client";

/**
 * Minted here rather than by the server. An id exists before a node is valid, and the editor needs one the
 * moment a node is created so other nodes can reference it. It survives the build, so what is referenced here
 * is what comes back.
 */
export const newId = (): string => crypto.randomUUID();

export const expressionsOf = (draft: SystemDraft): ExpressionDraft[] => draft.Expressions ?? [];
export const relationshipsOf = (draft: SystemDraft): RelationshipDraft[] => draft.Relationships ?? [];

export function emptyDraft(): SystemDraft {
  return { Id: newId(), Name: "Untitled model", Description: "", Expressions: [], Relationships: [] };
}

export function addVariable(draft: SystemDraft): SystemDraft {
  // No dimensionality, rather than a dimensionless one. The domain refuses to guess a default here, because a
  // guess builds a different model than the one intended — so the editor asks instead, and the server says so
  // until the author answers.
  const variable: ExpressionDraft = {
    Id: newId(),
    Type: "Variable",
    Symbol: "",
    Dimensionality: null,
    OperandIds: [],
  };

  return { ...draft, Expressions: [...expressionsOf(draft), variable] };
}

export function addExpression(draft: SystemDraft): SystemDraft {
  // Two empty slots, because every kind takes at least one and the common ones take two. A null slot is a
  // legitimate state, so an unfinished node is nothing to apologise for.
  const expression: ExpressionDraft = { Id: newId(), Type: "Product", OperandIds: [null, null] };

  return { ...draft, Expressions: [...expressionsOf(draft), expression] };
}

export function addRelationship(draft: SystemDraft): SystemDraft {
  const relationship: RelationshipDraft = {
    Id: newId(),
    Type: "Equality",
    Agreement: "Nominal",
    SolvingRole: "Equation",
  };

  return { ...draft, Relationships: [...relationshipsOf(draft), relationship] };
}

export function changeExpression(
  draft: SystemDraft,
  id: string,
  change: Partial<ExpressionDraft>,
): SystemDraft {
  return {
    ...draft,
    Expressions: expressionsOf(draft).map((e) => (e.Id === id ? { ...e, ...change } : e)),
  };
}

export function changeRelationship(
  draft: SystemDraft,
  id: string,
  change: Partial<RelationshipDraft>,
): SystemDraft {
  return {
    ...draft,
    Relationships: relationshipsOf(draft).map((r) => (r.Id === id ? { ...r, ...change } : r)),
  };
}

/**
 * Removes a node and leaves every reference to it in place, which is what the server does. The references come
 * back as `ReferenceNotFound` problems; scrubbing them would silently discard structure the author built.
 */
export function removeNode(draft: SystemDraft, id: string): SystemDraft {
  return {
    ...draft,
    Expressions: expressionsOf(draft).filter((e) => e.Id !== id),
    Relationships: relationshipsOf(draft).filter((r) => r.Id !== id),
  };
}

export function changeSlot(draft: SystemDraft, id: string, index: number, operandId: string | null) {
  const operands = [...(expressionsOf(draft).find((e) => e.Id === id)?.OperandIds ?? [])];
  operands[index] = operandId;

  return changeExpression(draft, id, { OperandIds: operands });
}

export function addSlot(draft: SystemDraft, id: string): SystemDraft {
  const operands = expressionsOf(draft).find((e) => e.Id === id)?.OperandIds ?? [];

  return changeExpression(draft, id, { OperandIds: [...operands, null] });
}

export function removeSlot(draft: SystemDraft, id: string, index: number): SystemDraft {
  const operands = expressionsOf(draft).find((e) => e.Id === id)?.OperandIds ?? [];

  return changeExpression(draft, id, { OperandIds: operands.filter((_, i) => i !== index) });
}

/** What to call a node in a list of choices. */
export function labelFor(node: ExpressionDraft): string {
  const name = node.Symbol?.trim();
  if (name) return name;

  return node.Type ? `${node.Type} (${node.Id?.slice(0, 4)})` : `untyped (${node.Id?.slice(0, 4)})`;
}

const STORAGE_KEY = "calcusystem.draft";

/** The client owns the draft, so it is the client that keeps it. */
export function load(): SystemDraft {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored ? (JSON.parse(stored) as SystemDraft) : emptyDraft();
  } catch {
    return emptyDraft();
  }
}

export function save(draft: SystemDraft): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(draft));
  } catch {
    // A full or blocked store is not worth interrupting an edit over.
  }
}
