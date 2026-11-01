/*
 * Holds the pattern language, the sheets, and the prose about them against each other.
 *
 *   npm run patterns-check
 *
 * Four things, each of which has been wrong at least once:
 *   - `PATTERNS.md` mentions everything the language holds
 *   - every mark has a correct example to show
 *   - every alternative that claims it can be shown is shown, and nothing is shown that was never claimed
 *   - no deliberate violation has leaked out of the sheets into the UI it argues against
 *   - every link on the page resolves, and the router accepts it
 */
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";
import { renderToStaticMarkup } from "react-dom/server";
import { CatalogueProvider } from "../src/model/catalogue";
import { DO, WRONG } from "../src/patterns/examples";
import { Gallery, SECTIONS } from "../src/patterns/Gallery";
import { DECISIONS, OPEN, PATTERNS, PRINCIPLES } from "../src/patterns/language";
import { isPatterns, sectionHref } from "../src/patterns/route";

// From the working directory, not from `import.meta.url`: this is bundled into node_modules/.cache before it
// runs, so a path relative to the module resolves next to the bundle rather than next to the source.
const root = process.cwd();
const failures: string[] = [];

/** Enough of the catalogue to render the page. The marks are what is being checked, not the units. */
const DIMENSIONS = [
  { Symbol: "M", Name: "Mass" },
  { Symbol: "L", Name: "Length" },
  { Symbol: "Θ", Name: "Temperature" },
  { Symbol: "T", Name: "Time" },
];

function check(name: string, wrong: string[]) {
  console.log(`${wrong.length === 0 ? "  ok " : "FAIL"}  ${name}`);
  for (const line of wrong) console.log(`        ${line}`);

  failures.push(...wrong);
}

// ── The document mentions everything ────────────────────────────────────────

const document = readFileSync(resolve(root, "PATTERNS.md"), "utf8");

const ids = [
  ...PRINCIPLES.map((p) => p.id),
  ...PATTERNS.map((p) => p.id),
  ...DECISIONS.map((d) => d.id),
  ...OPEN.map((o) => o.id),
];

check(
  `PATTERNS.md mentions all ${ids.length} ids`,
  ids.filter((id) => !document.includes(`\`${id}\``)).map((id) => `not in PATTERNS.md: ${id}`),
);

// ── Every mark has something to show ────────────────────────────────────────

check(
  "every mark has a correct example",
  PATTERNS.filter((pattern) => !DO[pattern.id]).map((pattern) => `no DO example: ${pattern.id}`),
);

// ── The sheets and the argument agree ───────────────────────────────────────

const claimed = PATTERNS.flatMap((pattern) => pattern.rejected)
  .map((alternative) => alternative.id)
  .filter((id): id is string => id !== undefined);

check("every alternative that claims an example has one", [
  ...claimed.filter((id) => !WRONG[id]).map((id) => `claimed but not shown: ${id}`),
  ...Object.keys(WRONG)
    .filter((id) => !claimed.includes(id))
    .map((id) => `shown but no alternative claims it: ${id}`),
]);

// ── Violations stay in the sheets ───────────────────────────────────────────

function sources(directory: string): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const path = join(directory, entry);

    if (statSync(path).isDirectory()) return sources(path);

    return /\.tsx?$/.test(entry) ? [path] : [];
  });
}

const leaked = sources(resolve(root, "src"))
  .filter((path) => !path.includes("/patterns/"))
  .filter((path) => /\bnono-/.test(readFileSync(path, "utf8")))
  .map((path) => `a nono- class is used outside the sheets: ${path.slice(root.length + 1)}`);

check("no deliberate violation has leaked into the UI", leaked);

// ── Every link on the page goes somewhere ───────────────────────────────────

/* The bug this is here for: the router matched `#patterns` exactly, so every link at the top of the page —
   `#patterns-affordance` and the rest — fell through to the editor. The ids were right and unreachable. */
check(
  "the router accepts every section link",
  SECTIONS.map((section) => sectionHref(section.id))
    .filter((href) => !isPatterns(href))
    .map((href) => `the router would not stay on the page for ${href}`),
);

const markup = renderToStaticMarkup(
  <CatalogueProvider vocabulary={{ Dimensions: DIMENSIONS } as never} units={{ Dimensionalities: [] } as never}>
    <Gallery />
  </CatalogueProvider>,
);

const targets = new Set([...markup.matchAll(/ id="([^"]+)"/g)].map((match) => match[1]!));
const internal = [...markup.matchAll(/href="#([^"]+)"/g)].map((match) => match[1]!);

check(
  `all ${internal.length} links on the page resolve`,
  internal.filter((target) => !targets.has(target)).map((target) => `nothing on the page has id ${target}`),
);

// ── ─────────────────────────────────────────────────────────────────────────

if (failures.length > 0) {
  console.error(`\n${failures.length} problem${failures.length === 1 ? "" : "s"}.`);
  process.exit(1);
}

console.log("\nthe language, the sheets and the document agree.");
