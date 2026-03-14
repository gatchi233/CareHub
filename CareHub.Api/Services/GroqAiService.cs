using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareHub.Api.Services;

public sealed class GroqAiService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public GroqAiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["Groq:Model"] ?? "llama-3.3-70b-versatile";
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var body = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            max_tokens = 1024,
        };

        var resp = await _http.PostAsJsonAsync("openai/v1/chat/completions", body, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
    }
}
