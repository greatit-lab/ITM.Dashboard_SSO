// ITM.Dashboard.Api/Controllers/ErrorAnalyticsController.cs
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITM.Dashboard.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ErrorAnalyticsController : ControllerBase
    {
        private readonly ILogger<ErrorAnalyticsController> _logger;
        private string GetConnectionString() => new DatabaseInfo().GetConnectionString();

        public ErrorAnalyticsController(ILogger<ErrorAnalyticsController> logger)
        {
            _logger = logger;
        }

        private (string, NpgsqlCommand) BuildFilteredQuery(string selectClause, DateTime startDate, DateTime endDate, string site, string sdwt, string[] eqpids)
        {
            var cmd = new NpgsqlCommand();
            var sqlBuilder = new StringBuilder(selectClause);
            sqlBuilder.Append(" FROM public.plg_error e WHERE e.serv_ts >= @startDate AND e.serv_ts < @endDate");

            cmd.Parameters.AddWithValue("startDate", startDate.Date);
            cmd.Parameters.AddWithValue("endDate", endDate.Date.AddDays(1));

            if (eqpids != null && eqpids.Any() && !string.IsNullOrEmpty(eqpids[0]))
            {
                sqlBuilder.Append(" AND e.eqpid = ANY(@eqpids)");
                cmd.Parameters.AddWithValue("eqpids", eqpids);
            }
            else if (!string.IsNullOrEmpty(sdwt))
            {
                sqlBuilder.Append(" AND e.eqpid IN (SELECT eqpid FROM public.ref_equipment WHERE sdwt = @sdwt)");
                cmd.Parameters.AddWithValue("sdwt", sdwt);
            }
            else if (!string.IsNullOrEmpty(site))
            {
                sqlBuilder.Append(" AND e.eqpid IN (SELECT r.eqpid FROM public.ref_equipment r JOIN public.ref_sdwt s ON r.sdwt = s.sdwt WHERE s.site = @site)");
                cmd.Parameters.AddWithValue("site", site);
            }

            return (sqlBuilder.ToString(), cmd);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ErrorAnalyticsSummaryDto>> GetSummary(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string site, [FromQuery] string sdwt, [FromQuery] string[] eqpids)
        {
            var summary = new ErrorAnalyticsSummaryDto();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var (totalCountSql, totalCountCmd) = BuildFilteredQuery("SELECT COUNT(*)", startDate, endDate, site, sdwt, eqpids);
            totalCountCmd.CommandText = totalCountSql; // [수정] CommandText 할당
            totalCountCmd.Connection = conn;
            summary.TotalErrorCount = Convert.ToInt64(await totalCountCmd.ExecuteScalarAsync());

            var (eqpCountSql, eqpCountCmd) = BuildFilteredQuery("SELECT COUNT(DISTINCT e.eqpid)", startDate, endDate, site, sdwt, eqpids);
            eqpCountCmd.CommandText = eqpCountSql; // [수정] CommandText 할당
            eqpCountCmd.Connection = conn;
            summary.ErrorEqpCount = Convert.ToInt32(await eqpCountCmd.ExecuteScalarAsync());

            var (topErrorSql, topErrorCmd) = BuildFilteredQuery("SELECT e.error_id, e.error_label, COUNT(*) as count", startDate, endDate, site, sdwt, eqpids);
            topErrorCmd.CommandText = topErrorSql + " GROUP BY e.error_id, e.error_label ORDER BY count DESC LIMIT 1"; // GROUP BY에 error_label 추가
            topErrorCmd.Connection = conn;
            await using (var reader = await topErrorCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    summary.TopErrorId = reader.IsDBNull(0) ? "N/A" : reader.GetString(0);
                    summary.TopErrorLabel = reader.IsDBNull(1) ? string.Empty : reader.GetString(1); // [추가] TopErrorLabel 값 할당
                    summary.TopErrorCount = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetInt64(2)); // 인덱스 1 -> 2로 변경
                }
            }

            var (byEqpSql, byEqpCmd) = BuildFilteredQuery("SELECT e.eqpid, COUNT(*) as count", startDate, endDate, site, sdwt, eqpids);
            byEqpCmd.CommandText = byEqpSql + " GROUP BY e.eqpid ORDER BY count DESC";
            byEqpCmd.Connection = conn;
            await using (var reader = await byEqpCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    summary.ErrorCountByEqp.Add(new ChartDataItem
                    {
                        Label = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                        Value = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetInt64(1))
                    });
                }
            }

            return Ok(summary);
        }

        [HttpGet("trend")]
        public async Task<ActionResult<IEnumerable<ErrorTrendDataPointDto>>> GetErrorTrend(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string site, [FromQuery] string sdwt, [FromQuery] string[] eqpids)
        {
            var results = new List<ErrorTrendDataPointDto>();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var (sql, cmd) = BuildFilteredQuery("SELECT DATE_TRUNC('day', e.serv_ts) as day, COUNT(*) as count", startDate, endDate, site, sdwt, eqpids);
            cmd.CommandText = sql + " GROUP BY day ORDER BY day";
            cmd.Connection = conn;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ErrorTrendDataPointDto
                {
                    Date = reader.GetDateTime(0),
                    Count = reader.GetInt64(1)
                });
            }
            return Ok(results);
        }

        [HttpGet("logs")]
        public async Task<ActionResult<PagedResult<ErrorLogDto>>> GetErrorLogs(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string site, [FromQuery] string sdwt, [FromQuery] string[] eqpids,
            [FromQuery] int page = 0, [FromQuery] int pageSize = 10)
        {
            var results = new List<ErrorLogDto>();
            long totalItems = 0;
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var (countSql, countCmd) = BuildFilteredQuery("SELECT COUNT(*)", startDate, endDate, site, sdwt, eqpids);
            countCmd.CommandText = countSql; // [수정] CommandText 할당
            countCmd.Connection = conn;
            totalItems = Convert.ToInt64(await countCmd.ExecuteScalarAsync());

            var (sql, cmd) = BuildFilteredQuery("SELECT e.serv_ts, e.eqpid, e.error_id, e.error_label, e.error_desc, e.extra_message_1, e.extra_message_2", startDate, endDate, site, sdwt, eqpids);
            cmd.CommandText = sql + " ORDER BY e.serv_ts DESC OFFSET @offset LIMIT @pageSize";
            cmd.Parameters.AddWithValue("offset", page * pageSize);
            cmd.Parameters.AddWithValue("pageSize", pageSize);
            cmd.Connection = conn;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ErrorLogDto
                {
                    TimeStamp = reader.GetDateTime(0),
                    EqpId = reader.GetString(1),
                    ErrorId = reader.GetString(2),
                    ErrorLabel = reader.GetString(3),
                    ErrorDesc = reader.GetString(4),
                    // ▼▼▼ [추가] 조회된 추가 메시지 값을 DTO에 할당합니다. ▼▼▼
                    ExtraMessage1 = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    ExtraMessage2 = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                });
            }
            return Ok(new PagedResult<ErrorLogDto> { Items = results, TotalItems = totalItems });
        }
    }
}
