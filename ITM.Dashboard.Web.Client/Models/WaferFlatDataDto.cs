// ITM.Dashboard.Web.Client/Models/WaferFlatDataDto.cs

using System;
using System.Diagnostics.CodeAnalysis;

namespace ITM.Dashboard.Web.Client.Models
{
    public class WaferFlatDataDto : IEquatable<WaferFlatDataDto>
    {
        public string? EqpId { get; set; }
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

            return EqpId == other.EqpId &&
                   LotId == other.LotId &&
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
            var hash = new HashCode();
            hash.Add(EqpId);
            hash.Add(LotId);
            hash.Add(WaferId);
            hash.Add(ServTs);
            hash.Add(DateTime);
            hash.Add(CassetteRcp);
            hash.Add(StageRcp);
            hash.Add(StageGroup);
            hash.Add(Film);
            return hash.ToHashCode();
        }
    }
}
