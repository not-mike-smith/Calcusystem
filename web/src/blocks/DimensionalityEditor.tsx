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
 * A slot, because it writes the model. Named combinations first, since that is what an author reaches for;
 * the exponents are there behind *Custom* for anything the list does not name. The names are this editor's own
 * convenience — the symbols come from the server, so a dimension added to the domain appears here with
 * nothing changed, and nothing here decides what a dimensionality means.
 *
 * **The exponents are reachable only in custom mode.** A named combination is already the answer, so offering
 * to edit it underneath would be offering to make it something else by accident.
 */
export function DimensionalityEditor({ encoded, onChange }: Props) {
  const { dimensions } = useCatalogue();

  // Not chosen and dimensionless are different answers, and only one of them is the author's. The domain
  // refuses to guess a default here because a guess builds a different model than the one intended.
  const unchosen = encoded === null || encoded === undefined;
  const matching = unchosen ? null : presetFor(encoded, dimensions);

  /*
   * Custom is a mode the author is in, not a guess from whether the exponents happen to match a named
   * combination. Derived, picking Custom while the value still equalled Pressure left the select saying
   * "Pressure" with the exponents open — and tweaking a custom dimensionality into one that happens to match
   * would silently rename it.
   */
  const [chose, setChose] = useState<"preset" | "custom" | null>(null);
  const custom = chose === "custom" || (chose === null && !unchosen && matching === null);

  const [open, setOpen] = useState(false);
  const exponents = decode(encoded);

  const choose = (name: string) => {
    if (name === "custom") {
      setChose("custom");
      setOpen(true);
      return;
    }

    setChose("preset");
    setOpen(false);

    onChange(name === "" ? null : PRESETS.find((option) => option.name === name)?.encoded ?? "");
  };

  const reading = unchosen ? "?" : <DimensionalityView encoded={encoded} />;

  return (
    <span className="dimensionality-editor">
      <span className="slot">
        <select
          className="affix leading"
          value={unchosen ? "" : custom ? "custom" : matching ?? "custom"}
          onChange={(event) => choose(event.target.value)}
        >
          <option value="">— not chosen —</option>
          {PRESETS.map((option) => (
            <option key={option.name} value={option.name}>{option.name}</option>
          ))}
          <option value="custom">Custom</option>
        </select>

        {custom ? (
          <button
            type="button"
            className="reveals reading"
            aria-expanded={open}
            title={open ? "Hide the exponents" : "Set the exponents"}
            onClick={() => setOpen(!open)}
          >
            {reading}
            <span className="chevron" aria-hidden="true">▾</span>
          </button>
        ) : (
          /* A named combination is the answer; there is nothing here to press. */
          <span className="reading">{reading}</span>
        )}
      </span>

      {custom && open && (
        <span className="exponents">
          {dimensions.map((dimension) => {
            const symbol = dimension.Symbol ?? "";
            const power = exponents[symbol] ?? 0;

            return (
              <label key={symbol} className={power === 0 ? "unused" : undefined}>
                {/* Greyed where the exponent is zero, but the spinner stays live — that is how one is added. */}
                <abbr className="tells" title={dimension.Name ?? symbol}>{symbol}</abbr>
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
