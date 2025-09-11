// ITM.Dashboard.Web.Client/Models/StatisticItem.cs

namespace ITM.Dashboard.Web.Client.Models
{
    public class StatisticItem
    {
        public double Max { get; set; }
        public double Min { get; set; }
        public double Range { get; set; }
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double PercentStdDev { get; set; }
        public double PercentNonU { get; set; }
    }
}
