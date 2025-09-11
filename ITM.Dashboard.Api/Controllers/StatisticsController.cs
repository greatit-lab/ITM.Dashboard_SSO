// ITM.Dashboard.Api/Controllers/StatisticsController.cs

using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace ITM.Dashboard.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<StatisticsDto>> GetStatistics(
            [FromQuery] string lotId,
            [FromQuery] int waferId,
            [FromQuery] DateTime dateTime,
            [FromQuery] string cassetteRcp,
            [FromQuery] string stageRcp,
            [FromQuery] string stageGroup,
            [FromQuery] string film)
        {
            var dbInfo = new DatabaseInfo();
            var connectionString = dbInfo.GetConnectionString();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    MAX(t1), MIN(t1), AVG(t1), STDDEV_SAMP(t1),
                    MAX(gof), MIN(gof), AVG(gof), STDDEV_SAMP(gof),
                    MAX(z), MIN(z), AVG(z), STDDEV_SAMP(z),
                    MAX(srvisz), MIN(srvisz), AVG(srvisz), STDDEV_SAMP(srvisz)
                FROM public.plg_wf_flat
                WHERE lotid = @lotId
                  AND waferid = @waferId
                  AND datetime = @dateTime
                  AND cassettercp = @cassetteRcp
                  AND stagercp = @stageRcp
                  AND stagegroup = @stageGroup
                  AND film = @film;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("lotId", lotId);
            cmd.Parameters.AddWithValue("waferId", waferId);
            cmd.Parameters.AddWithValue("dateTime", dateTime);
            cmd.Parameters.AddWithValue("cassetteRcp", cassetteRcp);
            cmd.Parameters.AddWithValue("stageRcp", stageRcp);
            cmd.Parameters.AddWithValue("stageGroup", stageGroup);
            cmd.Parameters.AddWithValue("film", film);

            var statistics = new StatisticsDto();
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                // Helper function to safely read double values
                double SafeGetDouble(int index) => reader.IsDBNull(index) ? 0 : reader.GetDouble(index);

                statistics.T1.Max = SafeGetDouble(0);
                statistics.T1.Min = SafeGetDouble(1);
                statistics.T1.Mean = SafeGetDouble(2);
                statistics.T1.StdDev = SafeGetDouble(3);

                statistics.Gof.Max = SafeGetDouble(4);
                statistics.Gof.Min = SafeGetDouble(5);
                statistics.Gof.Mean = SafeGetDouble(6);
                statistics.Gof.StdDev = SafeGetDouble(7);

                statistics.Z.Max = SafeGetDouble(8);
                statistics.Z.Min = SafeGetDouble(9);
                statistics.Z.Mean = SafeGetDouble(10);
                statistics.Z.StdDev = SafeGetDouble(11);

                statistics.Srvisz.Max = SafeGetDouble(12);
                statistics.Srvisz.Min = SafeGetDouble(13);
                statistics.Srvisz.Mean = SafeGetDouble(14);
                statistics.Srvisz.StdDev = SafeGetDouble(15);
            }

            return Ok(statistics);
        }
    }
}
