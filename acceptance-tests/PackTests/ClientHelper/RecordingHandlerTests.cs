using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.PackTests.StandIns;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.PackTests.ClientHelper
{
    public class RecordingHandlerTests
    {
        private const string Token = "token-of-the-test-identity";
        private const string Target = "http://app.test/route/";

        private readonly RecordingExchangeLog log = new();

        [Test]
        public async Task A_request_and_its_response_are_recorded_in_full()
        {
            var app = new StandInApp(_ => new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    "{\"id\":\"dsp-run-9f3k2m7q1x-01\"}",
                    Encoding.UTF8,
                    "application/json"
                ),
                Headers = { { "X-Correlation-Id", "abc" } },
            });
            using var client = this.MakeClient(app);
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/disputes?page=2")
            {
                Content = new StringContent(
                    "{\"name\":\"dsp-run-9f3k2m7q1x-01\"}",
                    Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Add("X-Trace", "t-1");

            using var reply = await client.SendAsync(request);

            var exchange = this.log.Exchanges[0];
            await Assert.That(this.log.Exchanges.Count).IsEqualTo(1);
            await Assert.That(exchange.Request.Method).IsEqualTo("POST");
            await Assert.That(exchange.Request.Url).IsEqualTo("/route/v1/disputes?page=2");
            await Assert.That(exchange.Request.Headers["X-Trace"]).IsEqualTo("t-1");
            await Assert
                .That(exchange.Request.Headers["Content-Type"])
                .IsEqualTo("application/json; charset=utf-8");
            await Assert
                .That(exchange.Request.Body)
                .IsEqualTo("{\"name\":\"dsp-run-9f3k2m7q1x-01\"}");
            await Assert.That(exchange.Response.Status).IsEqualTo(201);
            await Assert.That(exchange.Response.Headers["X-Correlation-Id"]).IsEqualTo("abc");
            await Assert
                .That(exchange.Response.Body)
                .IsEqualTo("{\"id\":\"dsp-run-9f3k2m7q1x-01\"}");
            await Assert.That(exchange.Response.LatencyMs).IsGreaterThanOrEqualTo(0L);
        }

        [Test]
        public async Task The_request_still_carries_its_body_and_the_test_can_still_read_the_response()
        {
            var app = new StandInApp(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("answer"),
            });
            using var client = this.MakeClient(app);

            using var reply = await client.PostAsync("v1/echo", new StringContent("question"));

            await Assert.That(app.ReceivedBodies[0]).IsEqualTo("question");
            await Assert.That(await reply.Content.ReadAsStringAsync()).IsEqualTo("answer");
        }

        [Test]
        public async Task Secrets_are_taken_out_of_both_ends_before_anything_is_kept()
        {
            var app = new StandInApp(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"echo\":\"{Token}\",\"accessToken\":\"other\"}}",
                    Encoding.UTF8,
                    "application/json"
                ),
                Headers = { { "Set-Cookie", "session=abc" } },
            });
            using var client = this.MakeClient(app);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"v1/keys?access_token={Token}"
            )
            {
                Content = new StringContent(
                    $"{{\"password\":\"hunter2\",\"note\":\"{Token}\"}}",
                    Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

            using var reply = await client.SendAsync(request);

            var exchange = this.log.Exchanges[0];
            await Assert.That(exchange.Request.Headers["Authorization"]).IsEqualTo(Secrets.Marker);
            await Assert
                .That(exchange.Request.Url)
                .IsEqualTo($"/route/v1/keys?access_token={Secrets.Marker}");
            await Assert
                .That(exchange.Request.Body)
                .IsEqualTo($"{{\"password\":\"{Secrets.Marker}\",\"note\":\"{Secrets.Marker}\"}}");
            await Assert.That(exchange.Response.Headers["Set-Cookie"]).IsEqualTo(Secrets.Marker);
            await Assert
                .That(exchange.Response.Body)
                .IsEqualTo(
                    $"{{\"echo\":\"{Secrets.Marker}\",\"accessToken\":\"{Secrets.Marker}\"}}"
                );
            await Assert.That(app.Received[0].Headers.Authorization.Parameter).IsEqualTo(Token);
        }

        [Test]
        public async Task A_body_that_is_not_text_is_described_by_its_size()
        {
            var app = new StandInApp(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[300])
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("image/png") },
                },
            });
            using var client = this.MakeClient(app);

            using var reply = await client.GetAsync("v1/picture");

            await Assert
                .That(this.log.Exchanges[0].Response.Body)
                .IsEqualTo("[a body of 300 bytes of image/png, not text, is not recorded]");
            await Assert.That((await reply.Content.ReadAsByteArrayAsync()).Length).IsEqualTo(300);
        }

        [Test]
        public async Task A_reply_naming_a_character_set_net_does_not_know_is_recorded_as_utf8_and_reaches_the_test()
        {
            var app = new StandInApp(_ =>
            {
                var content = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"score\":110}"));
                content.Headers.TryAddWithoutValidation(
                    "Content-Type",
                    "application/json; charset=utf8"
                );
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            });
            using var client = this.MakeClient(app);

            using var reply = await client.GetAsync("v1/hello");

            await Assert.That(reply.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(this.log.Exchanges[0].Response.Body).IsEqualTo("{\"score\":110}");
        }

        [Test]
        public async Task A_request_that_gets_no_answer_is_still_recorded_and_its_failure_reaches_the_test()
        {
            var app = new StandInApp(_ => throw new HttpRequestException("Connection refused"));
            using var client = this.MakeClient(app);

            await Assert
                .That(async () => await client.GetAsync("v1/hello"))
                .Throws<HttpRequestException>();

            await Assert.That(this.log.Exchanges.Count).IsEqualTo(1);
            await Assert.That(this.log.Exchanges[0].Request.Url).IsEqualTo("/route/v1/hello");
            await Assert.That(this.log.Exchanges[0].Response).IsNull();
        }

        [Test]
        public async Task Each_request_is_kept_with_the_time_it_was_sent()
        {
            var app = new StandInApp(_ => new HttpResponseMessage(HttpStatusCode.OK));
            using var client = this.MakeClient(app);

            using var first = await client.GetAsync("v1/first");
            using var second = await client.GetAsync("v1/second");

            await Assert.That(this.log.SentAt[0] <= this.log.SentAt[1]).IsTrue();
            await Assert.That(this.log.Exchanges[1].Request.Url).IsEqualTo("/route/v1/second");
        }

        private HttpClient MakeClient(StandInApp app)
        {
            return new HttpClient(
                new RecordingHandler(new Redaction([Token]), this.log) { InnerHandler = app }
            )
            {
                BaseAddress = new Uri(Target),
            };
        }
    }
}
