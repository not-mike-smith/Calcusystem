import { useState } from "react";
import type { DimensionReport } from "../api/client";
import { decode, encode, format, presetFor, PRESETS } from "../api/dimensionality";

interface Props {
  value: string | null | undefined;
  dimensions: DimensionReport[];
  onChange: (encoded: string | null) => void;
}

/**
 * A dimensionality, by name where it has one and by exponents where it does not.
 *
 * The symbols come from the server, so a dimension added to the domain appears here with nothing changed. The
 * named combinations are this editor's own convenience; none of them decides what a dimensionality means.
 */
export function DimensionalityEditor({ value, dimensions, onChange }: Props) {
  // Not chosen and dimensionless are different answers, and only one of them is the author's.
  const unchosen = value === null || value === undefined;
  const preset = unchosen ? null : presetFor(value, dimensions);
  const [open, setOpen] = useState(false);
  const exponents = decode(value);

  const choose = (name: string) => {
    if (name === "custom") return setOpen(true);
    if (name === "") return onChange(null);
    onChange(PRESETS.find((p) => p.name === name)?.encoded ?? "");
  };

  const setExponent = (symbol: string, power: number) =>
    onChange(encode({ ...exponents, [symbol]: power }, dimensions));

  return (
    <div className="dimensionality">
      <select
        value={unchosen ? "" : (preset ?? "custom")}
        onChange={(e) => choose(e.target.value)}
      >
        <option value="">— not chosen —</option>
        {PRESETS.map((p) => (
          <option key={p.name} value={p.name}>{p.name}</option>
        ))}
        <option value="custom">{preset ? "Custom…" : `Custom — ${format(value)}`}</option>
      </select>

      <button
        type="button"
        className="ghost"
        title="Set the exponents directly"
        onClick={() => setOpen(!open)}
      >
        {unchosen ? "?" : format(value)}
      </button>

      {open && (
        <div className="exponents">
          {dimensions.map((dimension) => (
            <label key={dimension.Symbol} title={dimension.Name ?? undefined}>
              <span>{dimension.Symbol}</span>
              <input
                type="number"
                value={exponents[dimension.Symbol!] ?? 0}
                onChange={(e) => setExponent(dimension.Symbol!, Number(e.target.value))}
              />
            </label>
          ))}
        </div>
      )}
    </div>
  );
}
