import { useState } from "react";
import { asNumber, type Uncertainty } from "../api/client";
import { parseTyped } from "../format/significance";
import { bandSymbol, bandToKms, baseUnit, show, showBand, toKms, unitBySymbol } from "../format/units";
import { useCatalogue } from "../model/catalogue";
import type { BandForm } from "./MeasurandView";

export interface MeasurandEdit {
  kmsValue: number | null;
  uncertainty: Uncertainty | null;
}

interface Props {
  /** Fixes which units are offered. Null until the author has chosen what the quantity measures. */
  dimensionality: string | null | undefined;
  kmsValue: number | null | undefined;
  uncertainty: Uncertainty | null | undefined;
  onChange: (edit: MeasurandEdit) => void;
}

/**
 * A value being written: in a unit of the author's choosing, with a band either absolute or relative.
 *
 * Everything is held in KMS. The unit is a reading, not a fact about the model — nothing on the wire records
 * which unit a value was typed in.
 */
export function MeasurandEditor({ dimensionality, kmsValue, uncertainty, onChange }: Props) {
  const { unitsFor } = useCatalogue();
  const units = unitsFor(dimensionality);

  const [symbol, setSymbol] = useState<string | null>(null);
  const unit = unitBySymbol(units, symbol) ?? baseUnit(units);

  const storedAs: BandForm = uncertainty?.IsStoredAsAbs ? "absolute" : "relative";
  const magnitude = uncertainty ? asNumber(uncertainty.Magnitude) : 0;

  const kms = kmsValue === null || kmsValue === undefined ? null : asNumber(kmsValue);

  const bandInUnit = unit && uncertainty?.IsStoredAsAbs
    ? showBand(magnitude, unit)
    : magnitude * 100;

  const write = (edit: Partial<MeasurandEdit>) =>
    onChange({ kmsValue: kms, uncertainty: uncertainty ?? null, ...edit });

  const writeValue = (typed: number | null) =>
    write({ kmsValue: typed === null || !unit ? typed : toKms(typed, unit) });

  const writeBand = (typed: number | null) => {
    if (typed === null || typed === 0) return write({ uncertainty: null });

    // Negative is meaningless and would invert the band, so it is refused at the input rather than stored.
    const size = Math.abs(typed);

    write({
      uncertainty: storedAs === "absolute" && unit
        ? { Type: "SymmetricUncertainty", IsStoredAsAbs: true, Magnitude: bandToKms(size, unit) }
        : { Type: "SymmetricUncertainty", IsStoredAsAbs: false, Magnitude: size / 100 },
    });
  };

  /* Switching form keeps the band the same size rather than the same number, so the value's interval does not
     move when the author only changed how they want to write it. */
  const switchForm = (form: BandForm) => {
    if (form === storedAs || !uncertainty) return;

    const absoluteKms = storedAs === "absolute" ? magnitude : magnitude * Math.abs(kms ?? 0);

    write({
      uncertainty: form === "absolute"
        ? { Type: "SymmetricUncertainty", IsStoredAsAbs: true, Magnitude: absoluteKms }
        : {
            Type: "SymmetricUncertainty",
            IsStoredAsAbs: false,
            Magnitude: kms ? absoluteKms / Math.abs(kms) : 0,
          },
    });
  };

  return (
    <span className="measurand-editor">
      <Typed
        value={kms === null || !unit ? null : show(kms, unit)}
        placeholder="unset"
        onCommit={writeValue}
      />

      <select
        className="unit-choice"
        value={unit?.Symbol ?? ""}
        disabled={!units}
        onChange={(event) => setSymbol(event.target.value)}
        title={units ? "The unit this value is written in" : "Choose what this measures first"}
      >
        {units
          ? (units.Units ?? []).map((choice) => (
              <option key={choice.Symbol} value={choice.Symbol ?? ""}>{choice.Symbol}</option>
            ))
          : <option value="">—</option>}
      </select>

      <span className="band-choice">
        <button
          type="button"
          className={storedAs === "absolute" ? "form chosen" : "form"}
          onClick={() => switchForm("absolute")}
          title="Keep the band as an absolute amount"
        >
          ±
        </button>
        <button
          type="button"
          className={storedAs === "relative" ? "form chosen" : "form"}
          onClick={() => switchForm("relative")}
          title="Keep the band as a fraction of the value"
        >
          %
        </button>
      </span>

      <Typed
        value={uncertainty ? bandInUnit : null}
        placeholder="0"
        onCommit={writeBand}
      />

      <i className="unit">
        {storedAs === "absolute" ? (unit ? bandSymbol(unit) : "") : "%"}
      </i>
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

  // The plain spelling of the number, not the display formatter's: a field the author is editing should read
  // back what they typed, and `1000` formatted for display is `1.000×10³`.
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
