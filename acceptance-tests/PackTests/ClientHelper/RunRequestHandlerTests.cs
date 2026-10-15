using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.PackTests.StandIns;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace AcceptanceTests.PackTests.ClientHelper
{
    public class RunRequestHandlerTests
    {
        private const string RunId = "run-9f3k2m7q1x";
        private const string Token = "token-of-the-test-identity";

        private static readonly Uri Target = new("http://app.test/product/qualityassurance/");

        [Test]
        public async Task A_request_carries_the_test_identitys_token_when_the_run_has_one()
        {
            var app = await Send(Token, new HttpRequestMessage(HttpMethod.Get, "v1/hello"));

            await Assert.That(app.Received[0].Headers.Authorization.Scheme).IsEqualTo("Bearer");
            await Assert.That(app.Received[0].Headers.Authorization.Parameter).IsEqualTo(Token);
        }

        [Test]
        public async Task The_token_goes_to_any_path_of_the_targets_host_and_port()
        {
            var app = await Send(
                Token,
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "http://APP.test:80/elsewhere/on/the/same/host"
                )
            );

            await Assert.That(app.Received[0].Headers.Authorization.Parameter).IsEqualTo(Token);
        }

        [Test]
        [Arguments("http://cdn.example.test/download/file")]
        [Arguments("https://app.test/v1/hello")]
        [Arguments("http://app.test:8080/v1/hello")]
        public async Task The_token_never_goes_to_another_host_scheme_or_port(string address)
        {
            var app = await Send(Token, new HttpRequestMessage(HttpMethod.Get, address));

            await Assert.That(app.Received[0].Headers.Authorization).IsNull();
            await Assert
                .That(app.Received[0].Headers.UserAgent.ToString())
                .IsEqualTo($"pellerex-acceptance-tests ({RunId})");
        }

        [Test]
        public async Task An_unauthenticated_run_sends_no_token()
        {
            var app = await Send(string.Empty, new HttpRequestMessage(HttpMethod.Get, "v1/hello"));

            await Assert.That(app.Received[0].Headers.Authorization).IsNull();
        }

        [Test]
        public async Task A_test_that_sets_its_own_credentials_keeps_them()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "v1/hello");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                "expired-on-purpose"
            );

            var app = await Send(Token, request);

            await Assert
                .That(app.Received[0].Headers.Authorization.Parameter)
                .IsEqualTo("expired-on-purpose");
        }

        [Test]
        public async Task A_request_names_the_run_in_its_user_agent()
        {
            var app = await Send(string.Empty, new HttpRequestMessage(HttpMethod.Get, "v1/hello"));

            await Assert
                .That(app.Received[0].Headers.UserAgent.ToString())
                .IsEqualTo($"pellerex-acceptance-tests ({RunId})");
        }

        [Test]
        public async Task A_request_asks_for_a_fresh_answer_that_is_not_kept()
        {
            var app = await Send(string.Empty, new HttpRequestMessage(HttpMethod.Get, "v1/hello"));

            await Assert.That(app.Received[0].Headers.CacheControl.NoCache).IsTrue();
            await Assert.That(app.Received[0].Headers.CacheControl.NoStore).IsTrue();
        }

        [Test]
        public async Task A_test_that_sets_its_own_cache_control_and_user_agent_keeps_them()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "v1/hello");
            request.Headers.CacheControl = new CacheControlHeaderValue
            {
                MaxAge = TimeSpan.FromSeconds(5),
            };
            request.Headers.UserAgent.ParseAdd("a-browser/1.0");

            var app = await Send(string.Empty, request);

            await Assert.That(app.Received[0].Headers.CacheControl.NoCache).IsFalse();
            await Assert
                .That(app.Received[0].Headers.UserAgent.ToString())
                .IsEqualTo("a-browser/1.0");
        }

        private static async Task<StandInApp> Send(string accessToken, HttpRequestMessage request)
        {
            var app = new StandInApp(_ => new HttpResponseMessage(HttpStatusCode.OK));
            using var client = new HttpClient(
                new RunRequestHandler(Target, accessToken, RunId) { InnerHandler = app }
            )
            {
                BaseAddress = Target,
            };
            using (request)
            {
                using var reply = await client.SendAsync(request);
            }

            return app;
        }
    }
}
