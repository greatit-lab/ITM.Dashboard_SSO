// ITM.Dashboard.Api/Controllers/PerformanceAnalyticsController.cs
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ITM.Dashboard.Api.Controllers
{
    // API 응답을 위한 새로운 DTO 정의
    public class PerformanceDataPointWithEqpIdDto
    {
        public string EqpId { get; set; }
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class PerformanceAnalyticsController : ControllerBase
    {
        private string GetConnectionString() => new DatabaseInfo().GetConnectionString();

        [HttpGet("history")]
        // ▼▼▼ [수정] intervalMinutes를 intervalSeconds로 변경하고 기본값을 300초(5분)로 설정 ▼▼▼
        public async Task<ActionResult<IEnumerable<PerformanceDataPointWithEqpIdDto>>> GetPerformanceHistory(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string[] eqpids, [FromQuery] int intervalSeconds = 300)
        {
            var results = new List<PerformanceDataPointWithEqpIdDto>();
            if (eqpids == null || eqpids.Length == 0)
            {
                return Ok(results);
            }

            // ▼▼▼ [수정] 0 이하의 값이 들어올 경우 최소 1초로 강제하여 DB 오류 방지 ▼▼▼
            if (intervalSeconds <= 0)
            {
                intervalSeconds = 1;
            }

            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            
            // ▼▼▼ [수정] 쿼리에서 intervalSeconds를 직접 사용하도록 변경 ▼▼▼
            var sql = $@"
                SELECT
                    eqpid,
                    (timestamp 'epoch' + (floor(extract(epoch from serv_ts) / {intervalSeconds}) * {intervalSeconds}) * interval '1 second') as interval_time,
                    AVG(cpu_usage) as avg_cpu,
                    AVG(mem_usage) as avg_mem
                FROM public.eqp_perf
                WHERE eqpid = ANY(@eqpids)
                  AND serv_ts >= @startDate
                  AND serv_ts <= @endDate
                GROUP BY eqpid, interval_time
                ORDER BY eqpid, interval_time;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("eqpids", eqpids);
            cmd.Parameters.AddWithValue("startDate", startDate.ToUniversalTime());
            cmd.Parameters.AddWithValue("endDate", endDate.ToUniversalTime());

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new PerformanceDataPointWithEqpIdDto
                {
                    EqpId = reader.GetString(0),
                    Timestamp = reader.GetDateTime(1),
                    CpuUsage = reader.GetDouble(2),
                    MemoryUsage = reader.GetDouble(3)
                });
            }
            return Ok(results);
        }
    }
}
