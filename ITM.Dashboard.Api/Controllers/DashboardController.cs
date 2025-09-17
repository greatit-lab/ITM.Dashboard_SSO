// ITM.Dashboard.Api/Controllers/DashboardController.cs
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ITM.Dashboard.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ILogger<DashboardController> _logger;
        private string GetConnectionString() => new DatabaseInfo().GetConnectionString();

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        private void AddFilterLogic(StringBuilder whereClause, NpgsqlCommand cmd, string site, string sdwt)
        {
            whereClause.Append(" AND r.sdwt IN (SELECT sdwt FROM public.ref_sdwt WHERE is_use = 'Y')");

            if (!string.IsNullOrEmpty(sdwt))
            {
                whereClause.Append(" AND r.sdwt = @sdwt");
                cmd.Parameters.AddWithValue("sdwt", sdwt);
            }
            else if (!string.IsNullOrEmpty(site))
            {
                whereClause.Append(" AND r.sdwt IN (SELECT sdwt FROM public.ref_sdwt WHERE site = @site AND is_use = 'Y')");
                cmd.Parameters.AddWithValue("site", site);
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] string site, [FromQuery] string sdwt)
        {
            var summary = new DashboardSummaryDto();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var totalMonitoredSql = new StringBuilder("SELECT COUNT(DISTINCT r.eqpid) FROM public.ref_equipment r JOIN public.agent_info a ON r.eqpid = a.eqpid WHERE 1=1");
            await using (var cmd = new NpgsqlCommand())
            {
                AddFilterLogic(totalMonitoredSql, cmd, site, sdwt);
                cmd.Connection = conn;
                cmd.CommandText = totalMonitoredSql.ToString();
                summary.TotalEqpCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var onlineAgentSql = new StringBuilder("SELECT COUNT(DISTINCT r.eqpid) FROM public.ref_equipment r JOIN public.agent_status s ON r.eqpid = s.eqpid WHERE s.status = 'ONLINE'");
            await using (var cmd = new NpgsqlCommand())
            {
                AddFilterLogic(onlineAgentSql, cmd, site, sdwt);
                cmd.Connection = conn;
                cmd.CommandText = onlineAgentSql.ToString();
                summary.OnlineAgentCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var todayErrorSql = new StringBuilder("SELECT COUNT(*) FROM public.plg_error e JOIN public.ref_equipment r ON e.eqpid = r.eqpid WHERE e.time_stamp >= CURRENT_DATE");
            await using (var cmd = new NpgsqlCommand())
            {
                AddFilterLogic(todayErrorSql, cmd, site, sdwt);
                cmd.Connection = conn;
                cmd.CommandText = todayErrorSql.ToString();
                summary.TodayErrorCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var todayDataSql = new StringBuilder("SELECT COUNT(*) FROM public.plg_wf_flat w JOIN public.ref_equipment r ON w.eqpid = r.eqpid WHERE w.serv_ts >= CURRENT_DATE");
            await using (var cmd = new NpgsqlCommand())
            {
                AddFilterLogic(todayDataSql, cmd, site, sdwt);
                cmd.Connection = conn;
                cmd.CommandText = todayDataSql.ToString();
                summary.TodayDataCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }

            return Ok(summary);
        }

        [HttpGet("agentstatus")]
        public async Task<ActionResult<IEnumerable<AgentStatusDto>>> GetAgentStatus([FromQuery] string site, [FromQuery] string sdwt)
        {
            var results = new List<AgentStatusDto>();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // ▼▼▼ [수정] 오늘 발생한 에러 건수를 COUNT하는 서브쿼리로 변경 ▼▼▼
            var sqlBuilder = new StringBuilder(@"
        SELECT 
            a.eqpid, 
            COALESCE(s.status, 'OFFLINE') = 'ONLINE' AS is_online, 
            s.last_perf_update AS last_contact,
            a.pc_name, 
            COALESCE(p.cpu_usage, 0) AS cpu_usage, 
            COALESCE(p.mem_usage, 0) AS mem_usage, 
            a.app_ver,
            a.type, a.ip_address, a.os, a.system_type, a.locale, a.timezone,
            COALESCE(e.alarm_count, 0) AS today_alarm_count  -- 에러 건수 (없으면 0)
        FROM public.agent_info a
        JOIN public.ref_equipment r ON a.eqpid = r.eqpid
        LEFT JOIN public.agent_status s ON a.eqpid = s.eqpid
        LEFT JOIN (
            SELECT eqpid, cpu_usage, mem_usage, ROW_NUMBER() OVER(PARTITION BY eqpid ORDER BY serv_ts DESC) as rn
            FROM public.eqp_perf
        ) p ON a.eqpid = p.eqpid AND p.rn = 1
        LEFT JOIN (
            SELECT eqpid, COUNT(*) AS alarm_count 
            FROM public.plg_error 
            WHERE time_stamp >= CURRENT_DATE -- 오늘 0시 이후 데이터
            GROUP BY eqpid
        ) e ON a.eqpid = e.eqpid
        WHERE 1=1");

            await using var cmd = new NpgsqlCommand();
            AddFilterLogic(sqlBuilder, cmd, site, sdwt);
            sqlBuilder.Append(" ORDER BY is_online DESC, a.eqpid;");

            cmd.Connection = conn;
            cmd.CommandText = sqlBuilder.ToString();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new AgentStatusDto
                {
                    EqpId = reader.GetString(0),
                    IsOnline = reader.GetBoolean(1),
                    LastContact = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    PcName = reader.GetString(3),
                    CpuUsage = reader.GetDouble(4),
                    MemoryUsage = reader.GetDouble(5),
                    AppVersion = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Type = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    IpAddress = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    Os = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    SystemType = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    Locale = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                    Timezone = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    // ▼▼▼ [수정] 조회된 알람 건수 값을 DTO에 할당 ▼▼▼
                    TodayAlarmCount = Convert.ToInt32(reader.GetInt64(13))
                });
            }
            return Ok(results);
        }

        [HttpGet("performancehistory/{eqpid}")]
        public async Task<ActionResult<IEnumerable<PerformanceDataPointDto>>> GetPerformanceHistory(string eqpid)
        {
            var results = new List<PerformanceDataPointDto>();
            if (string.IsNullOrEmpty(eqpid))
            {
                return BadRequest("EQP ID is required.");
            }

            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // [핵심 수정] 5분 단위로 데이터를 그룹화하고 평균을 계산하는 쿼리
            var sql = @"
                SELECT 
                    (timestamp 'epoch' + (floor(extract(epoch from serv_ts) / 300) * 300) * interval '1 second') as five_minute_interval,
                    AVG(cpu_usage) as avg_cpu,
                    AVG(mem_usage) as avg_mem
                FROM public.eqp_perf
                WHERE eqpid = @eqpid 
                  AND serv_ts > NOW() - INTERVAL '24 hours'
                GROUP BY five_minute_interval
                ORDER BY five_minute_interval;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("eqpid", eqpid);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new PerformanceDataPointDto
                {
                    Timestamp = reader.GetDateTime(0),
                    CpuUsage = reader.GetDouble(1),
                    MemoryUsage = reader.GetDouble(2)
                });
            }
            return Ok(results);
        }
    }
}
