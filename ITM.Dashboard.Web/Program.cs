// ITM.Dashboard.Web/Program.cs

using ITM.Dashboard.Web.Client.Pages;
using ITM.Dashboard.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();

// ▼▼▼ "ITM.Dashboard.Api" 라는 이름으로 HttpClient를 등록합니다 ▼▼▼
builder.Services.AddHttpClient("ITM.Dashboard.Api", client =>
{
    client.BaseAddress = new Uri("https://localhost:7278"); // API 서버 주소
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Home).Assembly);

app.Run();
