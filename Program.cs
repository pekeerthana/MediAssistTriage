using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using MediAssistTriage.Models;
using MediAssistTriage.Services;
using Microsoft.SemanticKernel.Connectors.Google;

var builder = WebApplication.CreateBuilder(args);

// Ensure GEMINI_API_KEY is set in your environment variables
var apiKey = builder.Configuration["GEMINI_API_KEY"];
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("WARNING: GEMINI_API_KEY is not set.");
}
else
{
    // Use gemini-1.5-flash for blazing fast responses (well under 2 seconds)
    builder.Services.AddKernel().AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
}

// Register internal services
builder.Services.AddSingleton<DepartmentService>(); // Singleton loads JSON once
builder.Services.AddScoped<RedFlagService>();
builder.Services.AddScoped<LlmService>();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/triage", async ([FromBody] TriageRequest request, RedFlagService redFlagEngine, LlmService llmService, DepartmentService deptService, ILogger<Program> logger) =>
{
    // NFR-4: Audit Logging (HIPAA-conscious: Log ID, not Name)
    var patientId = Guid.NewGuid().ToString(); 
    logger.LogInformation("Triage request received. PatientID: {PatientId}, Timestamp: {Time}", patientId, DateTime.UtcNow);

    // 1. Red-Flag Detection (FR-3)
    var (overrideLevel, flags) = redFlagEngine.CheckForRedFlags(request);
    
    string finalTriageLevel;
    double confidence;
    string reasoning;

    if (overrideLevel != null)
    {
        finalTriageLevel = overrideLevel;
        confidence = 1.0;
        reasoning = "System safety override triggered due to critical red flags.";
    }
    else
    {
        // 2. LLM Triage Assessment (FR-2)
        var llmResult = await llmService.AssessSymptomsAsync(request);
        finalTriageLevel = llmResult.triage_level;
        confidence = llmResult.confidence_score;
        reasoning = llmResult.reasoning;
    }

    // 3. Department Matching (FR-4 & FR-6)
    var (matchedDeptId, capacityFlag) = deptService.MatchDepartment(finalTriageLevel);

    // 4. Generate Report (FR-5)
    var report = new TriageReport(
        PatientId: patientId,
        TriageLevel: finalTriageLevel,
        ConfidenceScore: confidence,
        RedFlags: flags,
        MatchedDepartment: matchedDeptId,
        RecommendedAction: reasoning,
        EstimatedWaitMinutes: capacityFlag ? 120 : 15,
        CapacityFlag: capacityFlag
    );

    return Results.Ok(report);
});

app.Run();