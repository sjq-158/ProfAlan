// Proxies Study Area requests to the Gemini API. The API key is a Supabase secret
// (GEMINI_API_KEY), so it never reaches the browser or the repository.
// Only signed-in users can call this function: Supabase verifies their JWT before it runs.

const FREE_TIER_MODELS = ["gemini-2.5-flash", "gemini-2.5-flash-lite"];
const DEFAULT_MODEL = Deno.env.get("GEMINI_MODEL") ?? FREE_TIER_MODELS[0];
const MAX_PROMPT_LENGTH = 6000;

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

  const apiKey = Deno.env.get("GEMINI_API_KEY");
  if (!apiKey) return reply(503, { error: { message: "The AI assistant is not configured on the server yet." } });

  let payload: { model?: string; request?: { contents?: { parts?: { text?: string }[] }[] } };
  try {
    payload = await request.json();
  } catch {
    return reply(400, { error: { message: "The request body must be JSON." } });
  }

  const prompt = payload.request?.contents?.[0]?.parts?.[0]?.text ?? "";
  if (!prompt || prompt.length > MAX_PROMPT_LENGTH) {
    return reply(400, { error: { message: "The prompt is empty or too long." } });
  }

  const model = payload.model && FREE_TIER_MODELS.includes(payload.model) ? payload.model : DEFAULT_MODEL;
  const upstream = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent`, {
    method: "POST",
    headers: { "Content-Type": "application/json", "x-goog-api-key": apiKey },
    body: JSON.stringify(payload.request),
  });

  return new Response(await upstream.text(), {
    status: upstream.status,
    headers: { ...cors, "Content-Type": "application/json" },
  });
});
