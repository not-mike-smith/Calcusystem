import { asNumber, type Uncertainty, type Unit } from "../api/client";
import { bandToKms, show, showBand, toKms } from "./units";

/**
 * What an author has written: a quantity in KMS, and the unit they wrote it in.
 *
 * The rule every function here keeps: **the numbers on screen are derived from this, and only an author typing
 * changes them.** Choosing a unit or a band form says what the number already written means, so the quantity
 * moves and the number stays. Converting a fixed quantity into another unit is a different job, done by a
 * readout.
 */
export interface Written {
  kmsValue: number | null;
  uncertainty: Uncertainty | null;
  valueUnit: string | null;
}

/** What the two boxes read, in `unit`. */
export function shown(written: Written, unit: Unit | null): { value: number | null; band: number | null } {
  const absolute = written.uncertainty?.IsStoredAsAbs === true;
  const magnitude = written.uncertainty ? asNumber(written.uncertainty.Magnitude) : 0;

  return {
    value: written.kmsValue === null || !unit ? written.kmsValue : show(written.kmsValue, unit),
    band: !written.uncertainty
      ? null
      : absolute && unit
        ? showBand(magnitude, unit)
        : magnitude * 100,
  };
}

/**
 * Whether the band is absolute — and whether one typed next would be.
 *
 * Absolute with no band yet, because the box sits straight after a value in a named unit and
 * `8.0 MPa ± 0.3 MPa` is how that reads. The label says which it is, and switching costs a click and moves no
 * number.
 */
export const isAbsolute = (written: Written): boolean =>
  written.uncertainty === null || written.uncertainty === undefined
    ? true
    : written.uncertainty.IsStoredAsAbs === true;

/** The author typed a value, in `unit`. */
export function withValue(written: Written, unit: Unit | null, typed: number | null): Written {
  return { ...written, kmsValue: typed === null || !unit ? typed : toKms(typed, unit) };
}

/** The author typed a band, in `unit` if absolute or as a percentage if not. A sign on one is meaningless. */
export function withBand(written: Written, unit: Unit | null, typed: number | null): Written {
  if (typed === null || typed === 0) return { ...written, uncertainty: null };

  return { ...written, uncertainty: band(Math.abs(typed), isAbsolute(written), unit) };
}

/** The author chose a unit: the numbers already written are in it now. */
export function withUnit(written: Written, from: Unit | null, to: Unit): Written {
  const reading = shown(written, from);

  return {
    valueUnit: to.Symbol ?? null,
    kmsValue: reading.value === null ? null : toKms(reading.value, to),
    uncertainty: reading.band === null ? null : band(reading.band, isAbsolute(written), to),
  };
}

/** The author switched the band's form: the number already written is in the other form now. */
export function withForm(written: Written, unit: Unit | null): Written {
  if (!written.uncertainty) return written;

  const reading = shown(written, unit);

  return { ...written, uncertainty: band(reading.band ?? 0, !isAbsolute(written), unit) };
}

function band(size: number, absolute: boolean, unit: Unit | null): Uncertainty {
  return absolute && unit
    ? { Type: "SymmetricUncertainty", IsStoredAsAbs: true, Magnitude: bandToKms(size, unit) }
    : { Type: "SymmetricUncertainty", IsStoredAsAbs: false, Magnitude: size / 100 };
}
