import { useState } from "react";
import { decode, encode, PRESETS, presetFor } from "../api/dimensionality";
import { useCatalogue } from "../model/catalogue";
import { DimensionalityView } from "./DimensionalityView";

interface Props {
  encoded: string | null | undefined;
  /** Null means the author has not chosen, which is not the same answer as dimensionless. */
  onChange: (encoded: string | null) => void;
}

/**
 * What a quantity measures, being chosen.
 *
 * Named combinations first, since that is what an author reaches for; the exponents are there underneath for
 * anything the list does not name. The names are this editor's own convenience — the symbols come from the
 * server, so a dimension added to the domain appears here with nothing changed, and nothing here decides what
 * a dimensionality means.
 */
export function DimensionalityEditor({ encoded, onChange }: Props) {
  const { dimensions } = useCatalogue();

  // Not chosen and dimensionless are different answers, and only one of them is the author's. The domain
  // refuses to guess a default here because a guess builds a different model than the one intended.
  const unchosen = encoded === null || encoded === undefined;
  const preset = unchosen ? null : presetFor(encoded, dimensions);

  const [open, setOpen] = useState(false);
  const exponents = decode(encoded);

  const choose = (name: string) => {
    if (name === "custom") return setOpen(true);
    if (name === "") return onChange(null);

    onChange(PRESETS.find((option) => option.name === name)?.encoded ?? "");
  };

  return (
    <span className="dimensionality-editor">
      <select value={unchosen ? "" : preset ?? "custom"} onChange={(event) => choose(event.target.value)}>
        <option value="">— not chosen —</option>
        {PRESETS.map((option) => (
          <option key={option.name} value={option.name}>{option.name}</option>
        ))}
        <option value="custom">{preset ? "Custom…" : "Custom"}</option>
      </select>

      {/* A rotating chevron, because the panel it opens is the only thing that closes it and nothing else
          said so. */}
      <button
        type="button"
        className="ghost reading"
        aria-expanded={open}
        title={open ? "Hide the exponents" : "Set the exponents"}
        onClick={() => setOpen(!open)}
      >
        {unchosen ? "?" : <DimensionalityView encoded={encoded} />}
        <span className={open ? "chevron open" : "chevron"} aria-hidden="true">▾</span>
      </button>

      {open && (
        <span className="exponents">
          {dimensions.map((dimension) => {
            const symbol = dimension.Symbol ?? "";
            const power = exponents[symbol] ?? 0;

            return (
              <label key={symbol} className={power === 0 ? "unused" : undefined}>
                {/* Greyed where the exponent is zero, but the spinner stays live — that is how one is added. */}
                <abbr title={dimension.Name ?? symbol}>{symbol}</abbr>
                <input
                  type="number"
                  value={power}
                  onChange={(event) =>
                    onChange(encode({ ...exponents, [symbol]: Number(event.target.value) }, dimensions))}
                />
              </label>
            );
          })}
        </span>
      )}
    </span>
  );
}
