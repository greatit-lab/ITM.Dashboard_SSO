// ITM.Dashboard.Api/Controllers/PreAlignAnalyticsController.cs
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ITM.Dashboard.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreAlignAnalyticsController : ControllerBase
    {
        private string GetConnectionString() => new DatabaseInfo().GetConnectionString();

        [HttpGet("data")]
        public async Task<ActionResult<IEnumerable<PreAlignDataDto>>> GetPreAlignData(
            [FromQuery] string eqpid, [FromQuery] string startDate, [FromQuery] string endDate)
        {
            var results = new List<PreAlignDataDto>();
            if (string.IsNullOrEmpty(eqpid))
            {
                return Ok(results);
            }

            if (!DateTime.TryParse(startDate, out var start) || !DateTime.TryParse(endDate, out var end))
            {
                return BadRequest("Invalid date format. Please use 'yyyy-MM-dd'.");
            }

            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = @"
                SELECT serv_ts, xmm, ymm, notch
                FROM public.plg_prealign
                WHERE eqpid = @eqpid
                  AND serv_ts >= @startDate
                  AND serv_ts <= @endDate
                ORDER BY serv_ts;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("eqpid", eqpid);
            cmd.Parameters.AddWithValue("startDate", start.Date);
            cmd.Parameters.AddWithValue("endDate", end.Date.AddDays(1));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new PreAlignDataDto
                {
                    Timestamp = reader.GetDateTime(0),
                    Xmm = reader.GetDouble(1),
                    Ymm = reader.GetDouble(2),
                    Notch = reader.GetDouble(3)
                });
            }
            return Ok(results);
        }
    }
}
