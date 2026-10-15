using System;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Pellerex.Testing.Acceptance.Run;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static Pellerex.Testing.Acceptance.ClientHelper.AcceptanceConstants;

namespace AcceptanceTests.Examples
{
    /// <summary>
    /// The health routes the platform asks to know the app has started, is alive and is ready.
    /// Acceptance only: the platform's probes reach a deployed app directly, while a request
    /// through the platform's gateway to a health route gets the gateway's own answer. So these
    /// routes can be tested only where the pack reaches the app directly, on a local copy.
    /// </summary>
    [Property(TestTypes.PropertyName, TestTypes.Acceptance)]
    public class HealthRouteTests
    {
        private static readonly string[] HealthRoutes =
        [
            "health/startup",
            "health/live",
            "health/ready",
        ];

        [Test]
        public async Task The_app_answers_healthy_on_its_three_health_routes()
        {
            using var api = TestRun.Client();
            foreach (var route in HealthRoutes)
            {
                Console.WriteLine($"Asking {route} whether the app is healthy");
                using var reply = await api.GetAsync(route);
                var answer = JsonNode.Parse(await reply.Content.ReadAsStringAsync());

                await Assert.That(reply.StatusCode).IsEqualTo(HttpStatusCode.OK);
                await Assert.That(answer?["status"]?.GetValue<string>()).IsEqualTo("Healthy");
            }
        }
    }
}
