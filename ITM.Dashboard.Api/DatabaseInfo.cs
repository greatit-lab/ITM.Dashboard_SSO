// ITM.Dashboard.Api/DatabaseInfo.cs
using Npgsql;
using System;

namespace ITM.Dashboard.Api
{
    public sealed class DatabaseInfo
    {
        /* ── 하드코딩 예시는 그대로 두고 포트만 PostgreSQL 기본값(5432) ── */
        private const string _server = "00.000.00.00"; // 실제 DB 서버 IP로 변경해야 합니다.
        private const string _database = "itm";
        private const string _userId = "userid";
        private const string _password = "pw";
        private const int _port = 5432;

        public string ServerAddress => _server;
        public DatabaseInfo() { }
        public static DatabaseInfo CreateDefault() => new DatabaseInfo();

        public string GetConnectionString()
        {
            var csb = new NpgsqlConnectionStringBuilder
            {
                Host = _server,
                Database = _database,
                Username = _userId,
                Password = _password,
                Port = _port,
                Encoding = "UTF8",
                SslMode = SslMode.Disable,
                SearchPath = "public"
            };
            return csb.ConnectionString;
        }
    }
}
