// ITM.Dashboard.Api/Models/StatisticItem.cs

namespace ITM.Dashboard.Api.Models
{
    public class StatisticItem
    {
        public double Max { get; set; }
        public double Min { get; set; }
        public double Range => Max - Min;
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double PercentStdDev => (Mean != 0) ? (StdDev / Mean) * 100 : 0;
        public double PercentNonU => (Mean != 0) ? (Range / (2 * Mean)) * 100 : 0;
    }
}
