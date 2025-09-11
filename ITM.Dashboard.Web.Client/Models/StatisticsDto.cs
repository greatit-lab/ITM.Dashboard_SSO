// ITM.Dashboard.Web.Client/Models/StatisticsDto.cs

namespace ITM.Dashboard.Web.Client.Models
{
    public class StatisticsDto
    {
        public StatisticItem T1 { get; set; } = new();
        public StatisticItem Gof { get; set; } = new();
        public StatisticItem Z { get; set; } = new();
        public StatisticItem Srvisz { get; set; } = new();
    }
}
