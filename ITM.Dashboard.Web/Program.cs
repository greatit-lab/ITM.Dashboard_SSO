// ITM.Dashboard.Web/Program.cs

using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Configuration;
using ITM.Dashboard.Web.Client.Pages;
using ITM.Dashboard.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;   // <-- 인증 상태 전파용
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using System.Net;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------
// 1️⃣ Kestrel – HTTPS 8082 (TLS 인증서 직접 바인딩)
// -------------------------------------------------
builder.WebHost.ConfigureKestrel(opt =>
{
    // 모든 인터페이스(0.0.0.0)에서 8082 포트 사용
    opt.Listen(IPAddress.Any, 8082, listen =>
    {
        var pfxPath = Path.Combine(
            builder.Environment.ContentRootPath,
            "certs/dev-tls-10.000.00.000.pfx");

        // 비밀번호는 appsettings 혹은 시크릿 매니저에서 읽어옵니다.
        var pwd = builder.Configuration["Tls:PfxPassword"] ?? "DevTls!2025";

        listen.UseHttps(pfxPath, pwd);
    });
});

// -------------------------------------------------
// 2️⃣ 서비스 등록
// -------------------------------------------------
builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();   // <-- WebAssembly 전용

builder.Services.AddMudServices();                     // MudBlazor UI
builder.Services.AddControllers();                     // /saml/* 등 MVC 컨트롤러

// -------------------------------------------------
// 3️⃣ 쿠키 인증 (SAML 로그인 후 세션 유지)
// -------------------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(opts =>
        {
            opts.Cookie.Name = "ITM.Dashboard.Auth";

            // SSO 리다이렉트 시 쿠키 차단 방지
            opts.Cookie.SameSite = SameSiteMode.None;
            opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            opts.LoginPath = "/saml/login";
            opts.LogoutPath = "/saml/logout";
            opts.SlidingExpiration = true;
            opts.ExpireTimeSpan = TimeSpan.FromHours(8);
        });

builder.Services.AddAuthorization();   // 필요 시 정책을 여기서 정의

// -------------------------------------------------------------------
// 4️⃣ AuthenticationStateProvider 등록 (CascadingAuthenticationState 용)
// -------------------------------------------------------------------
builder.Services.AddScoped<AuthenticationStateProvider,
                         ServerAuthenticationStateProvider>();

// -------------------------------------------------
// 5️⃣ SAML2Configuration (싱글톤)
// -------------------------------------------------
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var samlSec = builder.Configuration.GetSection("Saml2");

    var cfg = new Saml2Configuration
    {
        Issuer = samlSec.GetValue<string>("SP:EntityId"),
        SignatureAlgorithm = samlSec.GetValue<string>("SP:SignatureAlgorithm")
    };

    // ACS URL (문자열 그대로) – Uri 변환 오류 방지
    cfg.AllowedAudienceUris.Add(samlSec.GetValue<string>("SP:AcsUrl"));

    // IdP 엔드포인트
    cfg.SingleSignOnDestination = new Uri(samlSec.GetValue<string>("IdP:SingleSignOnUrl"));
    cfg.SingleLogoutDestination = new Uri(samlSec.GetValue<string>("IdP:SingleLogoutUrl"));

    // ----- SP 서명 인증서 (AuthnRequest 서명용) -----
    var spPfxPath = Path.Combine(env.ContentRootPath,
                                 samlSec.GetValue<string>("SP:SigningPfx"));
    var spPwd = samlSec.GetValue<string>("SP:SigningPfxPassword");
    cfg.SigningCertificate = new X509Certificate2(
        spPfxPath, spPwd, X509KeyStorageFlags.MachineKeySet);

    logger.LogInformation("SP signing cert thumbprint: {Thumb}",
                          cfg.SigningCertificate.Thumbprint);

    // ----- IdP 서명 인증서 (Response 검증용) -----
    var idpCertPath = Path.Combine(env.ContentRootPath,
                                   samlSec.GetValue<string>("IdP:SigningCert"));
    if (File.Exists(idpCertPath))
    {
        cfg.SignatureValidationCertificates.Add(
            new X509Certificate2(idpCertPath));
        logger.LogInformation("Loaded IdP signing cert from {Path}", idpCertPath);
    }
    else
    {
        logger.LogWarning("IdP signing cert NOT found at {Path}", idpCertPath);
    }

    return cfg;
});

// -------------------------------------------------
// 6️⃣ HttpClient (ITM.Dashboard.Api) – 개발용 예시
// -------------------------------------------------
builder.Services.AddHttpClient("ITM.Dashboard.Api", client =>
{
    // 개발 환경에 맞게 URL을 바꾸세요.
    client.BaseAddress = new Uri("https://localhost:7278");
});

var app = builder.Build();

// -------------------------------------------------
// 7️⃣ 파이프라인 설정
// -------------------------------------------------
if (app.Environment.IsDevelopment())
{
    // WASM 디버깅 활성화
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();            // <‑‑ 정적 파일 제공 (wwwroot, _content 등)
app.UseRouting();
app.UseAntiforgery();

app.UseAuthentication();   // 반드시 UseRouting 뒤, UseAuthorization 앞에 위치
app.UseAuthorization();

// -------------------------------------------------
// 8️⃣ 라우팅 매핑
// -------------------------------------------------
app.MapControllers();   // /saml/* 같은 MVC 컨트롤러 엔드포인트

// Blazor WebAssembly 인터랙티브 라우팅
app.MapRazorComponents<App>()
   .AddInteractiveWebAssemblyRenderMode()   // <-- HeadOutlet 이 요구하는 모드
   .AddAdditionalAssemblies(typeof(Home).Assembly);

// -----------------------------------------------------------------
// 9️⃣ 클라이언트‑측 라우트가 존재하지 않을 때 fallback → index.html
// -----------------------------------------------------------------
app.MapFallbackToFile("index.html");   // <-- WASM 클라이언트가 라우팅을 담당하도록

app.Run();
