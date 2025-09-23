// ITM.Dashboard.Api/Controllers/FiltersController.cs
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITM.Dashboard.Api.Models;
using ITM.Dashboard.Api;
using System.Text;
using System.Linq;

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

    [HttpGet("availablemetrics")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableMetrics(
        [FromQuery] string eqpid, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] string? lotId, [FromQuery] string? cassetteRcp, [FromQuery] string? stageGroup, [FromQuery] string? film)
    {
        var availableMetrics = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        // 1. 제외할 컬럼 목록 정의
        var excludedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eqpid", "lotid", "waferid", "serv_ts", "datetime", "cassettercp",
            "stagercp", "stagegroup", "film", "site", "sdwt", "point"
            // 필요에 따라 여기에 더 많은 컬럼 추가 가능
        };

        // 2. 현재 필터 조건으로 WHERE 절 구성
        var whereClauses = new List<string>();
        var parameters = new Dictionary<string, object>();

        void AddCondition(string? value, string columnName)
        {
            if (!string.IsNullOrEmpty(value))
            {
                whereClauses.Add($"{columnName} = @{columnName}");
                parameters[columnName] = value;
            }
        }
        AddCondition(eqpid, "eqpid");
        AddCondition(lotId, "lotid");
        AddCondition(cassetteRcp, "cassettercp");
        AddCondition(stageGroup, "stagegroup");
        AddCondition(film, "film");

        if (startDate.HasValue) { whereClauses.Add("serv_ts >= @startDate"); parameters["startDate"] = startDate.Value; }
        if (endDate.HasValue) { whereClauses.Add("serv_ts <= @endDate"); parameters["endDate"] = endDate.Value.AddDays(1).AddTicks(-1); }

        string whereQuery = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // 3. 테이블의 모든 숫자 타입 컬럼 목록 가져오기
        var allNumericColumns = new List<string>();
        var columnSql = @"
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name   = 'plg_wf_flat'
              AND data_type IN ('integer', 'bigint', 'smallint', 'numeric', 'real', 'double precision');";

        await using (var cmd = new NpgsqlCommand(columnSql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                allNumericColumns.Add(reader.GetString(0));
            }
        }

        // 4. 제외 목록에 없는 컬럼들만 대상으로 유효성 검사
        var potentialMetrics = allNumericColumns.Where(c => !excludedColumns.Contains(c)).ToList();

        foreach (var metric in potentialMetrics)
        {
            // [중요] SQL Injection을 방지하기 위해 컬럼 이름을 직접 쿼리에 넣지 않고 ""로 감쌉니다.
            var checkSql = $"SELECT 1 FROM public.plg_wf_flat {whereQuery} AND \"{metric}\" IS NOT NULL LIMIT 1;";
            await using var checkCmd = new NpgsqlCommand(checkSql, conn);
            foreach (var p in parameters)
            {
                checkCmd.Parameters.AddWithValue(p.Key, p.Value);
            }

            var result = await checkCmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                availableMetrics.Add(metric);
            }
        }

        return Ok(availableMetrics.OrderBy(m => m));
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
