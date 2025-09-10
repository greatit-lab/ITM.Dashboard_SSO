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
    // Site 목록 조회 API
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

    // 특정 Site에 속한 SDWT 목록 조회 API
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

    // 특정 SDWT에 속하고, 실제 데이터가 있는 EQPID 목록 조회 API
    [HttpGet("eqpids/{sdwt}")]
    public async Task<ActionResult<IEnumerable<string>>> GetEqpids(string sdwt)
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();
        var sql = @"
            SELECT DISTINCT T1.eqpid
            FROM public.ref_equipment AS T1
            INNER JOIN public.plg_wf_flat AS T2 ON T1.eqpid = T2.eqpid
            WHERE T1.sdwt = @sdwt
            ORDER BY T1.eqpid;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("sdwt", sdwt);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) { results.Add(reader.GetString(0)); }
        return Ok(results);
    }

    // 특정 EQPID의 데이터 기간(최소/최대) 조회 API
    [HttpGet("daterange")]
    public async Task<ActionResult<DateRangeDto>> GetDataDateRange([FromQuery] string? eqpid)
    {
        if (string.IsNullOrEmpty(eqpid))
        {
            return Ok(new DateRangeDto());
        }

        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        var sql = @"
            SELECT MIN(serv_ts), MAX(serv_ts) 
            FROM public.plg_wf_flat 
            WHERE eqpid = @eqpid;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("eqpid", eqpid);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            DateTime? dbMinDate = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0);
            DateTime? dbMaxDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);

            DateTime thirtyDaysAgo = DateTime.Today.AddDays(-30);

            DateTime? defaultStartDate = (dbMinDate.HasValue && dbMinDate.Value > thirtyDaysAgo) 
                                         ? dbMinDate.Value
                                         : thirtyDaysAgo;

            var dateRange = new DateRangeDto
            {
                MinDate = defaultStartDate,
                MaxDate = dbMaxDate
            };
            return Ok(dateRange);
        }

        return Ok(new DateRangeDto());
    }

    // 상세 필터 목록 조회를 위한 공용 메서드
    private async Task<ActionResult<IEnumerable<string>>> GetDistinctColumnValues(string columnName, string eqpid, string? lotId = null)
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        var allowedColumns = new List<string> { "cassettercp", "stagercp", "stagegroup", "film", "lotid" };
        if (!allowedColumns.Contains(columnName.ToLower()))
        {
            return BadRequest("Invalid column name.");
        }

        var sqlBuilder = new StringBuilder($"SELECT DISTINCT {columnName} FROM public.plg_wf_flat WHERE eqpid = @eqpid AND {columnName} IS NOT NULL ");
        if (!string.IsNullOrEmpty(lotId))
        {
            sqlBuilder.Append("AND lotid = @lotid ");
        }
        sqlBuilder.Append($"ORDER BY {columnName};");

        await using var cmd = new NpgsqlCommand(sqlBuilder.ToString(), conn);
        cmd.Parameters.AddWithValue("eqpid", eqpid);
        if (!string.IsNullOrEmpty(lotId))
        {
            cmd.Parameters.AddWithValue("lotid", lotId);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) { results.Add(reader.GetString(0)); }
        return Ok(results);
    }

    // 상세 필터 API 목록
    [HttpGet("cassettercps/{eqpid}")]
    public Task<ActionResult<IEnumerable<string>>> GetCassetteRcps(string eqpid) => GetDistinctColumnValues("cassettercp", eqpid);

    [HttpGet("stagercps/{eqpid}")]
    public Task<ActionResult<IEnumerable<string>>> GetStageRcps(string eqpid) => GetDistinctColumnValues("stagercp", eqpid);

    [HttpGet("stagegroups/{eqpid}")]
    public Task<ActionResult<IEnumerable<string>>> GetStageGroups(string eqpid) => GetDistinctColumnValues("stagegroup", eqpid);

    [HttpGet("films/{eqpid}")]
    public Task<ActionResult<IEnumerable<string>>> GetFilms(string eqpid) => GetDistinctColumnValues("film", eqpid);

    [HttpGet("lotids/{eqpid}")]
    public Task<ActionResult<IEnumerable<string>>> GetLotIds(string eqpid) => GetDistinctColumnValues("lotid", eqpid);

    // 특정 Lot ID에 속한 Wafer ID 목록 조회 API (숫자 정렬 적용)
    [HttpGet("waferids/{eqpid}/{lotid}")]
    public async Task<ActionResult<IEnumerable<string>>> GetWaferIds(string eqpid, string lotid)
    {
        var results = new List<string>();
        var dbInfo = DatabaseInfo.CreateDefault();
        await using var conn = new NpgsqlConnection(dbInfo.GetConnectionString());
        await conn.OpenAsync();

        var sql = @"
            SELECT DISTINCT waferid 
            FROM public.plg_wf_flat 
            WHERE eqpid = @eqpid AND lotid = @lotid AND waferid IS NOT NULL 
            ORDER BY waferid;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("eqpid", eqpid);
        cmd.Parameters.AddWithValue("lotid", lotid);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetInt32(0).ToString());
        }
        return Ok(results);
    }
}
