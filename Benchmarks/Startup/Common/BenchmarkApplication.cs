using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brigade.Net.Benchmarks.Startup.Common;

public static class BenchmarkApplication
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Main"] = "Data Source=startup-main.db",
            ["ConnectionStrings:Reporting"] = "Data Source=startup-reporting.db",
            ["ConnectionStrings:Audit"] = "Data Source=startup-audit.db",
            ["ConnectionStrings:Archive"] = "Data Source=startup-archive.db",
            ["Mail:Host"] = "mail.example.invalid",
            ["Mail:Port"] = "2525",
            ["Storage:Bucket"] = "benchmark",
            ["Storage:RetentionDays"] = "30",
            ["Billing:Currency"] = "USD",
            ["Billing:TaxRate"] = "0.2",
            ["Features:AuditEnabled"] = "true",
            ["Features:PageSize"] = "20"
        });
        builder.Services.Configure<MailOptions>(builder.Configuration.GetSection("Mail"));
        builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
        builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));
        builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection("Features"));
        builder.Services.AddAuthentication("Benchmark")
            .AddScheme<AuthenticationSchemeOptions, BenchmarkAuthenticationHandler>("Benchmark", _ => { });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Read", policy => policy.RequireAuthenticatedUser())
            .AddPolicy("Write", policy => policy.RequireClaim("permission", "write"));
    }

    public static void Configure(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Ok()).AllowAnonymous();
        StartupInstrumentation.Attach(app);
    }
}
