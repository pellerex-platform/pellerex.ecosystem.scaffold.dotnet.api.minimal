using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// What every request a test sends carries unless the test sets it itself: the test identity's
    /// token, when the run has one, and only on a request to the run's own target, so a link the app
    /// hands back to another host never takes the credential with it; a user agent naming the run,
    /// so the app's logs show which requests were test traffic; and a request for a fresh answer
    /// that is not kept, so a test never reads a reply the app cached for another request, and a
    /// reply cached for a test is never served to anyone else.
    /// </summary>
    public class RunRequestHandler : DelegatingHandler
    {
        private readonly Uri target;
        private readonly string accessToken;
        private readonly string userAgent;

        public RunRequestHandler(Uri target, string accessToken, string runId)
        {
            this.target = target;
            this.accessToken = accessToken ?? string.Empty;
            this.userAgent = string.Format(
                CultureInfo.InvariantCulture,
                Client.UserAgentFormat,
                runId
            );
        }

        /// <summary>Adds what the request does not already carry, then sends it.</summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (
                this.accessToken.Length > 0
                && request.Headers.Authorization is null
                && this.GoesToTheTarget(request)
            )
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    Client.BearerScheme,
                    this.accessToken
                );
            }

            if (request.Headers.UserAgent.Count == 0)
            {
                request.Headers.UserAgent.ParseAdd(this.userAgent);
            }

            if (request.Headers.CacheControl is null)
            {
                request.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                };
            }

            return base.SendAsync(request, cancellationToken);
        }

        // The same scheme, host and port as the run's target: the one place the credential is for.
        private bool GoesToTheTarget(HttpRequestMessage request)
        {
            return request.RequestUri is not null
                && Uri.Compare(
                    request.RequestUri,
                    this.target,
                    UriComponents.SchemeAndServer,
                    UriFormat.Unescaped,
                    StringComparison.OrdinalIgnoreCase
                ) == 0;
        }
    }
}
