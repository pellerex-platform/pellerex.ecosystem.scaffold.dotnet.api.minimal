using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AcceptanceTests.Run;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.Examples
{
    /// <summary>
    /// A caller's correlation id, which ties every log line of its request together. Release only:
    /// it guards the path a deployed caller's request takes, through the platform's gateway to the
    /// app and back, so a support engineer can find a call in the logs by the id its caller holds.
    /// On a local copy there is no gateway, and the sample route's test already covers the app.
    /// </summary>
    [Property(TestTypes.PropertyName, TestTypes.Release)]
    public class CorrelationIdTests
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";

        [Test]
        public async Task A_callers_correlation_id_comes_back_on_the_reply()
        {
            // The id is named like a record the run creates, so the app's logs show the run's id.
            var correlationId = TestData.Name("cid");
            Console.WriteLine($"Calling the sample route as {correlationId}");
            using var request = new HttpRequestMessage(HttpMethod.Get, "v1/hello");
            request.Headers.Add(CorrelationIdHeader, correlationId);
            using var api = TestRun.Client();
            using var reply = await api.SendAsync(request);
            var echoed = reply.Headers.TryGetValues(CorrelationIdHeader, out var values)
                ? values.ToList()
                : [];

            await Assert.That(reply.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(string.Join(", ", echoed)).IsEqualTo(correlationId);
        }
    }
}
