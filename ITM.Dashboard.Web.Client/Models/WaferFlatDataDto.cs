// ITM.Dashboard.Web.Client/Models/WaferFlatDataDto.cs

using System;
using System.Diagnostics.CodeAnalysis;

namespace ITM.Dashboard.Web.Client.Models
{
    public class WaferFlatDataDto : IEquatable<WaferFlatDataDto>
    {
        public string? LotId { get; set; }
        public int? WaferId { get; set; }
        public DateTime? ServTs { get; set; }
        public DateTime? DateTime { get; set; }
        public string? CassetteRcp { get; set; }
        public string? StageRcp { get; set; }
        public string? StageGroup { get; set; }
        public string? Film { get; set; }

        public bool Equals(WaferFlatDataDto? other)
        {
            if (other is null) return false;

            return LotId == other.LotId &&
                   WaferId == other.WaferId &&
                   ServTs == other.ServTs &&
                   DateTime == other.DateTime &&
                   CassetteRcp == other.CassetteRcp &&
                   StageRcp == other.StageRcp &&
                   StageGroup == other.StageGroup &&
                   Film == other.Film;
        }

        public override bool Equals(object? obj) => Equals(obj as WaferFlatDataDto);

        public override int GetHashCode()
        {
            return HashCode.Combine(LotId, WaferId, ServTs, DateTime, CassetteRcp, StageRcp, StageGroup, Film);
        }
    }
}
