using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AcceptanceTests.PackTests.StandIns
{
    /// <summary>
    /// Stands in for the app at the end of the client's handlers: it answers each request with what
    /// a test set, and keeps what it received, its body included, so a test can see what was sent.
    /// </summary>
    public class StandInApp : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> answer;

        public StandInApp(Func<HttpRequestMessage, HttpResponseMessage> answer)
        {
            this.answer = answer;
        }

        public List<HttpRequestMessage> Received { get; } = [];

        public List<string> ReceivedBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            this.Received.Add(request);
            this.ReceivedBodies.Add(
                request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken)
            );
            return this.answer(request);
        }
    }
}
