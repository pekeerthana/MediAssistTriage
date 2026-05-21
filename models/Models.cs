using System.Text.Json.Serialization;

namespace MediAssistTriage.Models;

public record TriageRequest(
    [property: JsonPropertyName("patient_name")] string PatientName,
    [property: JsonPropertyName("age")] int Age,
    [property: JsonPropertyName("gender")] string Gender,
    [property: JsonPropertyName("symptoms")] List<string> Symptoms,
    [property: JsonPropertyName("medical_history_notes")] string? MedicalHistoryNotes
);

public record TriageReport(
    [property: JsonPropertyName("patient_id")] string PatientId,
    [property: JsonPropertyName("triage_level")] string TriageLevel,
    [property: JsonPropertyName("confidence_score")] double ConfidenceScore,
    [property: JsonPropertyName("red_flags")] List<string> RedFlags,
    [property: JsonPropertyName("matched_department")] string MatchedDepartment,
    [property: JsonPropertyName("recommended_action")] string RecommendedAction,
    [property: JsonPropertyName("estimated_wait_minutes")] int EstimatedWaitMinutes,
    [property: JsonPropertyName("capacity_flag")] bool CapacityFlag
);

public record Department(
    [property: JsonPropertyName("department_id")] string DepartmentId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("available_slots")] int AvailableSlots,
    [property: JsonPropertyName("accepts_triage_levels")] List<string> AcceptsTriageLevels
);