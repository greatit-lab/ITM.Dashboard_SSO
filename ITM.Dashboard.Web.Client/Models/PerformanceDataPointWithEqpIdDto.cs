// ITM.Dashboard.Web.Client/Models/PerformanceDataPointWithEqpIdDto.cs
using System;

namespace ITM.Dashboard.Web.Client.Models
{
    public class PerformanceDataPointWithEqpIdDto
    {
        public string EqpId { get; set; }
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
    }
}
