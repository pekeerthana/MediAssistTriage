using MediAssistTriage.Models;

namespace MediAssistTriage.Services;

public class RedFlagService
{
    public (string? OverrideLevel, List<string> Flags) CheckForRedFlags(TriageRequest request)
    {
        var flags = new List<string>();
        string? overrideLevel = null;
        var symptomsStr = string.Join(" ", request.Symptoms).ToLower();

        if (symptomsStr.Contains("chest pain") && symptomsStr.Contains("shortness of breath"))
        {
            flags.Add("CARDIAC_EVENT_RISK");
            overrideLevel = "EMERGENCY";
        }
        
        if (symptomsStr.Contains("face drooping") || symptomsStr.Contains("arm weakness") || symptomsStr.Contains("speech difficulty"))
        {
            flags.Add("STROKE_RISK");
            overrideLevel = "EMERGENCY";
        }

        if (request.Age < 12 && request.Symptoms.Any(s => s.Contains("> 39c") || s.Contains("> 102f") || s.Contains("high fever")))
        {
            flags.Add("PEDIATRIC_HIGH_FEVER");
            overrideLevel ??= "URGENT"; 
        }

        return (overrideLevel, flags);
    }
}