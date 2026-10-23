// Regenerates the client's types from the API's OpenAPI document, which `dotnet build` writes next to the
// project. Run before `dev` and `build` so the types cannot be stale while the server they describe is not.
//
// A missing document is not a failure: a checkout that has not built the API yet still has the generated types
// checked in, and they are only wrong if the wire has changed since.
import { execFileSync } from "node:child_process";
import { existsSync } from "node:fs";

const document = "../Api/Api.json";
const output = "src/api/schema.d.ts";

if (!existsSync(document)) {
  console.warn(`No ${document} — using the checked-in types. Run \`dotnet build Api\` to refresh them.`);
  process.exit(0);
}

execFileSync("npx", ["openapi-typescript", document, "-o", output], { stdio: "inherit" });
