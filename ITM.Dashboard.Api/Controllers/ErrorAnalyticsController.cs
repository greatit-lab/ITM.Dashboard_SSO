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

        private (string, NpgsqlCommand) BuildFilteredQuery(string baseSql, DateTime startDate, DateTime endDate, [FromQuery] string[] eqpids)
        {
            var cmd = new NpgsqlCommand();
            var whereClauses = new List<string> { "time_stamp >= @startDate", "time_stamp < @endDate" };
            cmd.Parameters.AddWithValue("startDate", startDate.Date);
            cmd.Parameters.AddWithValue("endDate", endDate.Date.AddDays(1));

            if (eqpids != null && eqpids.Any())
            {
                whereClauses.Add("eqpid = ANY(@eqpids)");
                cmd.Parameters.AddWithValue("eqpids", eqpids);
            }

            return (baseSql + " WHERE " + string.Join(" AND ", whereClauses), cmd);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ErrorAnalyticsSummaryDto>> GetSummary(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string[] eqpids)
        {
            var summary = new ErrorAnalyticsSummaryDto();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // Total Error Count
            var (totalCountSql, totalCountCmd) = BuildFilteredQuery("SELECT COUNT(*) FROM public.plg_error", startDate, endDate, eqpids);
            totalCountCmd.Connection = conn;
            summary.TotalErrorCount = Convert.ToInt64(await totalCountCmd.ExecuteScalarAsync());

            // Error EQP Count
            var (eqpCountSql, eqpCountCmd) = BuildFilteredQuery("SELECT COUNT(DISTINCT eqpid) FROM public.plg_error", startDate, endDate, eqpids);
            eqpCountCmd.Connection = conn;
            summary.ErrorEqpCount = Convert.ToInt32(await eqpCountCmd.ExecuteScalarAsync());

            // Top Error
            var (topErrorSql, topErrorCmd) = BuildFilteredQuery("SELECT error_id, COUNT(*) as count FROM public.plg_error", startDate, endDate, eqpids);
            var topErrorSqlFinal = topErrorSql + " GROUP BY error_id ORDER BY count DESC LIMIT 1";
            topErrorCmd.Connection = conn;
            topErrorCmd.CommandText = topErrorSqlFinal;
            await using (var reader = await topErrorCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    summary.TopErrorId = reader.IsDBNull(0) ? "N/A" : reader.GetString(0);
                    summary.TopErrorCount = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetInt64(1));
                }
            }

            // Error Count by EQP
            var (byEqpSql, byEqpCmd) = BuildFilteredQuery("SELECT eqpid, COUNT(*) as count FROM public.plg_error", startDate, endDate, eqpids);
            var byEqpSqlFinal = byEqpSql + " GROUP BY eqpid ORDER BY count DESC";
            byEqpCmd.Connection = conn;
            byEqpCmd.CommandText = byEqpSqlFinal;
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
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string[] eqpids)
        {
            var results = new List<ErrorTrendDataPointDto>();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var (sql, cmd) = BuildFilteredQuery("SELECT DATE_TRUNC('day', time_stamp) as day, COUNT(*) as count FROM public.plg_error", startDate, endDate, eqpids);
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
        public async Task<ActionResult<IEnumerable<ErrorLogDto>>> GetErrorLogs(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string[] eqpids,
            [FromQuery] int page = 0, [FromQuery] int pageSize = 20)
        {
            var results = new List<ErrorLogDto>();
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var (sql, cmd) = BuildFilteredQuery("SELECT time_stamp, eqpid, error_id, error_label, error_desc FROM public.plg_error", startDate, endDate, eqpids);
            cmd.CommandText = sql + " ORDER BY time_stamp DESC OFFSET @offset LIMIT @pageSize";
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
            return Ok(results);
        }
    }
}
