import { useState } from "react";
import { asNumber, type Uncertainty } from "../api/client";
import {
  isAbsolute, shown, withBand, withForm, withUnit, withValue, type Written,
} from "../format/editing";
import { parseTyped } from "../format/significance";
import { bandSymbol, baseUnit, unitBySymbol } from "../format/units";
import { useCatalogue } from "../model/catalogue";

/** What an author has written. The number on screen is derived from it; see `format/editing.ts`. */
export type MeasurandEdit = Written;

interface Props {
  /** Fixes which units are offered. Null until the author has chosen what the quantity measures. */
  dimensionality: string | null | undefined;
  kmsValue: number | null | undefined;
  uncertainty: Uncertainty | null | undefined;
  valueUnit: string | null | undefined;
  onChange: (edit: MeasurandEdit) => void;
}

/** Beyond this, a symbol will not sit in the band's label and the ditto mark stands in for it. */
const SYMBOL_FITS = 5;

/**
 * A value being written: a number, the unit it is in, and a band either absolute or relative.
 *
 * **Every control here states something; none of them re-reads it.** Selecting a unit means *the number I typed
 * is in these units*, so the number stays put and the quantity changes. Filling the control left to right
 * would otherwise move a number the author had just entered. `MeasurandReadout` is the one that converts.
 */
export function MeasurandEditor({
  dimensionality, kmsValue, uncertainty, valueUnit, onChange,
}: Props) {
  const { unitsFor } = useCatalogue();
  const units = unitsFor(dimensionality);

  const unit = unitBySymbol(units, valueUnit) ?? baseUnit(units);

  const written: Written = {
    kmsValue: kmsValue === null || kmsValue === undefined ? null : asNumber(kmsValue),
    uncertainty: uncertainty ?? null,
    valueUnit: unit?.Symbol ?? null,
  };

  const reading = shown(written, unit);
  const absolute = isAbsolute(written);

  // Reads the form a band *would* take as well as the one it has, so an empty box is not labelled `%` while
  // typing in it would give an absolute band.
  const label = absolute && unit ? bandSymbol(unit) : "%";
  const fits = label.length <= SYMBOL_FITS;

  const chooseUnit = (symbol: string) => {
    const chosen = unitBySymbol(units, symbol);
    if (chosen) onChange(withUnit(written, unit, chosen));
  };

  return (
    <span className="measurand-editor">
      <span className="field">
        <Typed
          value={reading.value}
          placeholder="unset"
          onCommit={(typed) => onChange(withValue(written, unit, typed))}
        />
        <select
          className="affix"
          value={unit?.Symbol ?? ""}
          disabled={!units}
          onChange={(event) => chooseUnit(event.target.value)}
          title={units ? "The unit this number is written in" : "Choose what this measures first"}
        >
          {units
            ? (units.Units ?? []).map((choice) => (
                <option key={choice.Symbol} value={choice.Symbol ?? ""}>{choice.Symbol}</option>
              ))
            : <option value="">—</option>}
        </select>
      </span>

      <span className="pm">±</span>

      <span className="field">
        <Typed
          value={reading.band}
          placeholder="0"
          onCommit={(typed) => onChange(withBand(written, unit, typed))}
        />
        <button
          type="button"
          className="affix toggle"
          onClick={() => onChange(withForm(written, unit))}
          disabled={!uncertainty}
          title={absolute
            ? `Absolute, in ${unit ? bandSymbol(unit) : "the same unit"} — click for a percentage`
            : "A percentage of the value — click for an absolute amount"}
        >
          {/* ″ means "the same unit as above", for a symbol too long to sit here. */}
          {fits ? label : "″"}
        </button>
      </span>
    </span>
  );
}

/**
 * A number the author types, kept as text while they type it.
 *
 * Writing back on every keystroke would renumber the box under them — a half-typed `1.2e` is not a number, and
 * a committed `1.2` would come back formatted.
 */
function Typed({
  value, placeholder, onCommit,
}: { value: number | null; placeholder: string; onCommit: (value: number | null) => void }) {
  const [typing, setTyping] = useState<string | null>(null);

  // The plain spelling of the number, not the display formatter's: a field being edited should read back what
  // was typed, and `1000` formatted for display is `1.000×10³`.
  const shown = typing ?? (value === null ? "" : String(Number(value.toPrecision(12))));

  const commit = () => {
    if (typing === null) return;

    onCommit(parseTyped(typing));
    setTyping(null);
  };

  return (
    <input
      className="number"
      inputMode="decimal"
      placeholder={placeholder}
      value={shown}
      onChange={(event) => setTyping(event.target.value)}
      onBlur={commit}
      onKeyDown={(event) => event.key === "Enter" && commit()}
    />
  );
}
