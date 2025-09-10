// 파일 경로: ITM.Dashboard.Api/Controllers/WaferDataController.cs
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITM.Dashboard.Api;

[Route("api/[controller]")]
[ApiController]
public class WaferDataController : ControllerBase
{
    [HttpGet("flatdata")]
    public async Task<ActionResult> GetWaferFlatDataPaged(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? eqpid = null,
        [FromQuery] string? lotid = null,
        [FromQuery] int? waferid = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? cassettercp = null,
        [FromQuery] string? stagercp = null,
        [FromQuery] string? stagegroup = null,
        [FromQuery] string? film = null
        )
    {
        var results = new List<WaferFlatDataDto>();
        long totalItems = 0;

        var dbInfo = new DatabaseInfo();
        var connectionString = dbInfo.GetConnectionString();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var whereClauses = new List<string>();
        var parameters = new Dictionary<string, object>();

        // 동적으로 WHERE 조건 생성
        if (!string.IsNullOrEmpty(eqpid)) { whereClauses.Add("eqpid = @eqpid"); parameters["eqpid"] = eqpid; }
        if (!string.IsNullOrEmpty(lotid)) { whereClauses.Add("lotid ILIKE @lotid"); parameters["lotid"] = $"%{lotid}%"; }
        if (waferid.HasValue) { whereClauses.Add("waferid = @waferid"); parameters["waferid"] = waferid.Value; }
        if (startDate.HasValue) { whereClauses.Add("datetime >= @startDate"); parameters["startDate"] = startDate.Value; }
        if (endDate.HasValue) { whereClauses.Add("datetime <= @endDate"); parameters["endDate"] = endDate.Value.AddDays(1).AddTicks(-1); }
        if (!string.IsNullOrEmpty(cassettercp)) { whereClauses.Add("cassettercp = @cassettercp"); parameters["cassettercp"] = cassettercp; }
        if (!string.IsNullOrEmpty(stagercp)) { whereClauses.Add("stagercp = @stagercp"); parameters["stagercp"] = stagercp; }
        if (!string.IsNullOrEmpty(stagegroup)) { whereClauses.Add("stagegroup = @stagegroup"); parameters["stagegroup"] = stagegroup; }
        if (!string.IsNullOrEmpty(film)) { whereClauses.Add("film = @film"); parameters["film"] = film; }

        string whereQuery = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $"SELECT COUNT(*) FROM public.plg_wf_flat {whereQuery};";
        await using (var countCmd = new NpgsqlCommand(countSql, conn))
        {
            foreach (var p in parameters) { countCmd.Parameters.AddWithValue(p.Key, p.Value); }
            var result = await countCmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value) { totalItems = Convert.ToInt64(result); }
        }

        var dataSql = $"SELECT lotid, waferid, datetime, point, x, y, cassettercp, stagercp, stagegroup, film FROM public.plg_wf_flat {whereQuery} ORDER BY datetime DESC OFFSET @Offset LIMIT @PageSize;";
        
        await using var cmd = new NpgsqlCommand(dataSql, conn);
        foreach (var p in parameters) { cmd.Parameters.AddWithValue(p.Key, p.Value); }
        cmd.Parameters.AddWithValue("Offset", page * pageSize);
        cmd.Parameters.AddWithValue("PageSize", pageSize);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new WaferFlatDataDto
            {
                LotId = reader.IsDBNull(0) ? null : reader.GetString(0),
                WaferId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                DateTime = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                Point = reader.GetInt32(3),
                X = reader.GetDouble(4),
                Y = reader.GetDouble(5),
                CassetteRcp = reader.IsDBNull(6) ? null : reader.GetString(6),
                StageRcp = reader.IsDBNull(7) ? null : reader.GetString(7),
                StageGroup = reader.IsDBNull(8) ? null : reader.GetString(8),
                Film = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }
        
        return Ok(new { items = results, totalItems });
    }
}
