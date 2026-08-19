namespace MIC.risk.Models;

public class RiskAction
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public RiskReport Report { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public long AssigneeEmpId { get; set; }
    public Employee Assignee { get; set; } = null!;
    public DateTimeOffset DueDate { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
