import { asNumber, type DimensionReport, type DimensionalityUnits, type Unit } from "../api/client";
import { decode, encode } from "../api/dimensionality";

/**
 * Converting between KMS and a unit, with the two numbers the server serves for it.
 *
 * These are `UnitOfMeasure`'s own `KmsConversionFactor` and `ZeroOffset`, and the four functions below are
 * `ConvertFromKmsValue` and `ConvertToKmsValue` spelled the same way, so the two sides can be read against each
 * other. An inverted factor is the mistake to watch for, and it looks right at 1 — `UnitCatalogueTests` applies
 * these formulas back for every unit served, in both directions, at values either side of zero.
 */

const factorOf = (unit: Unit) => asNumber(unit.KmsConversionFactor);
const zeroOf = (unit: Unit) => asNumber(unit.ZeroOffset);

/** A KMS value as it reads in this unit. */
export const show = (kms: number, unit: Unit): number => kms / factorOf(unit) - zeroOf(unit);

/** A value typed in this unit, as KMS. */
export const toKms = (shown: number, unit: Unit): number => (shown + zeroOf(unit)) * factorOf(unit);

/**
 * A band as it reads in this unit — **the factor alone, never the offset**. A band is a difference, and ±1 K
 * read with the offset would come out as −457.87 °F instead of 1.8.
 */
export const showBand = (kms: number, unit: Unit): number => kms / factorOf(unit);

/** A band typed in this unit, as KMS. */
export const bandToKms = (shown: number, unit: Unit): number => shown * factorOf(unit);

/** What a band in this unit is called — `Δ°C` rather than `°C` where the two differ. */
export const bandSymbol = (unit: Unit): string => unit.DeltaSymbol ?? unit.Symbol ?? "";

/** The unit a KMS value is already in, which is what to show in when nobody has chosen. */
export function baseUnit(units: DimensionalityUnits | null | undefined): Unit | null {
  if (!units) return null;

  return units.Units?.find((unit) => unit.Symbol === units.BaseSymbol) ?? units.Units?.[0] ?? null;
}

export function unitBySymbol(
  units: DimensionalityUnits | null | undefined,
  symbol: string | null | undefined,
): Unit | null {
  return units?.Units?.find((unit) => unit.Symbol === symbol) ?? null;
}

/**
 * The canonical spelling of an encoded dimensionality, so a lookup matches whatever order it was written in.
 * The server's own keys are canonical; a hand-edited draft's may not be.
 */
export function canonical(encoded: string | null | undefined, dimensions: DimensionReport[]): string {
  return encode(decode(encoded), dimensions);
}
