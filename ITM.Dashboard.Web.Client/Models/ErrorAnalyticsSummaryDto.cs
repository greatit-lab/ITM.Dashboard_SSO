// ITM.Dashboard.Web.Client/Models/ErrorAnalyticsSummaryDto.cs
using System.Collections.Generic;

namespace ITM.Dashboard.Web.Client.Models
{
    public class ErrorAnalyticsSummaryDto
    {
        public long TotalErrorCount { get; set; }
        public int ErrorEqpCount { get; set; }
        public string TopErrorId { get; set; }
        public int TopErrorCount { get; set; }
        public string TopErrorLabel { get; set; }
        public List<ChartDataItem> ErrorCountByEqp { get; set; } = new();
    }

    public class ChartDataItem
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }
}
