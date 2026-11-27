// Stores GEMINI_API_KEY from .env as a Supabase secret and deploys the gemini Edge Function so
// every visitor can use the AI assistant without a key. Logs in to Supabase first if needed.
// The key is passed to the CLI, never written to a file.
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const env = { ...process.env };
const envFile = join(root, ".env");
if (existsSync(envFile)) {
    for (const line of readFileSync(envFile, "utf8").split(/\r?\n/)) {
        const match = line.match(/^\s*([A-Z0-9_]+)\s*=\s*(.*)\s*$/);
        if (match && !line.trimStart().startsWith("#") && !env[match[1]]) env[match[1]] = match[2].replace(/^["']|["']$/g, "");
    }
}

const ref = (env.SUPABASE_URL ?? "").match(/^https:\/\/([a-z0-9]+)\.supabase\.co/)?.[1];
if (!ref) throw new Error("SUPABASE_URL is missing or not a supabase.co project URL.");
if (!env.GEMINI_API_KEY) throw new Error("GEMINI_API_KEY is missing from .env.");

const run = args => {
    const result = spawnSync("npx", ["--yes", "supabase", ...args], { cwd: root, stdio: "inherit", shell: process.platform === "win32" });
    if (result.status !== 0) process.exit(result.status ?? 1);
};

if (!env.SUPABASE_ACCESS_TOKEN && !existsSync(join(homedir(), ".supabase", "access-token"))) {
    console.log("Opening the Supabase login in your browser...");
    run(["login"]);
}

run(["secrets", "set", `GEMINI_API_KEY=${env.GEMINI_API_KEY}`, "--project-ref", ref]);
run(["functions", "deploy", "gemini", "--no-verify-jwt", "--project-ref", ref]);
console.log("AI assistant deployed. Every visitor can now use the Study Area generator.");
