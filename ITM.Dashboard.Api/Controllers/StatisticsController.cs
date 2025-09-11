// ITM.Dashboard.Api/Controllers/StatisticsController.cs

using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace ITM.Dashboard.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ILogger<StatisticsController> _logger; // <-- [추가] 로거 변수

        public StatisticsController(ILogger<StatisticsController> logger) // <-- [추가] 생성자 주입
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<StatisticsDto>> GetStatistics(
            [FromQuery] string lotId,
            [FromQuery] int waferId,
            [FromQuery] DateTime servTs,
            [FromQuery] DateTime dateTime,
            [FromQuery] string cassetteRcp,
            [FromQuery] string stageRcp,
            [FromQuery] string stageGroup,
            [FromQuery] string film)
        {
            // ▼▼▼ [추가] 수신된 파라미터 값을 로그로 기록합니다. ▼▼▼
            _logger.LogInformation("GetStatistics called with: LotId={lotId}, WaferId={waferId}, ServTs={servTs}, DateTime={dateTime}",
                lotId, waferId, servTs.ToString("o"), dateTime.ToString("o"));

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
                  AND serv_ts = @servTs
                  AND cassettercp = @cassetteRcp
                  AND stagercp = @stageRcp
                  AND stagegroup = @stageGroup
                  AND film = @film;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("lotId", lotId);
            cmd.Parameters.AddWithValue("waferId", waferId);
            cmd.Parameters.AddWithValue("servTs", servTs);
            cmd.Parameters.AddWithValue("dateTime", dateTime);
            cmd.Parameters.AddWithValue("cassetteRcp", cassetteRcp);
            cmd.Parameters.AddWithValue("stageRcp", stageRcp);
            cmd.Parameters.AddWithValue("stageGroup", stageGroup);
            cmd.Parameters.AddWithValue("film", film);

            var statistics = new StatisticsDto();
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _logger.LogInformation("Statistics data FOUND for LotId={lotId}", lotId);
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
            else
            {
                 _logger.LogWarning("Statistics data NOT FOUND for LotId={lotId}", lotId);
            }
            return Ok(statistics);
        }
    }
}
