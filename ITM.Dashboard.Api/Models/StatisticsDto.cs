// ITM.Dashboard.Api/Models/StatisticsDto.cs

namespace ITM.Dashboard.Api.Models
{
    public class StatisticsDto
    {
        public StatisticItem T1 { get; set; } = new StatisticItem();
        public StatisticItem Gof { get; set; } = new StatisticItem();
        public StatisticItem Z { get; set; } = new StatisticItem();
        public StatisticItem Srvisz { get; set; } = new StatisticItem();
    }
}
