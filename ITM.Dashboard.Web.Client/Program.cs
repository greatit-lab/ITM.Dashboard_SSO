// ITM.Dashboard.Web.Client/Program.cs

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
            builder.Services.AddMudServices();

            builder.Services.AddHttpClient("ITM.Dashboard.Api", client =>
            {
                client.BaseAddress = new Uri("https://127.0.0.1:7278");
            });

            await builder.Build().RunAsync();
        }
    }
}
