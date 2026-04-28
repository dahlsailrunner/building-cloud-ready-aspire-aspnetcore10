using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace CarvedRock.WebApp.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[AllowAnonymous]
public class ErrorModel(ILogger<ErrorModel> logger, IWebHostEnvironment environment) : PageModel
{
    public string? LogTraceId { get; set; }
    public Activity? CurrentActivity { get; set; }
    public string? TraceId { get; set; }
    public bool ShowDetails => !string.IsNullOrEmpty(LogTraceId);
    public bool IsDevelopment => environment.IsDevelopment();

    public void OnGet()
    {
        LogTraceId = Activity.Current?.RootId;
        CurrentActivity = Activity.Current;
        TraceId = HttpContext.TraceIdentifier;

        var userName = User.Identity?.IsAuthenticated ?? false ? User.Identity.Name : "";
        logger.LogWarning("User {userName} experienced an error.", userName);
    }
}