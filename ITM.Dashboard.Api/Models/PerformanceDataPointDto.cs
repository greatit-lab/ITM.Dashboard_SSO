// ITM.Dashboard.Api/Models/PerformanceDataPointDto.cs
using System;

namespace ITM.Dashboard.Api.Models
{
    public class PerformanceDataPointDto
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
    }
}
