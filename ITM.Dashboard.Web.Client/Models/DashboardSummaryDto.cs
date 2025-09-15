// ITM.Dashboard.Web.Client/Models/DashboardSummaryDto.cs
namespace ITM.Dashboard.Web.Client.Models
{
    public class DashboardSummaryDto
    {
        public int TotalEqpCount { get; set; }
        public int OnlineAgentCount { get; set; }
        public int TodayErrorCount { get; set; }
        public long TodayDataCount { get; set; }
    }
}
