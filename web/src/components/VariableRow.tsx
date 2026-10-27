import type { DimensionReport, DraftProblem, ExpressionDraft, MeasurandReport } from "../api/client";
import { asNumber } from "../api/client";
import { DimensionalityEditor } from "./DimensionalityEditor";
import { NodeProblems } from "./ProblemList";
import { Value } from "./Value";

interface Props {
  variable: ExpressionDraft;
  dimensions: DimensionReport[];
  problems: DraftProblem[];
  computed: MeasurandReport | null;
  isUnknown: boolean;
  onChange: (change: Partial<ExpressionDraft>) => void;
  onRemove: () => void;
}

/** One variable: what it is called, what it measures, and what it is worth if anyone has said. */
export function VariableRow({
  variable, dimensions, problems, computed, isUnknown, onChange, onRemove,
}: Props) {
  const percent = variable.Uncertainty?.Magnitude;

  const setValue = (text: string) =>
    onChange({ KmsValue: text.trim() === "" ? null : Number(text) });

  // Written as a fraction rather than an absolute, so the band follows the value when the value changes.
  const setUncertainty = (text: string) => {
    const fraction = Number(text) / 100;

    onChange({
      Uncertainty: text.trim() === "" || fraction === 0
        ? null
        : { Type: "SymmetricUncertainty", IsStoredAsAbs: false, Magnitude: fraction },
    });
  };

  return (
    <div className={problems.length > 0 ? "node faulty" : "node"}>
      <div className="row">
        <input
          className="symbol"
          placeholder="name"
          value={variable.Symbol ?? ""}
          onChange={(e) => onChange({ Symbol: e.target.value })}
        />

        <DimensionalityEditor
          value={variable.Dimensionality}
          dimensions={dimensions}
          onChange={(encoded) => onChange({ Dimensionality: encoded })}
        />

        <input
          className="number"
          type="number"
          placeholder="unset"
          value={variable.KmsValue === null || variable.KmsValue === undefined
            ? ""
            : asNumber(variable.KmsValue)}
          onChange={(e) => setValue(e.target.value)}
        />

        <label className="tolerance">
          ±
          <input
            className="number"
            type="number"
            placeholder="0"
            value={percent === null || percent === undefined ? "" : asNumber(percent) * 100}
            onChange={(e) => setUncertainty(e.target.value)}
          />
          %
        </label>

        <button type="button" className="ghost remove" onClick={onRemove} title="Remove">×</button>
      </div>

      <div className="row readout">
        {isUnknown
          ? <span className="unknown">unknown — nothing has given this a value</span>
          : <Value value={computed} />}
      </div>

      <NodeProblems problems={problems} />
    </div>
  );
}
