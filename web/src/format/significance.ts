/**
 * How many figures of a number to show, and whether to show it in scientific notation.
 *
 * The rule throughout: **an uncertainty decides the precision of the value it belongs to.** Writing a value to
 * more figures than its band justifies claims a precision the measurement does not have.
 */

export type Notation = "auto" | "scientific" | "decimal";

export interface NumberStyle {
  /** Default `"auto"` — scientific only where a decimal reading would be mostly zeros. */
  notation?: Notation;
  /** Overrides the figures an uncertainty would otherwise set. Used only where there is no band. */
  sigFigs?: number;
}

/** A value and its band, rounded to agree, ready to render. */
export interface Reading {
  value: string;
  /** The band, where upper and lower agree. */
  band: string | null;
  /** Both bands, where they differ. */
  asymmetric: { upper: string; lower: string } | null;
  /** The power of ten the value and bands are expressed in, or null for a plain decimal reading. */
  exponent: number | null;
}

/** Figures to keep with no band to go on. */
const BARE = 4;

/** More than this many leading or trailing zeros in a decimal reading, and scientific is easier to read. */
const ZEROS_TOLERATED = 2;

/**
 * How many significant figures of an uncertainty to keep: two where its leading digit is 1 or 2, one
 * otherwise.
 *
 * Keeping two throughout would write ±51 as ±51, claiming a digit the measurement does not have; keeping one
 * throughout would write ±12 as ±10, overstating the band by 8%.
 */
export function figuresFor(uncertainty: number): number {
  if (!Number.isFinite(uncertainty) || uncertainty <= 0) return BARE;

  return leadingDigit(uncertainty) <= 2 ? 2 : 1;
}

/**
 * The power of ten the last figure of `uncertainty` sits in, which is where its value is rounded to as well.
 *
 * Rounding can carry the leading digit — 9.7 to one figure is 10 — so the figure count is reconsidered once
 * against the rounded number.
 */
export function placeFor(uncertainty: number): number {
  const place = (u: number) => exponentOf(u) - (figuresFor(u) - 1);

  const first = place(uncertainty);
  const rounded = roundTo(uncertainty, first);

  return rounded === 0 ? first : place(rounded);
}

/** The value and its band, each rounded to the place the band justifies. */
export function read(
  value: number,
  upper: number | null | undefined,
  lower: number | null | undefined,
  style: NumberStyle = {},
): Reading {
  const widest = Math.max(upper ?? 0, lower ?? 0);

  if (!Number.isFinite(value)) return { value: String(value), band: null, asymmetric: null, exponent: null };

  return widest > 0 && Number.isFinite(widest)
    ? banded(value, upper ?? 0, lower ?? 0, widest, style)
    : bare(value, style);
}

/** A number on its own, to a given number of figures. */
export function readBare(value: number, style: NumberStyle = {}): Reading {
  return bare(value, style);
}

function banded(
  value: number,
  upper: number,
  lower: number,
  widest: number,
  style: NumberStyle,
): Reading {
  const place = placeFor(widest);
  const shown = roundTo(value, place);
  const scientific = chooseNotation(shown, place, style.notation);
  const exponent = scientific ? exponentOf(shown === 0 ? widest : shown) : null;

  const at = (x: number) => digits(roundTo(x, place), place, exponent);
  const symmetric = roundTo(upper, place) === roundTo(lower, place);

  return {
    value: at(value),
    band: symmetric ? at(upper) : null,
    asymmetric: symmetric ? null : { upper: at(upper), lower: at(lower) },
    exponent,
  };
}

function bare(value: number, style: NumberStyle): Reading {
  const figures = style.sigFigs ?? BARE;
  const place = value === 0 ? 0 : exponentOf(value) - (figures - 1);
  const shown = roundTo(value, place);
  const scientific = chooseNotation(shown, place, style.notation);
  const exponent = scientific ? exponentOf(shown) : null;

  return { value: digits(shown, place, exponent), band: null, asymmetric: null, exponent };
}

/**
 * Scientific only where a decimal reading would be mostly zeros — more than two of them leading or trailing.
 * Judged after rounding, so the zeros counted are the ones that would actually be shown.
 */
function chooseNotation(shown: number, place: number, notation: Notation = "auto"): boolean {
  if (notation !== "auto") return notation === "scientific";
  if (shown === 0 || !Number.isFinite(shown)) return false;

  const magnitude = Math.abs(shown);

  if (magnitude < 1) return -exponentOf(magnitude) - 1 > ZEROS_TOLERATED;

  const whole = Math.abs(roundTo(shown, Math.max(place, 0))).toFixed(0);
  const trailing = whole.length - whole.replace(/0+$/, "").length;

  return trailing > ZEROS_TOLERATED;
}

/** The digits of `x`, either as a plain decimal at `place` or as a mantissa over `exponent`. */
function digits(x: number, place: number, exponent: number | null): string {
  if (exponent === null) return x.toFixed(Math.max(-place, 0));

  const mantissa = x / Math.pow(10, exponent);

  return mantissa.toFixed(Math.max(exponent - place, 0));
}

function exponentOf(x: number): number {
  return Math.floor(Math.log10(Math.abs(x)));
}

function leadingDigit(x: number): number {
  return Math.floor(Math.abs(x) / Math.pow(10, exponentOf(x)));
}

/** Rounds to a power of ten, through a string so that 1.005 at two places does not land on 1.00. */
function roundTo(x: number, place: number): number {
  if (!Number.isFinite(x)) return x;
  if (place <= 0) return Number(x.toFixed(Math.min(-place, 100)));

  const step = Math.pow(10, place);

  return Math.round(x / step) * step;
}

const SUPERSCRIPT: Record<string, string> = {
  "-": "⁻", 0: "⁰", 1: "¹", 2: "²", 3: "³", 4: "⁴", 5: "⁵", 6: "⁶", 7: "⁷", 8: "⁸", 9: "⁹",
};

/** `-4` becomes `⁻⁴`, for an exponent shown beside a power of ten. */
export function superscript(exponent: number): string {
  return String(exponent).split("").map((c) => SUPERSCRIPT[c] ?? c).join("");
}

/** Reads a typed number, to at most 12 significant figures, or null where it is not one. */
export function parseTyped(text: string): number | null {
  const trimmed = text.trim();
  if (trimmed === "") return null;

  const parsed = Number(trimmed);
  if (!Number.isFinite(parsed)) return null;

  return Number(parsed.toPrecision(12));
}
