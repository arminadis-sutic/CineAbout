using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("reset-password")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public partial class ResetPasswordPageController : ControllerBase
    {
        [GeneratedRegex(@"^\d{1,10}\.[0-9a-f]{64}$")]
        private static partial Regex TokenFormat();

        [HttpGet]
        public ContentResult Open([FromQuery] string? token)
        {
            var valid = token != null && TokenFormat().IsMatch(token);

            Response.Headers.CacheControl = "no-store";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.StatusCode = valid ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;

            var html = valid ? Page(token!) : InvalidPage;
            return Content(html, "text/html; charset=utf-8");
        }

        private static string Page(string token)
        {
            var deepLink = $"cineabout://app/reset?token={token}";
            return $$"""
                <!doctype html>
                <html lang="en"><head><meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>CineAbout - reset password</title>
                <style>
                  body{margin:0;background:#0b0b0b;color:#f2f2f2;font-family:system-ui,sans-serif;display:flex;min-height:100vh;align-items:center;justify-content:center}
                  main{max-width:440px;padding:32px}
                  h1{font-size:24px} a.button{display:block;background:#e50914;color:#fff;text-align:center;padding:14px;border-radius:10px;text-decoration:none;font-weight:700;margin:24px 0}
                  code{display:block;word-break:break-all;background:#222;padding:12px;border-radius:8px;font-size:13px;color:#ddd}
                  p{color:#bbb;line-height:1.5}
                </style></head><body><main>
                <h1>Reset your CineAbout password</h1>
                <p>Opening the CineAbout app on this device...</p>
                <a class="button" href="{{deepLink}}">Open CineAbout</a>
                <p>If the app does not open, install CineAbout and open this page on your phone again, or copy this
                token and paste it in the app under <b>Forgot password? &rarr; I already have a token</b>. It works once and expires in 15 minutes.</p>
                <code>{{token}}</code>
                <script>setTimeout(function(){ window.location.href = "{{deepLink}}"; }, 300);</script>
                </main></body></html>
                """;
        }

        private const string InvalidPage = """
            <!doctype html><html lang="en"><head><meta charset="utf-8"><title>CineAbout</title></head>
            <body style="background:#0b0b0b;color:#f2f2f2;font-family:system-ui,sans-serif;padding:32px">
            <h1>This reset link is not valid</h1><p>Request a new link from the app: Forgot password?</p></body></html>
            """;
    }
}
