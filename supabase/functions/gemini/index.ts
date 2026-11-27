// Proxies Study Area requests to the Gemini API for every visitor, signed in or not.
// The API key is a Supabase secret (GEMINI_API_KEY), so it never reaches the browser or the
// repository. Deployed with --no-verify-jwt; the project's publishable key is still required.

// Rolling aliases always point at the current free-tier models, so retirements do not break us.
const MODELS = (Deno.env.get("GEMINI_MODELS") ?? "gemini-flash-lite-latest,gemini-flash-latest")
  .split(",").map((m) => m.trim()).filter(Boolean);
const MAX_PROMPT_LENGTH = 6000;
const TIMEOUT_MS = 45_000;

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, apikey, content-type, x-client-info",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

function reply(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
}

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (request.method !== "POST") return reply(405, { error: { message: "Use POST." } });
  if (!request.headers.get("apikey")) return reply(401, { error: { message: "Missing project key." } });

  const apiKey = Deno.env.get("GEMINI_API_KEY");
  if (!apiKey) return reply(503, { error: { message: "The AI assistant is not configured on the server yet." } });

  let payload: { request?: { contents?: { parts?: { text?: string }[] }[] } };
  try {
    payload = await request.json();
  } catch {
    return reply(400, { error: { message: "The request body must be JSON." } });
  }

  const prompt = payload.request?.contents?.[0]?.parts?.[0]?.text ?? "";
  if (!prompt || prompt.length > MAX_PROMPT_LENGTH) {
    return reply(400, { error: { message: "The prompt is empty or too long." } });
  }

  // Try each model in order; move on when one is retired, overloaded or too slow.
  let last: Response | null = null;
  for (const model of MODELS) {
    try {
      const upstream = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent`, {
        method: "POST",
        headers: { "Content-Type": "application/json", "x-goog-api-key": apiKey },
        body: JSON.stringify(payload.request),
        signal: AbortSignal.timeout(TIMEOUT_MS),
      });
      if (upstream.ok || ![404, 429, 500, 503].includes(upstream.status)) {
        return new Response(await upstream.text(), { status: upstream.status, headers: { ...cors, "Content-Type": "application/json" } });
      }
      last = upstream;
    } catch {
      last = null;
    }
  }

  if (last) return new Response(await last.text(), { status: last.status, headers: { ...cors, "Content-Type": "application/json" } });
  return reply(504, { error: { message: "Gemini took too long to answer. Try again." } });
});
