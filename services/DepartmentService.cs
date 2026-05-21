using MediAssistTriage.Models;
using System.Text.Json;

namespace MediAssistTriage.Services;

public class DepartmentService
{
    private readonly List<Department> _departments;

    public DepartmentService()
    {
        var json = File.ReadAllText("data/departments.json");
        using var document = JsonDocument.Parse(json);
        _departments = JsonSerializer.Deserialize<List<Department>>(document.RootElement.GetProperty("departments").GetRawText())!;
    }

    public (string DepartmentId, bool CapacityFlag) MatchDepartment(string triageLevel)
    {
        var validDepts = _departments.Where(d => d.AcceptsTriageLevels.Contains(triageLevel)).ToList();

        // Exact match with available slots (FR-4)
        var primaryMatch = validDepts.FirstOrDefault(d => d.AvailableSlots > 0);
        if (primaryMatch != null) return (primaryMatch.DepartmentId, false);

        // Capacity Fallback: match next best with 0 slots, set flag to true (FR-6)
        var fallback = validDepts.FirstOrDefault();
        return (fallback?.DepartmentId ?? "DEPT-UNKNOWN", true);
    }
}