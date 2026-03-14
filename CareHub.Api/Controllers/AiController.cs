using CareHub.Api.Data;
using CareHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
public sealed class AiController : ControllerBase
{
    private readonly CareHubDbContext _db;
    private readonly GroqAiService _ai;
    private readonly AiRateLimiter _limiter;

    public AiController(CareHubDbContext db, GroqAiService ai, AiRateLimiter limiter)
    {
        _db = db;
        _ai = ai;
        _limiter = limiter;
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.Identity?.Name
        ?? "anonymous";

    // POST api/ai/shift-summary
    [HttpPost("shift-summary")]
    public async Task<IActionResult> ShiftSummary(
        [FromBody] AiResidentRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        var resident = await _db.Residents.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResidentId, ct);
        if (resident is null) return NotFound("Resident not found.");

        var since = DateTimeOffset.UtcNow.AddHours(-24);

        var observations = await _db.Observations.AsNoTracking()
            .Where(o => o.ResidentId == request.ResidentId && o.RecordedAt >= since.UtcDateTime)
            .OrderByDescending(o => o.RecordedAt)
            .Take(20)
            .ToListAsync(ct);

        var marEntries = await _db.MarEntries.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId && !m.IsVoided
                        && m.AdministeredAtUtc >= since)
            .OrderByDescending(m => m.AdministeredAtUtc)
            .Take(30)
            .ToListAsync(ct);

        var medications = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId)
            .ToListAsync(ct);

        var residentName = $"{resident.ResidentFName} {resident.ResidentLName}".Trim();

        var dataBlock = new StringBuilder();
        dataBlock.AppendLine($"Resident: {residentName}");
        dataBlock.AppendLine($"Room: {resident.RoomNumber}, DOB: {resident.DateOfBirth}, Gender: {resident.Gender}");
        dataBlock.AppendLine();

        dataBlock.AppendLine("== Active Medications ==");
        foreach (var m in medications)
            dataBlock.AppendLine($"- {m.MedName} ({m.Dosage}), {m.TimesPerDay}x/day");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== Recent Observations (last 24h) ==");
        foreach (var o in observations)
            dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {o.Type}: {o.Value} (by {o.RecordedBy})");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== Recent MAR Entries (last 24h) ==");
        foreach (var e in marEntries)
        {
            var medName = medications.FirstOrDefault(m => m.Id == e.MedicationId)?.MedName ?? "Unknown";
            dataBlock.AppendLine($"- [{e.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {medName}: {e.Status} ({e.DoseQuantity} {e.DoseUnit}) by {e.RecordedBy}");
        }

        var systemPrompt = """
            You are a clinical assistant for a care facility. Generate a concise,
            plain-language shift summary for the incoming staff. Include:
            - Overall status of the resident
            - Key observations and any concerning trends
            - Medication administration summary (given, refused, missed)
            - Any items requiring follow-up
            Keep it under 200 words. Use bullet points. Do not make diagnoses.
            """;

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, dataBlock.ToString(), ct);
            return Ok(new AiResponse { Content = result, ResidentName = residentName });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/medication-explain
    [HttpPost("medication-explain")]
    public async Task<IActionResult> MedicationExplain(
        [FromBody] AiMedicationRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        if (string.IsNullOrWhiteSpace(request.MedicationName))
            return BadRequest("MedicationName is required.");

        var systemPrompt = """
            You are a helpful medication reference assistant for care facility staff.
            Given a medication name and dosage, provide:
            - What the medication is used for (1-2 sentences)
            - Common side effects (bullet list, max 5)
            - Important notes for care staff (e.g., take with food, monitor BP)
            Keep it simple and under 150 words. This is for informational purposes only.
            Do NOT provide dosage advice or make treatment recommendations.
            """;

        var userPrompt = $"Medication: {request.MedicationName}";
        if (!string.IsNullOrWhiteSpace(request.Dosage))
            userPrompt += $", Dosage: {request.Dosage}";

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, userPrompt, ct);
            return Ok(new AiResponse { Content = result, ResidentName = null });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/detect-trends
    [HttpPost("detect-trends")]
    public async Task<IActionResult> DetectTrends(
        [FromBody] AiResidentRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        var resident = await _db.Residents.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResidentId, ct);
        if (resident is null) return NotFound("Resident not found.");

        var since = DateTimeOffset.UtcNow.AddDays(-14);

        var observations = await _db.Observations.AsNoTracking()
            .Where(o => o.ResidentId == request.ResidentId && o.RecordedAt >= since.UtcDateTime)
            .OrderBy(o => o.RecordedAt)
            .Take(50)
            .ToListAsync(ct);

        var marEntries = await _db.MarEntries.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId && !m.IsVoided
                        && m.AdministeredAtUtc >= since)
            .OrderBy(m => m.AdministeredAtUtc)
            .Take(100)
            .ToListAsync(ct);

        var medications = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId)
            .ToListAsync(ct);

        var residentName = $"{resident.ResidentFName} {resident.ResidentLName}".Trim();

        var dataBlock = new StringBuilder();
        dataBlock.AppendLine($"Resident: {residentName}");
        dataBlock.AppendLine($"Analysis period: last 14 days");
        dataBlock.AppendLine();

        dataBlock.AppendLine("== Observations (chronological) ==");
        foreach (var o in observations)
            dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {o.Type}: {o.Value}");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== MAR Entries (chronological) ==");
        var grouped = marEntries.GroupBy(e => e.Status);
        foreach (var g in grouped)
            dataBlock.AppendLine($"- {g.Key}: {g.Count()} entries");
        dataBlock.AppendLine();
        var refusals = marEntries.Where(e => e.Status == "Refused").ToList();
        if (refusals.Any())
        {
            dataBlock.AppendLine("Refused medications detail:");
            foreach (var r in refusals)
            {
                var medName = medications.FirstOrDefault(m => m.Id == r.MedicationId)?.MedName ?? "Unknown";
                dataBlock.AppendLine($"  - [{r.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {medName}");
            }
        }

        var systemPrompt = """
            You are a clinical trend analysis assistant for a care facility.
            Analyze the resident's data over the past 14 days and identify:
            - Trends in vital signs (improving, stable, declining)
            - Patterns in medication refusals or missed doses
            - Any observations that warrant staff attention
            - Overall trajectory assessment
            If no concerning trends are found, say so clearly.
            Keep it under 200 words. Use bullet points. Do not make diagnoses.
            Flag items that may need follow-up with a doctor.
            """;

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, dataBlock.ToString(), ct);
            return Ok(new AiResponse { Content = result, ResidentName = residentName });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }
}

public sealed class AiResidentRequest
{
    public Guid ResidentId { get; set; }
}

public sealed class AiMedicationRequest
{
    public string MedicationName { get; set; } = "";
    public string? Dosage { get; set; }
}

public sealed class AiResponse
{
    public string Content { get; set; } = "";
    public string? ResidentName { get; set; }
    public string Disclaimer { get; set; } = "AI-Generated - For Informational Purposes Only";
}
