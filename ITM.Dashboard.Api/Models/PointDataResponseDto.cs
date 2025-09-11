// ITM.Dashboard.Api/Models/PointDataResponseDto.cs

using System.Collections.Generic;

namespace ITM.Dashboard.Api.Models
{
    public class PointDataResponseDto
    {
        public List<string> Headers { get; set; } = new List<string>();
        public List<List<object>> Data { get; set; } = new List<List<object>>();
    }
}
