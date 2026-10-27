import { asNumber, type DimensionReport, type DimensionalityUnits, type Unit } from "../api/client";
import { decode, encode } from "../api/dimensionality";

/**
 * Converting between KMS and a unit, using the two numbers the server serves for it.
 *
 * Every unit is affine in KMS, so a slope and an offset are the whole of its conversion. The server measures
 * them from the unit's own conversion method and tests that applying them reproduces it, so this is the one
 * place the arithmetic is written on this side and it is checked against `Measurement` rather than trusted.
 */

const slopeOf = (unit: Unit) => asNumber(unit.Slope);
const offsetOf = (unit: Unit) => asNumber(unit.Offset);

/** A KMS value as it reads in this unit. */
export const show = (kms: number, unit: Unit): number => slopeOf(unit) * kms + offsetOf(unit);

/** A value typed in this unit, as KMS. */
export const toKms = (shown: number, unit: Unit): number => (shown - offsetOf(unit)) / slopeOf(unit);

/**
 * A band as it reads in this unit — **slope only, never the offset**. A band is a difference, and ±1 K read
 * with the offset would come out as −457.87 °F instead of 1.8.
 */
export const showBand = (kms: number, unit: Unit): number => Math.abs(slopeOf(unit)) * kms;

/** A band typed in this unit, as KMS. */
export const bandToKms = (shown: number, unit: Unit): number => shown / Math.abs(slopeOf(unit));

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
