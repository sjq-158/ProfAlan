using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProfAlan.Features.StudyArea.Models;

namespace ProfAlan.Features.StudyArea.Services;

public sealed class GeminiException(string message) : Exception(message);

/// <summary>
/// Calls the Gemini API straight from the browser with the user's own key and asks for JSON
/// that matches a response schema, so the output can be checked by ProfAlan's engines.
/// </summary>
public sealed class GeminiClient(HttpClient http)
{
    public static readonly IReadOnlyList<string> SuggestedModels = ["gemini-2.5-flash", "gemini-2.5-pro", "gemini-2.5-flash-lite"];

    private const string Notation =
        "Use the notation of Lewis and Papadimitriou, Elements of the Theory of Computation (2nd ed.): " +
        "write the empty string as e, union in regular expressions as ∪, and grammars as lines like S → aSb | e " +
        "with single uppercase letters as variables. Do not use emoji.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<GeneratedProblem> GenerateProblemAsync(string apiKey, string model, GenerationRequest request, CancellationToken cancel = default)
    {
        var sigma = string.Join(", ", request.Alphabet.ToCharArray());
        var prompt = $$"""
            You are a teaching assistant for an automata theory course. Write one practice problem.
            Topic focus: {{request.Topic}}. The solver must submit a {{request.AnswerKind}}. Difficulty: {{request.Difficulty}}.
            Alphabet: {{{sigma}}}.
            {{Notation}}
            The prompt must define the language precisely in set-builder notation, for example
            "Construct a DFA for L = { w ∈ {a,b}* | w ends with bb }".
            referenceAnswer must be a correct regular expression for the language if it is regular, otherwise a context-free grammar.
            Give 12 to 16 distinct test cases over the alphabet, including the empty string written as an empty input "",
            with roughly half accepted. Mark about two thirds of them hidden.
            """;

        var schema = new
        {
            type = "OBJECT",
            properties = new
            {
                title = new { type = "STRING" },
                prompt = new { type = "STRING" },
                answerKind = new { type = "STRING", @enum = new[] { "dfa", "nfa", "pda", "turing", "regex", "grammar" } },
                alphabet = new { type = "STRING", description = "The symbols of Σ written together, e.g. ab" },
                referenceAnswer = new { type = "STRING" },
                hint = new { type = "STRING" },
                testCases = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new { input = new { type = "STRING" }, accept = new { type = "BOOLEAN" }, hidden = new { type = "BOOLEAN" } },
                        required = new[] { "input", "accept", "hidden" }
                    }
                }
            },
            required = new[] { "title", "prompt", "answerKind", "alphabet", "referenceAnswer", "hint", "testCases" }
        };

        return await AskAsync<GeneratedProblem>(apiKey, model, prompt, schema, 0.9, cancel);
    }

    public async Task<LanguageExplanation> ExplainLanguageAsync(string apiKey, string model, string language, CancellationToken cancel = default)
    {
        var prompt = $"""
            Explain, for an automata theory student, the language described by: {language}
            It may be a regular expression, a context-free grammar, or set-builder notation.
            {Notation}
            Give a one-sentence summary in plain words, a short explanation of how to recognise it
            (which machine type is enough and why), up to six shortest members and up to six shortest non-members.
            Write the empty string as e in the member lists.
            """;

        var schema = new
        {
            type = "OBJECT",
            properties = new
            {
                summary = new { type = "STRING" },
                explanation = new { type = "STRING" },
                members = new { type = "ARRAY", items = new { type = "STRING" } },
                nonMembers = new { type = "ARRAY", items = new { type = "STRING" } }
            },
            required = new[] { "summary", "explanation", "members", "nonMembers" }
        };

        return await AskAsync<LanguageExplanation>(apiKey, model, prompt, schema, 0.4, cancel);
    }

    private async Task<T> AskAsync<T>(string apiKey, string model, string prompt, object schema, double temperature, CancellationToken cancel)
    {
        var body = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { responseMimeType = "application/json", responseSchema = schema, temperature }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model.Trim())}:generateContent")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("x-goog-api-key", apiKey.Trim());

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancel);
        }
        catch (HttpRequestException)
        {
            throw new GeminiException("Could not reach the Gemini API. Check your connection.");
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(cancel);
            if (!response.IsSuccessStatusCode) throw new GeminiException(Describe(response.StatusCode, text));

            try
            {
                using var doc = JsonDocument.Parse(text);
                var json = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                return JsonSerializer.Deserialize<T>(json ?? "", Json) ?? throw new GeminiException("Gemini returned an empty answer.");
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException)
            {
                throw new GeminiException("Gemini returned something that was not the expected JSON. Try again.");
            }
        }
    }

    private static string Describe(HttpStatusCode status, string body)
    {
        string? message = null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            message = doc.RootElement.GetProperty("error").GetProperty("message").GetString();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
        }

        return status switch
        {
            HttpStatusCode.BadRequest when message?.Contains("API key", StringComparison.OrdinalIgnoreCase) == true => "Gemini rejected the API key. Check that it is correct and enabled.",
            HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "This API key is not allowed to use the Gemini API.",
            HttpStatusCode.NotFound => "That model name was not found. Pick one of the suggested models.",
            HttpStatusCode.TooManyRequests => "The key's rate limit or quota was reached. Wait a moment and try again.",
            _ => message ?? $"Gemini returned an error ({(int)status})."
        };
    }
}
