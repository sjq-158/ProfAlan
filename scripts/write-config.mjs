// Generates wwwroot/appsettings.json from environment variables or a local .env file,
// so Supabase settings never live in source control.
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const envFile = join(root, ".env");
const fromFile = {};

if (existsSync(envFile)) {
    for (const line of readFileSync(envFile, "utf8").split(/\r?\n/)) {
        const match = line.match(/^\s*([A-Z0-9_]+)\s*=\s*(.*)\s*$/);
        if (match && !line.trimStart().startsWith("#")) fromFile[match[1]] = match[2].replace(/^["']|["']$/g, "");
    }
}

const read = name => process.env[name] ?? fromFile[name] ?? "";
const config = {
    Supabase: {
        Url: read("SUPABASE_URL"),
        AnonKey: read("SUPABASE_ANON_KEY")
    }
};

writeFileSync(join(root, "wwwroot", "appsettings.json"), JSON.stringify(config, null, 2) + "\n");
const mode = config.Supabase.Url && config.Supabase.AnonKey ? `Supabase (${config.Supabase.Url})` : "demo mode (no Supabase settings found)";
console.log(`appsettings.json written: ${mode}`);
