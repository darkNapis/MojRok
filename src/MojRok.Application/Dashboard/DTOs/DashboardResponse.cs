using MojRok.Application.Deadlines.DTOs;

namespace MojRok.Application.Dashboard.DTOs;

public class DashboardResponse
{
    public Dictionary<string, int> TotalByStatus { get; set; } = new();
    public int UpcomingNext7Days { get; set; }
    public int OverdueCount { get; set; }
    public int RecentlyCompleted { get; set; }
    public List<DeadlineResponse> Next3Urgent { get; set; } = new();
}