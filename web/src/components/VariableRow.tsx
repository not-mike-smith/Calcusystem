import type { DraftProblem, ExpressionDraft, MeasurandReport } from "../api/client";
import { DimensionalityEditor } from "../blocks/DimensionalityEditor";
import { MeasurandEditor, type MeasurandEdit } from "../blocks/MeasurandEditor";
import { MeasurandReadout } from "../blocks/MeasurandReadout";
import { NodeProblems } from "./ProblemList";

interface Props {
  variable: ExpressionDraft;
  problems: DraftProblem[];
  computed: MeasurandReport | null;
  isUnknown: boolean;
  onChange: (change: Partial<ExpressionDraft>) => void;
  onRemove: () => void;
}

/** One variable: what it is called, what it measures, and what it is worth if anyone has said. */
export function VariableRow({ variable, problems, computed, isUnknown, onChange, onRemove }: Props) {
  const edit = (measurand: MeasurandEdit) =>
    onChange({ KmsValue: measurand.kmsValue, Uncertainty: measurand.uncertainty });

  return (
    <div className={problems.length > 0 ? "node faulty" : "node"}>
      <div className="row">
        <input
          className="symbol"
          placeholder="name"
          value={variable.Symbol ?? ""}
          onChange={(event) => onChange({ Symbol: event.target.value })}
        />

        <DimensionalityEditor
          encoded={variable.Dimensionality}
          onChange={(encoded) => onChange({ Dimensionality: encoded })}
        />

        <MeasurandEditor
          dimensionality={variable.Dimensionality}
          kmsValue={variable.KmsValue === null || variable.KmsValue === undefined
            ? null
            : Number(variable.KmsValue)}
          uncertainty={variable.Uncertainty}
          onChange={edit}
        />

        <button type="button" className="ghost remove" onClick={onRemove} title="Remove">×</button>
      </div>

      {/* A variable nobody has valued is not a variable with no value: the first is what the solver is for. */}
      {isUnknown && (
        <div className="row readout">
          <span className="unknown">unknown — nothing has given this a value</span>
        </div>
      )}

      {!isUnknown && computed && (
        <div className="row readout">
          <MeasurandReadout
            value={computed}
            storedAs={variable.Uncertainty
              ? (variable.Uncertainty.IsStoredAsAbs ? "absolute" : "relative")
              : null}
          />
        </div>
      )}

      <NodeProblems problems={problems} />
    </div>
  );
}
