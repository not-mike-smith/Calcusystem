import type {
  DraftProblem, ExpressionDraft, OutcomeReport, RelationshipDraft, VocabularyReport,
} from "../api/client";
import { labelFor } from "../model/draft";
import { NodeProblems } from "./ProblemList";
import { Value } from "./Value";

interface Props {
  relationship: RelationshipDraft;
  vocabulary: VocabularyReport;
  nodes: ExpressionDraft[];
  problems: DraftProblem[];
  outcome: OutcomeReport | null;
  onChange: (change: Partial<RelationshipDraft>) => void;
  onRemove: () => void;
}

/** One relationship between two nodes, and what it did when the model was last run. */
export function RelationshipRow({
  relationship, vocabulary, nodes, problems, outcome, onChange, onRemove,
}: Props) {
  const choose = (operand: "LhsId" | "RhsId") => (id: string) => onChange({ [operand]: id || null });

  return (
    <div className={problems.length > 0 ? "node faulty" : "node"}>
      <div className="row">
        <Operand value={relationship.LhsId} nodes={nodes} onChange={choose("LhsId")} />

        <select
          value={relationship.Type ?? ""}
          onChange={(e) => onChange({ Type: (e.target.value || null) as RelationshipDraft["Type"] })}
        >
          <option value="">— no kind chosen —</option>
          {(vocabulary.RelationshipKinds ?? []).map((kind) => (
            <option key={kind} value={kind}>{kind}</option>
          ))}
        </select>

        <Operand value={relationship.RhsId} nodes={nodes} onChange={choose("RhsId")} />

        <button type="button" className="ghost remove" onClick={onRemove} title="Remove">×</button>
      </div>

      <div className="row detail">
        <label>
          role
          <select
            value={relationship.SolvingRole ?? ""}
            onChange={(e) =>
              onChange({ SolvingRole: (e.target.value || null) as RelationshipDraft["SolvingRole"] })}
          >
            <option value="">— unset —</option>
            {(vocabulary.SolvingRoles ?? []).map((role) => (
              <option key={role} value={role}>{role}</option>
            ))}
          </select>
        </label>

        <label>
          agreement
          <select
            value={relationship.Agreement ?? ""}
            onChange={(e) =>
              onChange({ Agreement: (e.target.value || null) as RelationshipDraft["Agreement"] })}
          >
            <option value="">— unset —</option>
            {(vocabulary.AgreementRules ?? []).map((rule) => (
              <option key={rule} value={rule}>{rule}</option>
            ))}
          </select>
        </label>

        {relationship.Type === "SimpleComparison" && (
          <>
            <label>
              rule
              <select
                value={relationship.RuleLhs ?? ""}
                onChange={(e) =>
                  onChange({ RuleLhs: (e.target.value || null) as RelationshipDraft["RuleLhs"] })}
              >
                <option value="">— unset —</option>
                {(vocabulary.Landmarks ?? []).map((landmark) => (
                  <option key={landmark} value={landmark}>{landmark}</option>
                ))}
              </select>
            </label>
            <select
              value={relationship.RuleMustBe ?? ""}
              onChange={(e) =>
                onChange({ RuleMustBe: (e.target.value || null) as RelationshipDraft["RuleMustBe"] })}
            >
              <option value="">— unset —</option>
              {(vocabulary.Comparisons ?? []).map((comparison) => (
                <option key={comparison} value={comparison}>{comparison}</option>
              ))}
            </select>
            <select
              value={relationship.RuleRhs ?? ""}
              onChange={(e) =>
                onChange({ RuleRhs: (e.target.value || null) as RelationshipDraft["RuleRhs"] })}
            >
              <option value="">— unset —</option>
              {(vocabulary.Landmarks ?? []).map((landmark) => (
                <option key={landmark} value={landmark}>{landmark}</option>
              ))}
            </select>
          </>
        )}
      </div>

      {outcome && (
        <div className="row readout">
          <Verdict outcome={outcome} />
          <Value value={outcome.Lhs} />
          <span className="symbol-glyph">{outcome.Symbol}</span>
          <Value value={outcome.Rhs} />
        </div>
      )}

      <NodeProblems problems={problems} />
    </div>
  );
}

function Operand({
  value, nodes, onChange,
}: { value: string | null | undefined; nodes: ExpressionDraft[]; onChange: (id: string) => void }) {
  return (
    <select value={value ?? ""} onChange={(e) => onChange(e.target.value)}>
      <option value="">— empty —</option>
      {nodes.map((node) => (
        <option key={node.Id} value={node.Id ?? ""}>{labelFor(node)}</option>
      ))}
    </select>
  );
}

/* Three outcomes, not two. A relationship that could not be judged is not a relationship that failed. */
function Verdict({ outcome }: { outcome: OutcomeReport }) {
  if (outcome.IsSatisfied === null || outcome.IsSatisfied === undefined)
    return <span className="verdict undetermined">not yet judged</span>;

  if (outcome.IsSatisfied) return <span className="verdict holds">holds</span>;

  return (
    <span className="verdict broken">
      {outcome.IsViolation ? "violated" : "inconsistent"}
    </span>
  );
}
