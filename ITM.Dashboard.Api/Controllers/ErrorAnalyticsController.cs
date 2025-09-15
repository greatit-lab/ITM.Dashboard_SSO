// ITM.Dashboard.Api/Controllers/ErrorAnalyticsController.cs
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
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
        private string GetConnectionString() => new DatabaseInfo().GetConnectionString();

        private void AddFilterLogic(StringBuilder whereClause, NpgsqlCommand cmd, string site, string sdwt)
        {
            if (!string.IsNullOrEmpty(sdwt))
            {
                whereClause.Append(" AND r.sdwt = @sdwt");
                cmd.Parameters.AddWithValue("sdwt", sdwt);
            }
            else if (!string.IsNullOrEmpty(site))
            {
                whereClause.Append(" AND r.sdwt IN (SELECT sdwt FROM public.ref_sdwt WHERE site = @site)");
                cmd.Parameters.AddWithValue("site", site);
            }
        }

        private (string, NpgsqlCommand) BuildFilteredQuery(string baseSql, DateTime startDate, DateTime endDate, string site, string sdwt, string[] eqpids)
        {
            var cmd = new NpgsqlCommand();
            var whereClauses = new StringBuilder(" WHERE e.serv_ts >= @startDate AND e.serv_ts < @endDate");
            cmd.Parameters.AddWithValue("startDate", startDate.Date);
            cmd.Parameters.AddWithValue("endDate", endDate.Date.AddDays(1));

            AddFilterLogic(whereClauses, cmd, site, sdwt);

            if (eqpids != null && eqpids.Any() && !string.IsNullOrEmpty(eqpids[0]))
            {
                whereClauses.Append(" AND e.eqpid = ANY(@eqpids)");
                cmd.Parameters.AddWithValue("eqpids", eqpids);
            }

            return (baseSql + whereClauses.ToString(), cmd);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ErrorAnalyticsSummaryDto>> GetSummary(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string site, [FromQuery] string sdwt, [FromQuery] string[] eqpids)
        {
            var summary = new ErrorAnalyticsSummaryDto();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            
            // [수정] INNER JOIN을 LEFT JOIN으로 변경
            string baseFrom = " FROM public.plg_error e LEFT JOIN public.ref_equipment r ON e.eqpid = r.eqpid ";

            var (totalCountSql, totalCountCmd) = BuildFilteredQuery("SELECT COUNT(*) " + baseFrom, startDate, endDate, site, sdwt, eqpids);
            totalCountCmd.Connection = conn;
            summary.TotalErrorCount = Convert.ToInt64(await totalCountCmd.ExecuteScalarAsync());

            var (eqpCountSql, eqpCountCmd) = BuildFilteredQuery("SELECT COUNT(DISTINCT e.eqpid) " + baseFrom, startDate, endDate, site, sdwt, eqpids);
            eqpCountCmd.Connection = conn;
            summary.ErrorEqpCount = Convert.ToInt32(await eqpCountCmd.ExecuteScalarAsync());

            var (topErrorSql, topErrorCmd) = BuildFilteredQuery("SELECT e.error_id, COUNT(*) as count " + baseFrom, startDate, endDate, site, sdwt, eqpids);
            topErrorCmd.CommandText = topErrorSql + " GROUP BY e.error_id ORDER BY count DESC LIMIT 1";
            topErrorCmd.Connection = conn;
            await using (var reader = await topErrorCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    summary.TopErrorId = reader.IsDBNull(0) ? "N/A" : reader.GetString(0);
                    summary.TopErrorCount = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetInt64(1));
                }
            }

            var (byEqpSql, byEqpCmd) = BuildFilteredQuery("SELECT e.eqpid, COUNT(*) as count " + baseFrom, startDate, endDate, site, sdwt, eqpids);
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
            
            // [수정] INNER JOIN을 LEFT JOIN으로 변경
            string baseFrom = " FROM public.plg_error e LEFT JOIN public.ref_equipment r ON e.eqpid = r.eqpid ";
            var (sql, cmd) = BuildFilteredQuery("SELECT DATE_TRUNC('day', e.serv_ts) as day, COUNT(*) as count" + baseFrom, startDate, endDate, site, sdwt, eqpids);
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
        public async Task<ActionResult> GetErrorLogs(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string site, [FromQuery] string sdwt, [FromQuery] string[] eqpids, 
            [FromQuery] int page = 0, [FromQuery] int pageSize = 10)
        {
            var results = new List<ErrorLogDto>();
            long totalItems = 0;
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            
            // [수정] INNER JOIN을 LEFT JOIN으로 변경
            string baseFrom = " FROM public.plg_error e LEFT JOIN public.ref_equipment r ON e.eqpid = r.eqpid ";

            var (countSql, countCmd) = BuildFilteredQuery("SELECT COUNT(*)" + baseFrom, startDate, endDate, site, sdwt, eqpids);
            countCmd.Connection = conn;
            totalItems = Convert.ToInt64(await countCmd.ExecuteScalarAsync());

            var (sql, cmd) = BuildFilteredQuery("SELECT e.serv_ts, e.eqpid, e.error_id, e.error_label, e.error_desc" + baseFrom, startDate, endDate, site, sdwt, eqpids);
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
                    ErrorDesc = reader.GetString(4)
                });
            }
            return Ok(new { items = results, totalItems });
        }
    }
}
