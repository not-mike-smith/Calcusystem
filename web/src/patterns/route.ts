/**
 * What counts as the patterns page, in one place.
 *
 * The gallery's own section links are hashes under the same route, so the router has to accept them. Deciding
 * that in the router and spelling the links in the gallery is two answers to one question, and it cost every
 * link at the top of the page: an exact match on `#patterns` sent `#patterns-affordance` back to the editor.
 */
export const PATTERNS = "#patterns";

export const isPatterns = (hash: string): boolean => hash.startsWith(PATTERNS);

/** Where a section of the page lives. Its element carries the same id. */
export const sectionHref = (id: string): string => `${PATTERNS}-${id}`;
export const sectionId = (id: string): string => sectionHref(id).slice(1);
