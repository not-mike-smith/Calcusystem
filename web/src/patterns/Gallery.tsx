import { useEffect, useState, type ReactNode } from "react";
import { DimensionalityEditor } from "../blocks/DimensionalityEditor";
import { DimensionalityView } from "../blocks/DimensionalityView";
import { MeasurandReadout } from "../blocks/MeasurandReadout";
import { MeasurandView } from "../blocks/MeasurandView";
import { DO, WRONG, pressure, temperature, type Example } from "./examples";
import {
  CHANNELS, DECISIONS, OPEN, PATTERNS, PRINCIPLES, type Pattern,
} from "./language";
import { sectionHref, sectionId } from "./route";

/**
 * The pattern language, rendered.
 *
 * `PATTERNS.md` is the written reference and this is the live one: every mark in the vocabulary beside the
 * component that uses it, with the reasons and the rejected alternatives it was chosen over. Prose about a
 * mark can go stale; a page that renders it cannot.
 *
 * To refine a pattern, read its rule here, look at the thing beside it, then read what it was chosen over.
 */
/**
 * One list, so the contents and the sections cannot disagree. Two lists is how every link at the top of this
 * page came to point at an id that was not on it.
 */
export const SECTIONS: { id: string; nav: string; title: string; body: () => ReactNode }[] = [
  { id: "principles", nav: "Principles", title: "Principles", body: () => <Principles /> },
  {
    id: "affordance",
    nav: "Affordance",
    title: "Affordance — what happens if I touch this",
    body: () => <Marks vocabulary="affordance" />,
  },
  {
    id: "status",
    nav: "Status",
    title: "Status — what is true about this",
    body: () => <Marks vocabulary="status" />,
  },
  { id: "channels", nav: "Channels", title: "Channels — one vocabulary each", body: () => <Channels /> },
  { id: "decisions", nav: "Decisions", title: "Decisions", body: () => <Decisions /> },
  { id: "blocks", nav: "Blocks", title: "Blocks, in every state", body: () => <Blocks /> },
  { id: "open", nav: "Open", title: "Not settled", body: () => <Open /> },
];

export function Gallery() {
  return (
    <main className="gallery">
      <header>
        <span className="title">Patterns</span>
        <span className="subtitle">the mark vocabulary, rendered</span>
        <a className="back" href="#">← the editor</a>
      </header>

      <nav className="contents">
        {SECTIONS.map((section) => (
          <a key={section.id} href={sectionHref(section.id)}>{section.nav}</a>
        ))}
      </nav>

      <Scratchpad />

      {SECTIONS.map((section) => (
        <section key={section.id} className="panel" id={sectionId(section.id)}>
          <h2>{section.title}</h2>
          {section.body()}
        </section>
      ))}
    </main>
  );
}

function Principles() {
  return (
    <>
      {PRINCIPLES.map((principle) => (
        <article key={principle.id} className="principle">
          <h3>{principle.title}</h3>
          <p>{principle.text}</p>
        </article>
      ))}
    </>
  );
}

function Marks({ vocabulary }: { vocabulary: Pattern["vocabulary"] }) {
  return (
    <>
      {PATTERNS.filter((pattern) => pattern.vocabulary === vocabulary).map((pattern) => (
        <Mark key={pattern.id} pattern={pattern} />
      ))}
    </>
  );
}

function Mark({ pattern }: { pattern: Pattern }) {
  const [why, setWhy] = useState(false);
  const right = DO[pattern.id];
  const wrong = pattern.rejected.filter((alternative) => alternative.id && WRONG[alternative.id]);

  return (
    <article className="mark-entry">
      <div className="mark-head">
        <code>{pattern.name}</code>
        <span className="consequence">{pattern.consequence}</span>
        <span className="note">{pattern.mark} · shown {pattern.shown}</span>
      </div>

      <p className="mark-rule">{pattern.rule}</p>

      <div className="sheet">
        <Panel verdict="do" caption="as it should be" example={right} />

        {wrong.map((alternative) => (
          <Panel
            key={alternative.id}
            verdict="dont"
            caption={alternative.what}
            because={alternative.because}
            example={WRONG[alternative.id!]}
          />
        ))}
      </div>

      {(pattern.why.length > 0 || pattern.rejected.length > wrong.length) && (
        <>
          <button type="button" className="reveals" aria-expanded={why} onClick={() => setWhy(!why)}>
            why this one
            <span className="chevron" aria-hidden="true">▾</span>
          </button>

          {why && (
            <div className="mark-why">
              {pattern.why.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}

              {/* The ones that cannot be shown, only argued. */}
              <dl className="rejected">
                {pattern.rejected
                  .filter((alternative) => !alternative.id || !WRONG[alternative.id])
                  .map((alternative) => (
                    <div key={alternative.what}>
                      <dt>{alternative.what}</dt>
                      <dd>{alternative.because}</dd>
                    </div>
                  ))}
              </dl>

              {pattern.open && (
                <ul className="unsettled">
                  {pattern.open.map((question) => <li key={question}>{question}</li>)}
                </ul>
              )}
            </div>
          )}
        </>
      )}
    </article>
  );
}

/** One side of a sheet: the thing, what it is, and the code that makes it. */
function Panel({
  verdict, caption, because, example,
}: {
  verdict: "do" | "dont";
  caption: string;
  because?: string;
  example: Example | undefined;
}) {
  if (!example) return null;

  return (
    <section className={`panel-${verdict}`}>
      <h4>
        <span className="verdict-mark" aria-hidden="true">{verdict === "do" ? "✓" : "✗"}</span>
        {verdict === "do" ? "Do" : "Don't"}
        <span className="caption">{caption}</span>
      </h4>

      <div className="specimen">{example.render()}</div>

      {because && <p className="because">{because}</p>}

      <pre><code>{example.code}</code></pre>
    </section>
  );
}

/**
 * A stylesheet of your own, injected after the app's so it wins, and kept in this browser.
 *
 * The point of the page is to argue about marks. Arguing is faster when the mark can be changed where it is
 * being looked at.
 */
function Scratchpad() {
  const [css, setCss] = useState(() => {
    try {
      return localStorage.getItem("calcusystem.scratchpad") ?? SEED;
    } catch {
      return SEED;
    }
  });

  useEffect(() => {
    try {
      localStorage.setItem("calcusystem.scratchpad", css);
    } catch {
      // A blocked store is not worth interrupting an experiment over.
    }
  }, [css]);

  return (
    <section className="panel scratchpad">
      <style>{css}</style>

      <details>
        <summary>Try a different stylesheet</summary>
        <p className="quiet">
          Injected after the app's, so it wins, and kept in this browser. Everything on this page is the real
          component, so a change here is a change to what you are looking at.
        </p>
        <textarea
          spellCheck={false}
          value={css}
          onChange={(event) => setCss(event.target.value)}
          aria-label="Your stylesheet"
        />
        <button type="button" onClick={() => setCss(SEED)}>Back to the seed</button>
      </details>
    </section>
  );
}

const SEED = `/* The tokens most worth pushing on. Uncomment and change.

:root {
  --fill: #eceef1;      the mark that says a control writes the model
  --line: #dfe3e8;      a slot's border, once approached
  --accent: #2f6f4f;    holds
  --warn: #a4442f;      broken
  --pending: #b7791f;   unjudged
}

.slot { border-radius: 10px; }
.views .chevron { opacity: 1; }
.mark { font-size: 12px; }
*/
`;

function Channels() {
  return (
    <>
      <p className="quiet">
        The rule that matters. A channel carrying two meanings is the defect this language exists to prevent,
        and the one it was born from.
      </p>
      <table className="channels">
        <tbody>
          {CHANNELS.map((channel) => (
            <tr key={channel.channel}>
              <th>{channel.channel}</th>
              <td className={`vocab ${channel.vocabulary}`}>{channel.vocabulary}</td>
              <td>{channel.carries}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}

function Decisions() {
  return (
    <>
      {DECISIONS.map((decision) => (
        <article key={decision.id} className="principle">
          <h3>{decision.question}</h3>
          <p><b>{decision.answer}</b></p>
          {decision.why.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}
          <dl className="rejected">
            {decision.rejected.map((alternative) => (
              <div key={alternative.what}>
                <dt>{alternative.what}</dt>
                <dd>{alternative.because}</dd>
              </div>
            ))}
          </dl>
        </article>
      ))}
    </>
  );
}

/** The blocks in every state they have. Not marks — the things the marks are worn by. */
function Blocks() {
  const [encoded, setEncoded] = useState<string | null>("M1,L-1,T-2");

  return (
    <dl>
        <dt>dimensionality, inline</dt>
        <dd><DimensionalityView encoded="M1,L-1,T-2" /><span className="note">rows stay level</span></dd>

        <dt>dimensionality, stacked</dt>
        <dd>
          <DimensionalityView encoded="M1,L-1,T-2" layout="stacked" />
          <span className="note">given a line of its own</span>
        </dd>

        <dt>no denominator</dt>
        <dd><DimensionalityView encoded="M1,L2" /></dd>

        <dt>dimensionless</dt>
        <dd><DimensionalityView encoded="" /><span className="note">not the same answer as unchosen</span></dd>

        <dt>a named combination</dt>
        <dd>
          <DimensionalityEditor encoded={encoded} onChange={setEncoded} />
          <span className="note">the reading is a label — nothing to press</span>
        </dd>

        <dt>unchosen</dt>
        <dd>
          <DimensionalityEditor encoded={null} onChange={() => {}} />
          <span className="note">? until the author answers</span>
        </dd>

        <dt>absolute band</dt>
        <dd><MeasurandView value={pressure(8, 0.3)} /></dd>

        <dt>relative band</dt>
        <dd><MeasurandView value={pressure(8, 0.3)} band="relative" /></dd>

        <dt>asymmetric band</dt>
        <dd><MeasurandView value={pressure(8, 0.3, 0.12)} /></dd>

        <dt>no band</dt>
        <dd><MeasurandView value={pressure(8, 0)} /><span className="note">four figures, nothing to set them</span></dd>

        <dt>power factored out</dt>
        <dd>
          <MeasurandView value={pressure(8e6, 3e5)} />
          <span className="note">a view has one unit and no controls</span>
        </dd>

        <dt>power on each number</dt>
        <dd>
          <MeasurandReadout value={pressure(8e6, 3e5)} />
          <span className="note">a readout has a control between them</span>
        </dd>

        <dt>an offset unit</dt>
        <dd>
          <MeasurandReadout value={temperature(293.15, 0.5)} storedAs="absolute" />
          <span className="note">read it in °C — the band stays 0.5</span>
        </dd>

      <dt>nothing at all</dt>
      <dd><MeasurandView value={null} /></dd>
    </dl>
  );
}

function Open() {
  return (
    <>
      <p className="quiet">What the language does not answer yet. The place to start refining.</p>
      {OPEN.map((question) => (
        <article key={question.id} className="principle">
          <h3>{question.question}</h3>
          <p>{question.note}</p>
        </article>
      ))}
    </>
  );
}
