// ITM.Dashboard.Api/Models/LotUniformityDto.cs
using System.Collections.Generic;

namespace ITM.Dashboard.Api.Models
{
    // 각 Wafer의 개별 데이터 포인트를 나타냅니다.
    public class LotUniformityDataPointDto
    {
        public int Point { get; set; } // X축 (Point #)
        public double Value { get; set; } // Y축 (선택된 측정 항목의 값)
    }

    // 차트의 각 라인(Wafer)을 나타냅니다.
    public class LotUniformitySeriesDto
    {
        public int WaferId { get; set; }
        public List<LotUniformityDataPointDto> DataPoints { get; set; } = new();
    }
}
