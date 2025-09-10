// 파일 경로: ITM.Dashboard.Web.Client/Program.cs

using ITM.Dashboard.Web.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ITM.Dashboard.Web.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // MudBlazor의 다이얼로그, 스낵바 등 각종 서비스를 등록합니다.
            builder.Services.AddMudServices();

            // API 서버와 통신하기 위한 HttpClient를 등록합니다.
            // "ITM.Dashboard.Api" 라는 이름으로 등록하여 IHttpClientFactory로 생성할 수 있게 합니다.
            builder.Services.AddHttpClient("ITM.Dashboard.Api", client =>
            {
                // API 서버의 주소를 입력합니다.
                client.BaseAddress = new Uri("https://localhost:7278");
            });

            await builder.Build().RunAsync();
        }
    }
}
