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
 */
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";
import { DO, WRONG } from "../src/patterns/examples";
import { DECISIONS, OPEN, PATTERNS, PRINCIPLES } from "../src/patterns/language";

// From the working directory, not from `import.meta.url`: this is bundled into node_modules/.cache before it
// runs, so a path relative to the module resolves next to the bundle rather than next to the source.
const root = process.cwd();
const failures: string[] = [];

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

// ── ─────────────────────────────────────────────────────────────────────────

if (failures.length > 0) {
  console.error(`\n${failures.length} problem${failures.length === 1 ? "" : "s"}.`);
  process.exit(1);
}

console.log("\nthe language, the sheets and the document agree.");
