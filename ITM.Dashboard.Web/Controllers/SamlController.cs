// ITM.Dashboard.Web/Controllers/SamlController.cs

using System.Security.Claims;
using ITfoxtec.Identity.Saml2;                // ← 여기 안에 Saml2RedirectBinding, Saml2PostBinding 있음
using ITfoxtec.Identity.Saml2.MvcCore;       // ASP.NET Core 확장
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITM.Dashboard.Web.Controllers;

[AllowAnonymous]
[Route("saml")]
public class SamlController : Controller
{
    private readonly Saml2Configuration _config;

    public SamlController(Saml2Configuration config)
    {
        _config = config;
    }

    /// <summary>
    /// 로그인 시작: IdP로 Redirect
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = "/")
    {
        Console.WriteLine(">>> SamlController.Login 호출됨");
        var binding = new Saml2RedirectBinding();
        binding.SetRelayStateQuery(new Dictionary<string, string> { { "ReturnUrl", returnUrl } });

        var saml2AuthnRequest = new Saml2AuthnRequest(_config)
        {
            AssertionConsumerServiceUrl = new Uri($"{Request.Scheme}://{Request.Host}/saml/acs")
        };

        return binding.Bind(saml2AuthnRequest).ToActionResult();
    }

    /// <summary>
    /// ACS: IdP → SAML Response 처리
    /// </summary>
    [HttpPost("acs")]
    public async Task<IActionResult> Acs()
    {
        var binding = new Saml2PostBinding();
        var saml2AuthnResponse = new Saml2AuthnResponse(_config);

        binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);

        if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
        {
            return BadRequest($"SAML response indicates failure: {saml2AuthnResponse.Status}");
        }

        // ClaimsPrincipal 직접 생성
        var claimsIdentity = new ClaimsIdentity(
            saml2AuthnResponse.ClaimsIdentity.Claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

        string returnUrl = binding.GetRelayStateQuery().ContainsKey("ReturnUrl")
            ? binding.GetRelayStateQuery()["ReturnUrl"]
            : "/";

        return Redirect(returnUrl);
    }

    /// <summary>
    /// 로그아웃 (SP → IdP)
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        var binding = new Saml2RedirectBinding();
        var saml2LogoutRequest = new Saml2LogoutRequest(_config, User);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return binding.Bind(saml2LogoutRequest).ToActionResult();
    }

    /// <summary>
    /// SP 메타데이터 (IdP 등록용)
    /// </summary>
    [HttpGet("metadata")]
    public IActionResult Metadata()
    {
        var entityDescriptor = new EntityDescriptor(_config);

        entityDescriptor.SPSsoDescriptor = new SPSsoDescriptor
        {
            AuthnRequestsSigned = true,
            WantAssertionsSigned = true,
            SingleLogoutServices = new List<SingleLogoutService>
            {
                new SingleLogoutService
                {
                    Binding = ProtocolBindings.HttpRedirect,
                    Location = new Uri($"{Request.Scheme}://{Request.Host}/saml/logout")
                }
            },
            NameIDFormats = new List<Uri> { NameIdentifierFormats.Unspecified },
            AssertionConsumerServices = new List<AssertionConsumerService>
            {
                new AssertionConsumerService
                {
                    Binding = ProtocolBindings.HttpPost,
                    Location = new Uri($"{Request.Scheme}://{Request.Host}/saml/acs")
                }
            }
        };

        return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
    }
}
