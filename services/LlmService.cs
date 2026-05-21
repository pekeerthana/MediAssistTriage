using System.Text.Json;
using Microsoft.SemanticKernel;
using MediAssistTriage.Models;

namespace MediAssistTriage.Services;

public class LlmResult 
{
    public string triage_level { get; set; } = "STANDARD";
    public double confidence_score { get; set; } = 0.5;
    public string reasoning { get; set; } = "";
}

public class LlmService(Kernel kernel)
{
    public async Task<LlmResult> AssessSymptomsAsync(TriageRequest request)
    {
        var prompt = $$"""
            You are a medical triage AI. Assess the following patient symptoms and history.
            Respond EXACTLY in this JSON format, nothing else:
            {
                "triage_level": "EMERGENCY" or "URGENT" or "STANDARD" or "SELF_CARE",
                "confidence_score": (number between 0.0 and 1.0),
                "reasoning": "brief natural-language explanation"
            }

            Symptoms: {{string.Join(", ", request.Symptoms)}}
            Age: {{request.Age}}
            History: {{request.MedicalHistoryNotes ?? "None"}}
            """;

        try
        {
            var result = await kernel.InvokePromptAsync(prompt);
            var json = result.ToString();
            
            // Clean up markdown block formatting if the LLM includes it
            if (json.StartsWith("```json")) json = json.Replace("```json", "").Replace("```", "");
            
            return JsonSerializer.Deserialize<LlmResult>(json.Trim()) ?? new LlmResult();
        }
        catch (Exception ex)
        {
            // Fallback for LLM failure
            Console.WriteLine($"LLM Error: {ex.Message}");
            return new LlmResult { triage_level = "STANDARD", confidence_score = 0.0, reasoning = "LLM Evaluation Failed. Defaulting to STANDARD." };
        }
    }
}