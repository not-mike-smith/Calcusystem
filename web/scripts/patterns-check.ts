/*
 * Fails if the written reference has stopped mentioning something the language holds.
 *
 * `language.ts` is the single statement of the vocabulary; `PATTERNS.md` is the prose about it and the gallery
 * is the live view of it. The gallery cannot drift, because it renders from the records. The document can, so
 * it is checked.
 *
 *   npm run patterns-check
 */
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { DECISIONS, OPEN, PATTERNS, PRINCIPLES } from "../src/patterns/language";

// From the working directory, not from `import.meta.url`: this is bundled into node_modules/.cache before it
// runs, so a path relative to the module resolves next to the bundle rather than next to the source.
const document = readFileSync(resolve(process.cwd(), "PATTERNS.md"), "utf8");

const required = [
  ...PRINCIPLES.map((p) => ["principle", p.id] as const),
  ...PATTERNS.map((p) => ["mark", p.id] as const),
  ...DECISIONS.map((d) => ["decision", d.id] as const),
  ...OPEN.map((o) => ["open question", o.id] as const),
];

const missing = required.filter(([, id]) => !document.includes(`\`${id}\``));

for (const [kind, id] of required) {
  const found = document.includes(`\`${id}\``);
  console.log(`${found ? "  ok " : "MISS "} ${kind.padEnd(14)} ${id}`);
}

console.log(`\n${required.length - missing.length}/${required.length} in PATTERNS.md`);

if (missing.length > 0) {
  console.error(
    `\nPATTERNS.md does not mention: ${missing.map(([, id]) => id).join(", ")}.` +
    `\nEvery id in language.ts is written there as \`id\`, so the two cannot drift apart silently.`,
  );
  process.exit(1);
}
