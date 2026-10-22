using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace RepoUniquePascalIdentifier.UnitTests
{
    /// <summary>
    /// The health routes the platform calls to know the app has started, is alive and is ready.
    /// The app runs in memory from its own <see cref="Program"/> and <see cref="Startup"/>, so each
    /// route answers as it does when the app is deployed.
    /// </summary>
    public class HealthRouteTests
    {
        private static readonly string[] HealthRoutes =
        [
            "health/startup",
            "health/live",
            "health/ready",
        ];

        [Test]
        public async Task Each_health_route_answers_that_the_app_is_healthy()
        {
            using var app = new WebApplicationFactory<Program>();
            using var client = app.CreateClient();
            foreach (var route in HealthRoutes)
            {
                using var reply = await client.GetAsync(route);
                var answer = JsonNode.Parse(await reply.Content.ReadAsStringAsync());

                await Assert.That(reply.StatusCode).IsEqualTo(HttpStatusCode.OK);
                await Assert.That(answer?["status"]?.GetValue<string>()).IsEqualTo("Healthy");
            }
        }
    }
}
