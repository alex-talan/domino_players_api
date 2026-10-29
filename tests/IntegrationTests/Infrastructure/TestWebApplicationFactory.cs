using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string Strategy { get; set; } = "Greedy";

    public string PlayerId { get; set; } = "p0";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Player:Name"] = "IntegrationPlayer",
                ["Player:Id"] = PlayerId,
                ["Player:Strategy"] = Strategy
            });
        });
    }
}
