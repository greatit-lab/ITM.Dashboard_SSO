// ITM.Dashboard.Api/Models/FilterDataDto.cs
namespace ITM.Dashboard.Api.Models
{
    public class FilterDataDto
    {
        // string 뒤에 '?'를 붙여 Null을 허용하도록 설정합니다.
        public string? Site { get; set; }
        public string? Sdwt { get; set; }
        public string? EqpId { get; set; }
    }
}
