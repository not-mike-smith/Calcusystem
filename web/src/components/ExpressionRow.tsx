import type { DraftProblem, ExpressionDraft, MeasurandReport } from "../api/client";
import { labelFor } from "../model/draft";
import { MeasurandReadout } from "../blocks/MeasurandReadout";
import { NodeProblems } from "./ProblemList";

interface Props {
  expression: ExpressionDraft;
  kinds: string[];
  operands: ExpressionDraft[];
  problems: DraftProblem[];
  computed: MeasurandReport | null;
  onChange: (change: Partial<ExpressionDraft>) => void;
  onSlot: (index: number, operandId: string | null) => void;
  onAddSlot: () => void;
  onRemoveSlot: (index: number) => void;
  onRemove: () => void;
}

/** One derived expression: a kind, and the nodes it is over. */
export function ExpressionRow({
  expression, kinds, operands, problems, computed, onChange, onSlot, onAddSlot, onRemoveSlot, onRemove,
}: Props) {
  const slots = expression.OperandIds ?? [];

  return (
    <div className={problems.length > 0 ? "node faulty" : "node"}>
      <div className="row">
        <select
          value={expression.Type ?? ""}
          onChange={(e) => onChange({ Type: (e.target.value || null) as ExpressionDraft["Type"] })}
        >
          <option value="">— no kind chosen —</option>
          {kinds.map((kind) => (
            <option key={kind} value={kind}>{kind}</option>
          ))}
        </select>

        <span className="over">over</span>

        {/* An operand slot writes the model, so it is a slot in the vocabulary's sense too. */}
        {slots.map((operandId, index) => (
          <span key={index} className="slot">
            <select value={operandId ?? ""} onChange={(e) => onSlot(index, e.target.value || null)}>
              <option value="">— empty —</option>
              {operands.map((operand) => (
                <option key={operand.Id} value={operand.Id ?? ""}>{labelFor(operand)}</option>
              ))}
            </select>
            <button
              type="button"
              className="affix toggle"
              title="Remove this operand"
              onClick={() => onRemoveSlot(index)}
            >
              ×
            </button>
          </span>
        ))}

        <button type="button" className="ghost" onClick={onAddSlot} title="Add an operand">+</button>

        <button type="button" className="ghost remove" onClick={onRemove} title="Remove">×</button>
      </div>

      <div className="row readout">
        {computed ? <MeasurandReadout value={computed} /> : <span className="measurand none">—</span>}
      </div>

      <NodeProblems problems={problems} />
    </div>
  );
}
