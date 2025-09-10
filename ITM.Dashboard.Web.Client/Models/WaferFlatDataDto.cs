// ITM.Dashboard.Web.Client/Models/WaferFlatDataDto.cs
namespace ITM.Dashboard.Web.Client.Models
{
    public class WaferFlatDataDto
    {
        public string? LotId { get; set; }
        public int? WaferId { get; set; }
        public DateTime? DateTime { get; set; }
        public string? CassetteRcp { get; set; }
        public string? StageRcp { get; set; }
        public string? StageGroup { get; set; }
        public string? Film { get; set; }
    }
}
