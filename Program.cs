using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using System.Collections.Concurrent;
using MediAssistTriage.Models;
using MediAssistTriage.Services;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS for the frontend
builder.Services.AddCors();

// Configure Semantic Kernel with Google Gemini
var apiKey = builder.Configuration["GEMINI_API_KEY"];
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("WARNING: GEMINI_API_KEY is not set in appsettings.Development.json or environment variables.");
}
else
{
    builder.Services.AddKernel().AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
}

// Register internal services and in-memory database
builder.Services.AddSingleton<DepartmentService>(); 
builder.Services.AddScoped<RedFlagService>();
builder.Services.AddScoped<LlmService>();
builder.Services.AddSingleton<ConcurrentDictionary<string, TriageReport>>(); 

var app = builder.Build();

// Middleware
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles(); // Serves index.html automatically
app.UseStaticFiles();  // Serves from wwwroot folder

// FR-7: POST /triage - Main Assessment Endpoint
app.MapPost("/triage", async ([FromBody] TriageRequest request, RedFlagService redFlagEngine, LlmService llmService, DepartmentService deptService, ILogger<Program> logger, ConcurrentDictionary<string, TriageReport> reportStore) =>
{
    var patientId = Guid.NewGuid().ToString(); 
    logger.LogInformation("Triage request received. PatientID: {PatientId}, Timestamp: {Time}", patientId, DateTime.UtcNow);

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
        var llmResult = await llmService.AssessSymptomsAsync(request);
        finalTriageLevel = llmResult.triage_level;
        confidence = llmResult.confidence_score;
        reasoning = llmResult.reasoning;
    }

    var (matchedDeptId, capacityFlag) = deptService.MatchDepartment(finalTriageLevel);

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

    reportStore[patientId] = report;
    return Results.Ok(report);
});

// FR-7: GET /report/{patient_id} - Retrieve Report Endpoint
app.MapGet("/report/{patient_id}", (string patient_id, ConcurrentDictionary<string, TriageReport> reportStore) => 
{
    if (reportStore.TryGetValue(patient_id, out var report)) 
    {
        return Results.Ok(report);
    }
    return Results.NotFound(new { error = "Report not found." });
});

// FR-7: POST /escalate/{patient_id} - Manual Escalation Endpoint
app.MapPost("/escalate/{patient_id}", (string patient_id, ConcurrentDictionary<string, TriageReport> reportStore, DepartmentService deptService) => 
{
    if (!reportStore.TryGetValue(patient_id, out var existingReport)) 
    {
        return Results.NotFound(new { error = "Report not found." });
    }

    var (newDept, capFlag) = deptService.MatchDepartment("EMERGENCY");

    var escalatedReport = existingReport with 
    {
        TriageLevel = "EMERGENCY",
        MatchedDepartment = newDept,
        CapacityFlag = capFlag,
        EstimatedWaitMinutes = capFlag ? 120 : 0, 
        RecommendedAction = "MANUAL OVERRIDE: Patient escalated to EMERGENCY by staff."
    };

    reportStore[patient_id] = escalatedReport;
    return Results.Ok(escalatedReport);
});

app.Run();