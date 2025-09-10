// 파일 경로: ITM.Dashboard.Web/Program.cs

using ITM.Dashboard.Web.Client.Pages;
using ITM.Dashboard.Web.Components;
using MudBlazor.Services;
using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();

// 서버 측 렌더링을 위해 HttpClient를 등록합니다.
builder.Services.AddHttpClient("ITM.Dashboard.Api", client =>
{
    // API 서버의 주소를 입력합니다.
    client.BaseAddress = new Uri("https://localhost:7278"); 
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Home).Assembly);

app.Run();
