// ITM.Dashboard.Web.Client/Models/LotUniformityDto.cs
using System.Collections.Generic;

// 각 Wafer의 개별 데이터 포인트를 나타냅니다.
public class LotUniformityDataPointDto
{
    public int Point { get; set; } // X축
    public double Value { get; set; } // Y축
}

// 차트의 각 라인(Wafer)을 나타냅니다.
public class LotUniformitySeriesDto
{
    public int WaferId { get; set; }
    public List<LotUniformityDataPointDto> DataPoints { get; set; } = new();
}

// 차트에 전달할 최종 데이터 포맷
public class LotUniformityChartData
{
    public int WaferId { get; set; }
    public int Point { get; set; }
    public double Value { get; set; }
}
