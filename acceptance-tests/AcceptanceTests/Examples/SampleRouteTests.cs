using System;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AcceptanceTests.Run;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.Examples
{
    /// <summary>
    /// The app's sample route. Release and Acceptance: one light request that creates nothing, so
    /// it is safe against the live app, and true wherever the app runs.
    /// </summary>
    [Property(TestTypes.PropertyName, TestTypes.Release)]
    [Property(TestTypes.PropertyName, TestTypes.Acceptance)]
    public class SampleRouteTests
    {
        private const double SampleScore = 110;

        [Test]
        public async Task The_sample_route_answers_with_its_score()
        {
            Console.WriteLine("Asking the sample route for its score");
            using var api = TestRun.Client();
            using var reply = await api.GetAsync("v1/hello");
            var answer = JsonNode.Parse(await reply.Content.ReadAsStringAsync());

            await Assert.That(reply.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(answer?["score"]?.GetValue<double>()).IsEqualTo(SampleScore);
        }
    }
}
