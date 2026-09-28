using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Api;

/// <summary>
/// Resolves the CORS allowlist from configuration and refuses to let a bad
/// Production value through. There is deliberately no wildcard fallback: a
/// misconfigured deploy must fail at boot, not serve every origin.
/// </summary>
public static class CorsOriginPolicy
{
    public const string SectionName = "Cors";
    public const string OriginsKey = "AllowedOrigins";
    public const string PolicyName = "AllowFrontend";

    public static string[] Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var origins = configuration
            .GetSection($"{SectionName}:{OriginsKey}")
            .Get<string[]>() ?? [];

        if (origins.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException(
                $"{SectionName}:{OriginsKey} contains a blank entry.");

        // Rejected in every environment, not just Production. A wildcard makes
        // the public endpoints readable by any origin, and no legitimate
        // configuration needs one - Development supplies localhost:5173 from
        // its own file.
        if (origins.Any(o => o.Contains('*')))
            throw new InvalidOperationException(
                $"{SectionName}:{OriginsKey} contains a wildcard origin, which is not permitted.");

        if (!environment.IsProduction())
            return origins;

        if (origins.Length == 0)
            throw new InvalidOperationException(
                $"{SectionName}:{OriginsKey} is empty in Production. Set it with container "
                + "environment variables, for example Cors__AllowedOrigins__0=https://giftmugweni.com.");

        return origins;
    }
}
