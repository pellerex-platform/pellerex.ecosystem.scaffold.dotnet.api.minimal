using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper.Models;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// Records every request a test sends and the response it gets back, in full: method, path and
    /// query, headers and body each way, the status and the time the answer took. Test frameworks
    /// do not capture HTTP traffic, so this record is how even a green run can be read request by
    /// request afterwards. Everything passes through <see cref="Redaction"/> before it is kept.
    /// </summary>
    public class RecordingHandler : DelegatingHandler
    {
        private readonly Redaction redaction;
        private readonly IExchangeLog exchangeLog;

        public RecordingHandler(Redaction redaction, IExchangeLog exchangeLog)
        {
            this.redaction = redaction;
            this.exchangeLog = exchangeLog;
        }

        /// <summary>
        /// Sends the request and records it with its response. The request is kept before it goes
        /// out, so one that gets no whole answer, because the app cannot be reached, the connection
        /// drops or the time runs out, is still in the test's record with no response, while its
        /// failure goes on to the test as it is. The response's body is read into memory first, so
        /// the test can still read it after it is recorded.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var exchange = new RecordedExchange
            {
                Request = await this.RecordRequestAsync(request, cancellationToken),
            };
            var sentAt = Stopwatch.GetTimestamp();
            this.exchangeLog.Add(exchange, sentAt);

            var response = await base.SendAsync(request, cancellationToken);
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            exchange.Response = await this.RecordResponseAsync(
                response,
                Stopwatch.GetElapsedTime(sentAt),
                cancellationToken
            );
            return response;
        }

        private async Task<RecordedRequest> RecordRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return new RecordedRequest
            {
                Method = request.Method.Method,
                Url = this.redaction.PathAndQuery(request.RequestUri?.PathAndQuery),
                Headers = this.RecordHeaders(request.Headers, request.Content?.Headers),
                Body = await this.RecordBodyAsync(request.Content, cancellationToken),
            };
        }

        private async Task<RecordedResponse> RecordResponseAsync(
            HttpResponseMessage response,
            TimeSpan latency,
            CancellationToken cancellationToken
        )
        {
            return new RecordedResponse
            {
                Status = (int)response.StatusCode,
                Headers = this.RecordHeaders(response.Headers, response.Content.Headers),
                Body = await this.RecordBodyAsync(response.Content, cancellationToken),
                LatencyMs = (long)Math.Round(latency.TotalMilliseconds),
            };
        }

        private Dictionary<string, string> RecordHeaders(
            HttpHeaders headers,
            HttpHeaders contentHeaders
        )
        {
            var recorded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in new[] { headers, contentHeaders })
            {
                if (group is null)
                {
                    continue;
                }

                // Read as sent, unparsed, so each header keeps its own separator, such as the spaces
                // between the parts of a user agent.
                foreach (var (name, values) in group.NonValidated)
                {
                    recorded[name] = this.redaction.HeaderValue(name, values.ToString());
                }
            }

            return recorded;
        }

        // The body is read into memory before it is read as text, so reading it here leaves it for
        // the request to send, or for the test to read.
        private async Task<string> RecordBodyAsync(
            HttpContent content,
            CancellationToken cancellationToken
        )
        {
            if (content is null)
            {
                return string.Empty;
            }

            await content.LoadIntoBufferAsync(cancellationToken);
            var mediaType = content.Headers.ContentType?.MediaType;
            var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
            if (!RecordedBodies.IsText(mediaType))
            {
                return RecordedBodies.DescribeNonText(bytes.LongLength, mediaType);
            }

            var body = RecordedBodies.ReadText(bytes, content.Headers.ContentType?.CharSet);
            return RecordedBodies.Cap(this.redaction.Body(body, mediaType));
        }
    }
}
