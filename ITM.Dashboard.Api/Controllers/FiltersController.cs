// ITM.Dashboard.Api/Controllers/FiltersController.cs
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITM.Dashboard.Api.Models;
using ITM.Dashboard.Api;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class FiltersController : ControllerBase
{
    // Site 목록 조회 API (변경 없음)
    [HttpGet("sites")]
    public async Task<ActionResult<IEnumerable<string>>> GetSites()
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();
        var sql = "SELECT DISTINCT site FROM public.ref_sdwt WHERE is_use = 'Y' ORDER BY site;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) { results.Add(reader.GetString(0)); }
        return Ok(results);
    }

    // 특정 Site에 속한 SDWT 목록 조회 API (변경 없음)
    [HttpGet("sdwts/{site}")]
    public async Task<ActionResult<IEnumerable<string>>> GetSdwts(string site)
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();
        var sql = "SELECT DISTINCT sdwt FROM public.ref_sdwt WHERE site = @site AND is_use = 'Y' ORDER BY sdwt;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("site", site);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) { results.Add(reader.GetString(0)); }
        return Ok(results);
    }

    // ▼▼▼ [수정] 데이터의 대소문자 및 공백 차이로 인해 JOIN이 실패하는 문제를 해결합니다. ▼▼▼
    [HttpGet("eqpids/{sdwt?}")]
    public async Task<ActionResult<IEnumerable<string>>> GetEqpids(string sdwt)
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        // [핵심 수정] UPPER()와 TRIM() 함수를 사용하여 데이터 불일치 문제를 해결합니다.
        var sql = new StringBuilder(@"
            SELECT DISTINCT T1.eqpid
            FROM public.ref_equipment AS T1
            INNER JOIN public.plg_wf_flat AS T2 
                ON UPPER(TRIM(T1.eqpid)) = UPPER(TRIM(T2.eqpid))");

        if (!string.IsNullOrEmpty(sdwt))
        {
            sql.Append(" WHERE UPPER(TRIM(T1.sdwt)) = UPPER(TRIM(@sdwt))");
        }
        sql.Append(" ORDER BY T1.eqpid;");

        await using var cmd = new NpgsqlCommand(sql.ToString(), conn);
        if (!string.IsNullOrEmpty(sdwt))
        {
            cmd.Parameters.AddWithValue("sdwt", sdwt);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }
        return Ok(results);
    }

    // 특정 EQPID의 데이터 기간(최소/최대) 조회 API (변경 없음)
    [HttpGet("daterange")]
    public async Task<ActionResult<DateRangeDto>> GetDataDateRange([FromQuery] string? eqpid)
    {
        if (string.IsNullOrEmpty(eqpid)) { return Ok(new DateRangeDto()); }
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();
        var sql = @"SELECT MIN(serv_ts), MAX(serv_ts) FROM public.plg_wf_flat WHERE eqpid = @eqpid;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("eqpid", eqpid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync() && !reader.IsDBNull(0) && !reader.IsDBNull(1))
        {
            return Ok(new DateRangeDto { MinDate = reader.GetDateTime(0), MaxDate = reader.GetDateTime(1) });
        }
        return Ok(new DateRangeDto());
    }

    [HttpGet("eqpidsbysite/{site}")]
    public async Task<ActionResult<IEnumerable<string>>> GetEqpidsBySite(string site)
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        var sql = @"
            SELECT T1.eqpid
            FROM public.ref_equipment AS T1
            INNER JOIN public.ref_sdwt AS T2 ON T1.sdwt = T2.sdwt
            WHERE T2.site = @site
            ORDER BY T1.eqpid;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("site", site);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }
        return Ok(results);
    }

    // ▼▼▼ [수정] 모든 필터 값을 받아 동적으로 쿼리하는 새로운 공용 메서드 ▼▼▼
    private async Task<ActionResult<IEnumerable<string>>> GetFilteredDistinctValues(
        string targetColumn,
        [FromQuery] string eqpid,
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] string? lotId, [FromQuery] int? waferId,
        [FromQuery] string? cassetteRcp, [FromQuery] string? stageRcp,
        [FromQuery] string? stageGroup, [FromQuery] string? film)
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        var whereClauses = new List<string> { "eqpid = @eqpid", $"{targetColumn} IS NOT NULL" };
        var parameters = new Dictionary<string, object> { { "eqpid", eqpid } };

        void AddCondition(string? value, string columnName, bool isNumeric = false)
        {
            if (string.IsNullOrEmpty(value) || columnName == targetColumn) return;
            if (isNumeric && int.TryParse(value, out var numValue))
            {
                whereClauses.Add($"{columnName} = @{columnName}");
                parameters[columnName] = numValue;
            }
            else if (!isNumeric)
            {
                whereClauses.Add($"{columnName} = @{columnName}");
                parameters[columnName] = value;
            }
        }

        if (startDate.HasValue) { whereClauses.Add("serv_ts >= @startDate"); parameters["startDate"] = startDate.Value; }
        if (endDate.HasValue) { whereClauses.Add("serv_ts <= @endDate"); parameters["endDate"] = endDate.Value.AddDays(1).AddTicks(-1); }

        AddCondition(lotId, "lotid");
        AddCondition(cassetteRcp, "cassettercp");
        AddCondition(stageRcp, "stagercp");
        AddCondition(stageGroup, "stagegroup");
        AddCondition(film, "film");

        if (waferId.HasValue && "waferid" != targetColumn)
        {
            whereClauses.Add("waferid = @waferid");
            parameters["waferid"] = waferId.Value;
        }

        var whereQuery = "WHERE " + string.Join(" AND ", whereClauses);
        var sql = $"SELECT DISTINCT {targetColumn} FROM public.plg_wf_flat {whereQuery} ORDER BY {targetColumn};";

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var p in parameters) { cmd.Parameters.AddWithValue(p.Key, p.Value); }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                results.Add(reader[0].ToString()!);
            }
        }
        return Ok(results);
    }

    // ▼▼▼ [수정] 각 필터 API가 새로운 공용 메서드를 호출하도록 변경 ▼▼▼
    [HttpGet("cassettercps")]
    public Task<ActionResult<IEnumerable<string>>> GetCassetteRcps([FromQuery] string eqpid, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? lotId, [FromQuery] int? waferId, [FromQuery] string? cassetteRcp, [FromQuery] string? stageRcp, [FromQuery] string? stageGroup, [FromQuery] string? film)
        => GetFilteredDistinctValues("cassettercp", eqpid, startDate, endDate, lotId, waferId, cassetteRcp, stageRcp, stageGroup, film);

    [HttpGet("stagercps")]
    public Task<ActionResult<IEnumerable<string>>> GetStageRcps([FromQuery] string eqpid, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? lotId, [FromQuery] int? waferId, [FromQuery] string? cassetteRcp, [FromQuery] string? stageRcp, [FromQuery] string? stageGroup, [FromQuery] string? film)
        => GetFilteredDistinctValues("stagercp", eqpid, startDate, endDate, lotId, waferId, cassetteRcp, stageRcp, stageGroup, film);

    [HttpGet("stagegroups")]
    public Task<ActionResult<IEnumerable<string>>> GetStageGroups([FromQuery] string eqpid, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? lotId, [FromQuery] int? waferId, [FromQuery] string? cassetteRcp, [FromQuery] string? stageRcp, [FromQuery] string? stageGroup, [FromQuery] string? film)
        => GetFilteredDistinctValues("stagegroup", eqpid, startDate, endDate, lotId, waferId, cassetteRcp, stageRcp, stageGroup, film);

    [HttpGet("films")]
    public Task<ActionResult<IEnumerable<string>>> GetFilms([FromQuery] string eqpid, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? lotId, [FromQuery] int? waferId, [FromQuery] string? cassetteRcp, [FromQuery] string? stageRcp, [FromQuery] string? stageGroup, [FromQuery] string? film)
        => GetFilteredDistinctValues("film", eqpid, startDate, endDate, lotId, waferId, cassetteRcp, stageRcp, stageGroup, film);

    [HttpGet("lotids")]
    public Task<ActionResult<IEnumerable<string>>> GetLotIds([FromQuery] string eqpid, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? lotId, [FromQuery] int? waferId, [FromQuery] string? cassetteRcp, [FromQuery] string? stageRcp, [FromQuery] string? stageGroup, [FromQuery] string? film)
        => GetFilteredDistinctValues("lotid", eqpid, startDate, endDate, lotId, waferId, cassetteRcp, stageRcp, stageGroup, film);

    [HttpGet("waferids")]
    public Task<ActionResult<IEnumerable<string>>> GetWaferIds([FromQuery] string eqpid, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? lotId, [FromQuery] int? waferId, [FromQuery] string? cassetteRcp, [FromQuery] string? stageRcp, [FromQuery] string? stageGroup, [FromQuery] string? film)
        => GetFilteredDistinctValues("waferid", eqpid, startDate, endDate, lotId, waferId, cassetteRcp, stageRcp, stageGroup, film);
}
