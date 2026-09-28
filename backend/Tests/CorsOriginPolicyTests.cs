using Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tests;

public class CorsOriginPolicyTests
{
    // Index separator is ':' not '[': .NET splits config child sections on
    // ConfigurationPath.KeyDelimiter only, so "Cors:AllowedOrigins[0]" would
    // never bind. JSON files and env vars both produce the colon form.
    private static IConfiguration Config(params string[] origins) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                origins.Select((o, i) => new KeyValuePair<string, string?>(
                    $"Cors:AllowedOrigins:{i}", o)))
            .Build();

    // Not a positional record: IHostEnvironment.EnvironmentName has a set
    // accessor, and an init-only record parameter cannot implement it (CS8854).
    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "cms";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public void Production_WithNoOrigins_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => CorsOriginPolicy.Resolve(Config(), new FakeEnvironment("Production")));

        Assert.Contains("Production", ex.Message);
    }

    [Fact]
    public void Production_WithWildcard_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => CorsOriginPolicy.Resolve(
                Config("https://giftmugweni.com", "*"),
                new FakeEnvironment("Production")));
    }

    [Fact]
    public void Production_WithAValidOrigin_ReturnsIt()
    {
        var origins = CorsOriginPolicy.Resolve(
            Config("https://giftmugweni.com", "https://stelele.github.io"),
            new FakeEnvironment("Production"));

        Assert.Equal(["https://giftmugweni.com", "https://stelele.github.io"], origins);
    }

    [Fact]
    public void Development_WithNoOrigins_IsAllowed()
    {
        var origins = CorsOriginPolicy.Resolve(Config(), new FakeEnvironment("Development"));

        Assert.Empty(origins);
    }

    [Fact]
    public void AnyEnvironment_WithABlankEntry_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => CorsOriginPolicy.Resolve(Config("   "), new FakeEnvironment("Development")));
    }
}
