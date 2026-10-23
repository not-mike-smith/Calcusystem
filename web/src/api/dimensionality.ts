import type { DimensionReport } from "./client";

/** Exponents by fundamental-dimension symbol. Absent means zero. */
export type Exponents = Record<string, number>;

/** The encoded form the wire uses: `"M1,L1,T-2"`, empty for dimensionless. */
export function encode(exponents: Exponents, order: DimensionReport[]): string {
  return order
    .filter((dimension) => (exponents[dimension.Symbol] ?? 0) !== 0)
    .map((dimension) => `${dimension.Symbol}${exponents[dimension.Symbol]}`)
    .join(",");
}

export function decode(encoded: string | null | undefined): Exponents {
  const exponents: Exponents = {};
  if (!encoded) return exponents;

  for (const entry of encoded.split(",")) {
    const match = /^([^\d-]+)(-?\d+)$/.exec(entry.trim());
    if (match) exponents[match[1]!] = Number(match[2]);
  }

  return exponents;
}

const SUPERSCRIPTS: Record<string, string> = {
  "-": "⁻", 0: "⁰", 1: "¹", 2: "²", 3: "³", 4: "⁴", 5: "⁵", 6: "⁶", 7: "⁷", 8: "⁸", 9: "⁹",
};

/** For reading: `"M1,L1,T-2"` becomes `"M·L·T⁻²"`, and dimensionless becomes a dash. */
export function format(encoded: string | null | undefined): string {
  const exponents = decode(encoded);
  const entries = Object.entries(exponents).filter(([, power]) => power !== 0);

  if (entries.length === 0) return "—";

  return entries
    .map(([symbol, power]) =>
      power === 1
        ? symbol
        : symbol + String(power).split("").map((c) => SUPERSCRIPTS[c] ?? c).join(""))
    .join("·");
}

/**
 * Named combinations an author is likely to want. A convenience of this editor only — the symbols themselves
 * come from the server, and nothing here decides what a dimensionality *means*.
 */
export const PRESETS: { name: string; encoded: string }[] = [
  { name: "Dimensionless", encoded: "" },
  { name: "Mass", encoded: "M1" },
  { name: "Length", encoded: "L1" },
  { name: "Time", encoded: "T1" },
  { name: "Temperature", encoded: "Θ1" },
  { name: "Electric current", encoded: "I1" },
  { name: "Area", encoded: "L2" },
  { name: "Volume", encoded: "L3" },
  { name: "Velocity", encoded: "L1,T-1" },
  { name: "Acceleration", encoded: "L1,T-2" },
  { name: "Force", encoded: "M1,L1,T-2" },
  { name: "Pressure", encoded: "M1,L-1,T-2" },
  { name: "Energy", encoded: "M1,L2,T-2" },
  { name: "Power", encoded: "M1,L2,T-3" },
  { name: "Density", encoded: "M1,L-3" },
  { name: "Mass flow", encoded: "M1,T-1" },
];

/** The preset this encoding is, or null where it is not one of them. */
export function presetFor(encoded: string | null | undefined, order: DimensionReport[]): string | null {
  const canonical = encode(decode(encoded), order);
  return PRESETS.find((preset) => encode(decode(preset.encoded), order) === canonical)?.name ?? null;
}
