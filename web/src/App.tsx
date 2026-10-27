import { useEffect, useMemo, useState } from "react";
import {
  units as readUnits,
  vocabulary as readVocabulary,
  type DraftProblem,
  type MeasurandReport,
  type OutcomeReport,
  type SystemDraft,
  type UnitCatalogueReport,
  type VocabularyReport,
} from "./api/client";
import { CatalogueProvider } from "./model/catalogue";
import { PosednessPanel } from "./components/PosednessPanel";
import { DraftProblems } from "./components/ProblemList";
import { ExpressionRow } from "./components/ExpressionRow";
import { RelationshipRow } from "./components/RelationshipRow";
import { VariableRow } from "./components/VariableRow";
import * as Draft from "./model/draft";
import { useAnalysis } from "./model/useAnalysis";

export function App() {
  const [draft, setDraft] = useState<SystemDraft>(Draft.load);
  const [vocabulary, setVocabulary] = useState<VocabularyReport | null>(null);
  const [units, setUnits] = useState<UnitCatalogueReport | null>(null);
  const [unreachable, setUnreachable] = useState(false);

  // Both are fixed facts about the server, fetched once and kept for the session.
  useEffect(() => {
    Promise.all([readVocabulary(), readUnits()]).then(
      ([served, catalogue]) => {
        setVocabulary(served);
        setUnits(catalogue);
      },
      () => setUnreachable(true),
    );
  }, []);

  useEffect(() => Draft.save(draft), [draft]);

  const analysis = useAnalysis(draft);

  const expressions = Draft.expressionsOf(draft);
  const variables = expressions.filter((e) => e.Type === "Variable");
  const derived = expressions.filter((e) => e.Type !== "Variable");
  const relationships = Draft.relationshipsOf(draft);

  const problemsBy = useMemo(() => byNode(analysis.problems), [analysis.problems]);
  const valueOf = useMemo(() => byId(analysis.calculation?.Values), [analysis.calculation]);
  const outcomeOf = useMemo(() => outcomesById(analysis.calculation?.Outcomes), [analysis.calculation]);
  const missing = useMemo(
    () => new Set(analysis.calculation?.MissingValueIds ?? []),
    [analysis.calculation],
  );

  const nameOf = (id: string | null | undefined): string => {
    const node = expressions.find((e) => e.Id === id);
    if (node) return Draft.labelFor(node);

    const relationship = relationships.find((r) => r.Id === id);
    return relationship?.Name?.trim() || `${relationship?.Type ?? "node"} (${id?.slice(0, 4)})`;
  };

  if (unreachable) {
    return (
      <main className="unreachable">
        <h1>The server is not answering</h1>
        <p>
          Start it with <code>dotnet run --project Api</code> and reload. This page proxies
          <code> /api</code> to <code>http://localhost:5080</code>.
        </p>
      </main>
    );
  }

  if (!vocabulary || !units) return <main className="quiet">Connecting…</main>;

  return (
    <CatalogueProvider vocabulary={vocabulary} units={units}>
    <main>
      <header>
        <input
          className="title"
          value={draft.Name ?? ""}
          placeholder="Untitled model"
          onChange={(e) => setDraft({ ...draft, Name: e.target.value })}
        />
        <input
          className="subtitle"
          value={draft.Description ?? ""}
          placeholder="what this model is for"
          onChange={(e) => setDraft({ ...draft, Description: e.target.value })}
        />
        <button type="button" onClick={() => setDraft(Draft.emptyDraft())}>New model</button>
        <span className={analysis.pending ? "pulse" : "pulse quiet"}>●</span>
      </header>

      <div className="columns">
        <div className="model">
          <section className="panel">
            <h2>
              Variables
              <button type="button" onClick={() => setDraft(Draft.addVariable(draft))}>Add</button>
            </h2>
            {variables.length === 0 && <p className="quiet">Nothing measured yet.</p>}
            {variables.map((variable) => (
              <VariableRow
                key={variable.Id}
                variable={variable}
                problems={problemsBy.get(variable.Id ?? "") ?? []}
                computed={valueOf.get(variable.Id ?? "") ?? null}
                isUnknown={missing.has(variable.Id ?? "")}
                onChange={(change) => setDraft(Draft.changeExpression(draft, variable.Id!, change))}
                onRemove={() => setDraft(Draft.removeNode(draft, variable.Id!))}
              />
            ))}
          </section>

          <section className="panel">
            <h2>
              Formulas
              <button type="button" onClick={() => setDraft(Draft.addExpression(draft))}>Add</button>
            </h2>
            {derived.length === 0 && <p className="quiet">Nothing computed yet.</p>}
            {derived.map((expression) => (
              <ExpressionRow
                key={expression.Id}
                expression={expression}
                kinds={(vocabulary.ExpressionKinds ?? []).filter((kind) => kind !== "Variable")}
                operands={expressions.filter((e) => e.Id !== expression.Id)}
                problems={problemsBy.get(expression.Id ?? "") ?? []}
                computed={valueOf.get(expression.Id ?? "") ?? null}
                onChange={(change) => setDraft(Draft.changeExpression(draft, expression.Id!, change))}
                onSlot={(index, id) => setDraft(Draft.changeSlot(draft, expression.Id!, index, id))}
                onAddSlot={() => setDraft(Draft.addSlot(draft, expression.Id!))}
                onRemoveSlot={(index) => setDraft(Draft.removeSlot(draft, expression.Id!, index))}
                onRemove={() => setDraft(Draft.removeNode(draft, expression.Id!))}
              />
            ))}
          </section>

          <section className="panel">
            <h2>
              Relationships
              <button type="button" onClick={() => setDraft(Draft.addRelationship(draft))}>Add</button>
            </h2>
            {relationships.length === 0 && <p className="quiet">Nothing asserted or required yet.</p>}
            {relationships.map((relationship) => (
              <RelationshipRow
                key={relationship.Id}
                relationship={relationship}
                vocabulary={vocabulary}
                nodes={expressions}
                problems={problemsBy.get(relationship.Id ?? "") ?? []}
                outcome={outcomeOf.get(relationship.Id ?? "") ?? null}
                onChange={(change) =>
                  setDraft(Draft.changeRelationship(draft, relationship.Id!, change))}
                onRemove={() => setDraft(Draft.removeNode(draft, relationship.Id!))}
              />
            ))}
          </section>
        </div>

        <aside>
          {analysis.message && (
            <section className="panel faulty">
              <h2>This could not be read</h2>
              <p>{analysis.message}</p>
            </section>
          )}

          <DraftProblems problems={analysis.problems} />

          <PosednessPanel decomposition={analysis.decomposition} nameOf={nameOf} />

          <section className="panel">
            <h2>Findings</h2>
            {analysis.calculation
              ? <Findings
                  complete={analysis.calculation.IsComplete ?? false}
                  hold={analysis.calculation.AllRelationshipsHold ?? false}
                  missing={[...missing].map(nameOf)}
                />
              : <p className="quiet">Not until the model builds.</p>}
          </section>
        </aside>
      </div>
    </main>
    </CatalogueProvider>
  );
}

/* Values and verdicts are separate questions: a model whose requirement is broken still computed. */
function Findings({
  complete, hold, missing,
}: { complete: boolean; hold: boolean; missing: string[] }) {
  return (
    <>
      <p className={complete ? "verdict holds" : "verdict undetermined"}>
        {complete ? "Every value resolved." : "Some values did not resolve."}
      </p>
      <p className={hold ? "verdict holds" : "verdict broken"}>
        {hold ? "Every relationship holds." : "A relationship does not hold."}
      </p>
      {missing.length > 0 && (
        <p className="names">
          <span className="quiet">Supply these and more will resolve: </span>
          {missing.map((name) => <code key={name}>{name}</code>)}
        </p>
      )}
    </>
  );
}

function byNode(problems: DraftProblem[]): Map<string, DraftProblem[]> {
  const found = new Map<string, DraftProblem[]>();

  for (const problem of problems) {
    if (!problem.NodeId) continue;
    found.set(problem.NodeId, [...(found.get(problem.NodeId) ?? []), problem]);
  }

  return found;
}

function byId(values: { Id?: string; Value?: MeasurandReport }[] | undefined) {
  return new Map((values ?? []).map((value) => [value.Id ?? "", value.Value ?? null]));
}

function outcomesById(outcomes: OutcomeReport[] | undefined) {
  return new Map((outcomes ?? []).map((outcome) => [outcome.RelationshipId ?? "", outcome]));
}
