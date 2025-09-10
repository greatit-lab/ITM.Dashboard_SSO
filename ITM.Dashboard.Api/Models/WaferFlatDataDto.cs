// ITM.Dashboard.Api/Models/WaferFlatDataDto.cs
using System;

namespace ITM.Dashboard.Api.Models
{
    public class WaferFlatDataDto
    {
        public string? LotId { get; set; }
        public int? WaferId { get; set; }
        public DateTime? DateTime { get; set; }
        public int Point { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public string? CassetteRcp { get; set; }
        public string? StageRcp { get; set; }
        public string? StageGroup { get; set; }
        public string? Film { get; set; }
    }
}
