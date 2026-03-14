using System.Net.Http.Json;
using System.Text.Json;

namespace CareHub.Services.Remote;

public sealed class AiApiService
{
    private readonly HttpClient _http;

    public AiApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AiResult> ShiftSummaryAsync(Guid residentId)
    {
        return await PostAsync("api/ai/shift-summary", new { residentId });
    }

    public async Task<AiResult> MedicationExplainAsync(string medicationName, string? dosage)
    {
        return await PostAsync("api/ai/medication-explain", new { medicationName, dosage });
    }

    public async Task<AiResult> DetectTrendsAsync(Guid residentId)
    {
        return await PostAsync("api/ai/detect-trends", new { residentId });
    }

    private async Task<AiResult> PostAsync(string endpoint, object payload)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync(endpoint, payload);

            if (!resp.IsSuccessStatusCode)
            {
                var errorText = await resp.Content.ReadAsStringAsync();
                return new AiResult
                {
                    Success = false,
                    Content = $"AI service error ({(int)resp.StatusCode}): {errorText}"
                };
            }

            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return new AiResult
            {
                Success = true,
                Content = body.GetProperty("content").GetString() ?? "",
                Disclaimer = body.TryGetProperty("disclaimer", out var d) ? d.GetString() ?? "" : "AI-Generated - For Informational Purposes Only"
            };
        }
        catch (Exception ex)
        {
            return new AiResult
            {
                Success = false,
                Content = $"Could not reach AI service: {ex.Message}"
            };
        }
    }
}

public sealed class AiResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = "";
    public string Disclaimer { get; set; } = "AI-Generated - For Informational Purposes Only";
}
