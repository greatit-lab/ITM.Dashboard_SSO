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
        public async Task<ActionResult<IEnumerable<PerformanceDataPointWithEqpIdDto>>> GetPerformanceHistory(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string[] eqpids, [FromQuery] int intervalMinutes = 5)
        {
            var results = new List<PerformanceDataPointWithEqpIdDto>();
            if (eqpids == null || eqpids.Length == 0)
            {
                return Ok(results); // EQPID가 없으면 빈 목록 반환
            }

            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var intervalSeconds = intervalMinutes * 60;

            // 여러 장비의 성능 데이터를 지정된 간격으로 그룹화하여 평균을 계산하는 쿼리
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
            cmd.Parameters.AddWithValue("startDate", startDate.ToUniversalTime()); // UTC 기준으로 변환
            cmd.Parameters.AddWithValue("endDate", endDate.ToUniversalTime());   // UTC 기준으로 변환

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
