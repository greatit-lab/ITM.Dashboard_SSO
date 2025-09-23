// greatit-lab/itm.dashboard/ITM.Dashboard-ee112ad9c8a673098624c0f281b029396658070e/ITM.Dashboard.Api/Controllers/LotUniformityController.cs

using ITM.Dashboard.Api;
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class LotUniformityController : ControllerBase
{
    private readonly HashSet<string> _allowedMetrics = new(StringComparer.OrdinalIgnoreCase)
    {
        "t1", "gof", "z", "srvisz", "cu_ht_nocal", "cu_res_nocal"
    };

    [HttpGet("trend")]
    public async Task<ActionResult<IEnumerable<LotUniformitySeriesDto>>> GetLotUniformityTrend(
        [FromQuery] string lotId,
        [FromQuery] string cassetteRcp,
        [FromQuery] string stageGroup,
        [FromQuery] string? film, // ✅ [수정] film 파라미터를 nullable로 변경
        [FromQuery] string yAxisMetric,
        [FromQuery] string eqpid,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (string.IsNullOrEmpty(lotId) || !_allowedMetrics.Contains(yAxisMetric))
        {
            return BadRequest("Invalid parameters.");
        }

        var results = new Dictionary<int, LotUniformitySeriesDto>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        var sqlBuilder = new StringBuilder();
        // ✅ [수정] WHERE 절에서 film 관련 부분을 제거하고 동적으로 추가하도록 변경
        sqlBuilder.AppendFormat(@"
            SELECT waferid, point, ""{0}""
            FROM public.plg_wf_flat
            WHERE lotid = @lotId
              AND cassettercp = @cassetteRcp
              AND stagegroup = @stageGroup
              AND eqpid = @eqpid
              AND serv_ts BETWEEN @startDate AND @endDate", yAxisMetric);
        
        // ✅ [추가] film 파라미터가 있을 때만 쿼리에 조건을 추가
        if (!string.IsNullOrEmpty(film))
        {
            sqlBuilder.Append(" AND film = @film");
        }

        sqlBuilder.AppendFormat(@"
              AND point IS NOT NULL AND ""{0}"" IS NOT NULL
            ORDER BY waferid, point;", yAxisMetric);

        await using var cmd = new NpgsqlCommand(sqlBuilder.ToString(), conn);
        cmd.Parameters.AddWithValue("lotId", lotId);
        cmd.Parameters.AddWithValue("cassetteRcp", cassetteRcp);
        cmd.Parameters.AddWithValue("stageGroup", stageGroup);
        cmd.Parameters.AddWithValue("eqpid", eqpid);
        cmd.Parameters.AddWithValue("startDate", startDate.Date);
        cmd.Parameters.AddWithValue("endDate", endDate.Date.AddDays(1).AddTicks(-1));
        
        // ✅ [추가] film 파라미터가 있을 때만 파라미터 바인딩
        if (!string.IsNullOrEmpty(film))
        {
            cmd.Parameters.AddWithValue("film", film);
        }


        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var waferId = reader.GetInt32(0);
            if (!results.ContainsKey(waferId))
            {
                results[waferId] = new LotUniformitySeriesDto { WaferId = waferId };
            }

            results[waferId].DataPoints.Add(new LotUniformityDataPointDto
            {
                Point = reader.GetInt32(1),
                Value = reader.GetDouble(2)
            });
        }

        return Ok(results.Values.ToList());
    }
}
