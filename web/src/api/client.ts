import type { components } from "./schema";

type Schemas = components["schemas"];

export type SystemDraft = Schemas["SystemDraft"];
export type ExpressionDraft = Schemas["ExpressionDraft"];
export type RelationshipDraft = Schemas["RelationshipDraft"];
export type DraftProblem = Schemas["DraftProblem"];
export type DraftProblemKind = Schemas["DraftProblemKind"];
export type ExpressionDraftType = Schemas["ExpressionDraftType"];
export type BinaryOperatorType = Schemas["BinaryOperatorType"];
export type CalculationReport = Schemas["CalculationReport"];
export type DecompositionReport = Schemas["DecompositionReport"];
export type VocabularyReport = Schemas["VocabularyReport"];
export type MeasurandReport = Schemas["MeasurandReport"];
export type OutcomeReport = Schemas["OutcomeReport"];
export type DimensionReport = Schemas["DimensionReport"];
export type UnitCatalogueReport = Schemas["UnitCatalogueReport"];
export type DimensionalityUnits = Schemas["DimensionalityUnitsReport"];
export type Unit = Schemas["UnitReport"];
export type Uncertainty = Schemas["Uncertainty"];

/** What came back: the answer, or the problems that stopped it. */
export type Answer<TValue> =
  | { ok: true; value: TValue }
  | { ok: false; message: string; problems: DraftProblem[] };

/**
 * The one place a response is unwrapped. A 422 carries every `DraftProblem` behind an unbuildable draft under
 * `problems`; a 400 means the payload was not a draft at all and names no node.
 */
async function post<TValue>(route: string, draft: SystemDraft): Promise<Answer<TValue>> {
  const response = await fetch(`/api/drafts/${route}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(draft),
  });

  if (response.ok) return { ok: true, value: (await response.json()) as TValue };

  const problem = (await response.json().catch(() => ({}))) as {
    detail?: string;
    problems?: DraftProblem[];
  };

  return {
    ok: false,
    message: problem.detail ?? response.statusText,
    problems: problem.problems ?? [],
  };
}

export const calculate = (draft: SystemDraft) => post<CalculationReport>("calculation", draft);
export const decompose = (draft: SystemDraft) => post<DecompositionReport>("decomposition", draft);
export const build = (draft: SystemDraft) => post<unknown>("build", draft);

async function get<TValue>(route: string, what: string): Promise<TValue> {
  const response = await fetch(route);
  if (!response.ok) throw new Error(`The ${what} could not be read: ${response.status}`);

  return (await response.json()) as TValue;
}

export const vocabulary = () => get<VocabularyReport>("/api/vocabulary", "vocabulary");

/** Fetched once and kept: it is large, and it changes only when the library does. */
export const units = () => get<UnitCatalogueReport>("/api/units", "unit catalogue");

/**
 * A served number as a number. The wire type admits a string because a double can be written as `NaN` or
 * `Infinity`, which a calculation can genuinely produce.
 */
export function asNumber(value: number | string | null | undefined): number {
  return typeof value === "number" ? value : Number(value);
}
