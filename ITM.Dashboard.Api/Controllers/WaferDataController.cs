// ITM.Dashboard.Api/Controllers/WaferDataController.cs
using ITM.Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdfiumViewer;
using System.IO;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Runtime.Versioning;

namespace ITM.Dashboard.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WaferDataController : ControllerBase
    {
        private readonly ILogger<WaferDataController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WaferDataController(ILogger<WaferDataController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("pointdata")]
        public async Task<ActionResult<PointDataResponseDto>> GetPointData(
            [FromQuery] string lotId, [FromQuery] int waferId, [FromQuery] DateTime servTs,
            [FromQuery] DateTime dateTime, [FromQuery] string cassetteRcp, [FromQuery] string stageRcp,
            [FromQuery] string stageGroup, [FromQuery] string film)
        {
            _logger.LogInformation("GetPointData called with: LotId={lotId}, WaferId={waferId}, ServTs={servTs}, DateTime={dateTime}",
                lotId, waferId, servTs.ToString("o"), dateTime.ToString("o"));

            var dbInfo = new DatabaseInfo();
            var connectionString = dbInfo.GetConnectionString();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT * FROM public.plg_wf_flat
                WHERE lotid = @lotId AND waferid = @waferId AND serv_ts = @servTs AND datetime = @dateTime
                  AND cassettercp = @cassetteRcp AND stagercp = @stageRcp AND stagegroup = @stageGroup AND film = @film;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("lotId", lotId);
            cmd.Parameters.AddWithValue("waferId", waferId);
            cmd.Parameters.AddWithValue("servTs", servTs);
            cmd.Parameters.AddWithValue("dateTime", dateTime);
            cmd.Parameters.AddWithValue("cassetteRcp", cassetteRcp);
            cmd.Parameters.AddWithValue("stageRcp", stageRcp);
            cmd.Parameters.AddWithValue("stageGroup", stageGroup);
            cmd.Parameters.AddWithValue("film", film);

            // 1. 모든 데이터를 메모리로 읽어들입니다.
            var allRows = new List<Dictionary<string, object>>();
            var allColumnNames = new List<string>(); // DB의 원래 컬럼 순서 유지를 위해 사용
            await using var reader = await cmd.ExecuteReaderAsync();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                allColumnNames.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                foreach (var colName in allColumnNames)
                {
                    row[colName] = reader[colName];
                }
                allRows.Add(row);
            }

            _logger.LogInformation("Initially fetched {count} rows for Point Data for LotId={lotId}", allRows.Count, lotId);

            // 2. NULL이 아닌 값을 가진 컬럼만 동적으로 필터링합니다.
            var columnsToExclude = new HashSet<string>
            {
                "lotid", "waferid", "serv_ts", "datetime", "cassettercp",
                "stagercp", "stagegroup", "film", "eqpid"
            };

            var activeHeaders = new List<string>();
            foreach (var colName in allColumnNames)
            {
                if (columnsToExclude.Contains(colName.ToLower())) continue;

                // 전체 행을 검사하여, 해당 컬럼에 NULL이 아닌 값이 하나라도 있는지 확인합니다.
                if (allRows.Any(row => row[colName] != DBNull.Value))
                {
                    activeHeaders.Add(colName);
                }
            }

            // 3. 최종 응답 데이터를 구성합니다.
            var response = new PointDataResponseDto { Headers = activeHeaders };
            foreach (var row in allRows)
            {
                var dataRow = new List<object>();
                foreach (var header in activeHeaders)
                {
                    var value = row[header];
                    dataRow.Add(value is DBNull ? "" : value);
                }
                response.Data.Add(dataRow);
            }

            _logger.LogInformation("Returning {count} active columns for Point Data for LotId={lotId}", response.Headers.Count, lotId);

            return Ok(response);
        }

        // GetWaferFlatDataPaged 메서드는 변경할 필요 없습니다.
        [HttpGet("flatdata")]
        public async Task<ActionResult> GetWaferFlatDataPaged(
            [FromQuery] int page = 0, [FromQuery] int pageSize = 20, [FromQuery] string? eqpid = null,
            [FromQuery] string? lotId = null, [FromQuery] int? waferId = null, [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null, [FromQuery] string? cassetteRcp = null, [FromQuery] string? stageRcp = null,
            [FromQuery] string? stageGroup = null, [FromQuery] string? film = null,
            [FromQuery] string? sortLabel = null, [FromQuery] string? sortDirection = null
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

            if (!string.IsNullOrEmpty(eqpid)) { whereClauses.Add("eqpid = @eqpid"); parameters["eqpid"] = eqpid; }
            if (!string.IsNullOrEmpty(lotId)) { whereClauses.Add("lotid ILIKE @lotid"); parameters["lotid"] = $"%{lotId}%"; }
            if (waferId.HasValue) { whereClauses.Add("waferid = @waferid"); parameters["waferid"] = waferId.Value; }
            if (startDate.HasValue) { whereClauses.Add("serv_ts >= @startDate"); parameters["startDate"] = startDate.Value; }
            if (endDate.HasValue) { whereClauses.Add("serv_ts <= @endDate"); parameters["endDate"] = endDate.Value.AddDays(1).AddTicks(-1); }
            if (!string.IsNullOrEmpty(cassetteRcp)) { whereClauses.Add("cassettercp = @cassettercp"); parameters["cassettercp"] = cassetteRcp; }
            if (!string.IsNullOrEmpty(stageRcp)) { whereClauses.Add("stagercp = @stagercp"); parameters["stagercp"] = stageRcp; }
            if (!string.IsNullOrEmpty(stageGroup)) { whereClauses.Add("stagegroup = @stagegroup"); parameters["stagegroup"] = stageGroup; }
            if (!string.IsNullOrEmpty(film)) { whereClauses.Add("film = @film"); parameters["film"] = film; }

            string whereQuery = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
            SELECT COUNT(*) FROM (
                SELECT 1
                FROM public.plg_wf_flat
                {whereQuery}
                GROUP BY serv_ts, datetime, lotid, waferid, cassettercp, stagercp, stagegroup, film
            ) AS distinct_rows;";

            await using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                foreach (var p in parameters) { countCmd.Parameters.AddWithValue(p.Key, p.Value); }
                var result = await countCmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value) { totalItems = Convert.ToInt64(result); }
            }

            var orderByClause = "ORDER BY serv_ts ASC"; // 기본 정렬
            if (!string.IsNullOrEmpty(sortLabel))
            {
                // SQL Injection 방지를 위해 허용된 컬럼 목록 사용
                var allowedSortColumns = new Dictionary<string, string>
                {
                    { "ServTs", "serv_ts" },
                    { "LotId", "lotid" },
                    { "WaferId", "waferid" },
                    { "CassetteRcp", "cassettercp" },
                    { "StageRcp", "stagercp" },
                    { "StageGroup", "stagegroup" },
                    { "Film", "film" },
                    { "DateTime", "datetime" }
                };

                if (allowedSortColumns.TryGetValue(sortLabel, out var dbColumnName))
                {
                    var direction = "ASC";
                    if ("Descending".Equals(sortDirection, StringComparison.OrdinalIgnoreCase))
                    {
                        direction = "DESC";
                    }
                    orderByClause = $"ORDER BY {dbColumnName} {direction}";
                }
            }

            var dataSql = $@"
            SELECT
                eqpid, lotid, waferid, serv_ts, datetime, cassettercp, stagercp, stagegroup, film
            FROM public.plg_wf_flat
            {whereQuery}
            GROUP BY
                eqpid, serv_ts, datetime, lotid, waferid, cassettercp, stagercp, stagegroup, film
            {orderByClause}
            OFFSET @Offset LIMIT @PageSize;";

            await using var cmd = new NpgsqlCommand(dataSql, conn);
            foreach (var p in parameters) { cmd.Parameters.AddWithValue(p.Key, p.Value); }
            cmd.Parameters.AddWithValue("Offset", page * pageSize);
            cmd.Parameters.AddWithValue("PageSize", pageSize);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new WaferFlatDataDto
                {
                    EqpId = reader.IsDBNull(0) ? null : reader.GetString(0),
                    LotId = reader.IsDBNull(1) ? null : reader.GetString(1),
                    WaferId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    ServTs = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                    DateTime = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                    CassetteRcp = reader.IsDBNull(5) ? null : reader.GetString(5),
                    StageRcp = reader.IsDBNull(6) ? null : reader.GetString(6),
                    StageGroup = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Film = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }

            return Ok(new { items = results, totalItems });
        }

        [HttpGet("pdfimage")]
        [SupportedOSPlatform("windows")]
        public async Task<IActionResult> GetPdfImage(
            [FromQuery] string eqpid,
            [FromQuery] DateTime dateTime,
            [FromQuery] int pointNumber
            )
        {
            _logger.LogInformation("GetPdfImage called with: eqpid={eqpid}, dateTime={dateTime}, pointNumber={pointNumber}",
                eqpid, dateTime.ToString("o"), pointNumber);

            // 1. DB에서 PDF 파일의 URL 조회 (기존과 동일)
            var dbInfo = new DatabaseInfo();
            var connectionString = dbInfo.GetConnectionString();
            string? fileUrl = null;

            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var sql = "SELECT file_uri FROM public.plg_wf_map WHERE eqpid = @eqpid AND datetime = @dateTime LIMIT 1;";
                await using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("eqpid", eqpid);
                    cmd.Parameters.AddWithValue("dateTime", dateTime);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        fileUrl = result.ToString();
                    }
                }
            }

            if (string.IsNullOrEmpty(fileUrl))
            {
                _logger.LogWarning("PDF file_uri (URL) not found for eqpid={eqpid}", eqpid);
                return NotFound("PDF 정보를 찾을 수 없습니다.");
            }

            try
            {
                // 2. [핵심] URL로부터 PDF 파일을 HTTP로 다운로드
                _logger.LogInformation("Downloading PDF from URL: {fileUrl}", fileUrl);
                var client = _httpClientFactory.CreateClient();
                var pdfBytes = await client.GetByteArrayAsync(fileUrl);

                // 3. [핵심] 다운로드한 byte 데이터를 MemoryStream으로 감싸서 전달
                using (var stream = new MemoryStream(pdfBytes))
                using (var pdfDocument = PdfDocument.Load(stream)) // [수정] MemoryStream 사용
                {
                    var pageIndex = pointNumber;
                    if (pageIndex < 0 || pageIndex >= pdfDocument.PageCount)
                    {
                        return BadRequest("유효하지 않은 페이지 번호입니다.");
                    }

                    var dpi = 150;
                    using (var image = pdfDocument.Render(pageIndex, dpi, dpi, PdfRenderFlags.CorrectFromDpi))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            image.Save(memoryStream, ImageFormat.Png);
                            return File(memoryStream.ToArray(), "image/png");
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Failed to download PDF from {fileUrl}", fileUrl);
                return StatusCode(502, "PDF 저장소 서버에서 파일을 가져오는 데 실패했습니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render PDF page to image from URL: {fileUrl}", fileUrl);
                return StatusCode(500, "PDF를 이미지로 변환하는 중 오류가 발생했습니다.");
            }
        }
    }
}
