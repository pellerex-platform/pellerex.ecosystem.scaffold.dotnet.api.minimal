using System;
using System.Net.Http;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.ClientHelper.Models;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.Run
{
    /// <summary>
    /// The run's inputs and the client every test calls the app with. The client carries the test
    /// identity's credential when the run has one, records every request and response for the
    /// result file, and keeps every secret out of that record. Tests use paths without a leading
    /// slash, such as "v1/hello", so they stay under the target's own path.
    /// </summary>
    public static class TestRun
    {
        private static RunInputs inputs;
        private static HttpMessageHandler handlers;
        private static string inputsErrorCode = string.Empty;

        /// <summary>The run's id, which every record the run creates carries in its name.</summary>
        public static string RunId => Inputs().RunId;

        /// <summary>
        /// Reads the run's inputs once, before any test. Inputs that cannot be read make the run an
        /// error: every test then fails at its first request, and the adapter reports the reason
        /// instead of a red verdict.
        /// </summary>
        [Before(TestSession)]
        public static void ReadRunInputs()
        {
            var (read, errorCode) = new RunInputsReader().Read(Environment.GetEnvironmentVariable);
            if (errorCode.Length > 0)
            {
                inputsErrorCode = errorCode;
                RunProblems.Report(
                    $"The run's inputs cannot be read: {ErrorCodes.Messages[errorCode]}"
                );
                return;
            }

            inputs = read;
            var accessToken = read.Credentials.TryGetValue(
                PackConstants.Inputs.AccessTokenFileName,
                out var token
            )
                ? token
                : string.Empty;
            var recording = new RecordingHandler(
                new Redaction(read.Credentials.Values),
                TestExchangeLog.Instance
            )
            {
                // Redirects are not followed, so each one is recorded and a test sees it as the app sent it.
                InnerHandler = new SocketsHttpHandler { AllowAutoRedirect = false },
            };
            handlers = new RunRequestHandler(read.TargetUrl, accessToken, read.RunId)
            {
                InnerHandler = recording,
            };
        }

        /// <summary>Lets the handlers and their connections go once every test has finished.</summary>
        [After(TestSession)]
        public static void ReleaseHandlers()
        {
            handlers?.Dispose();
        }

        /// <summary>
        /// A client to call the app with, relative to the run's target. Every client shares the
        /// run's handlers and connections, and a test may dispose its own without touching any
        /// other test's.
        /// </summary>
        public static HttpClient Client()
        {
            var read = Inputs();
            return new HttpClient(handlers, disposeHandler: false)
            {
                BaseAddress = read.TargetUrl,
                Timeout = PackConstants.Client.RequestTimeout,
            };
        }

        /// <summary>The run's inputs, as read before the first test.</summary>
        public static RunInputs Inputs()
        {
            if (inputs is null)
            {
                var reason =
                    inputsErrorCode.Length > 0
                        ? ErrorCodes.Messages[inputsErrorCode]
                        : "they are read before the first test.";
                throw new InvalidOperationException(
                    $"The run's inputs are not available: {reason}"
                );
            }

            return inputs;
        }
    }
}
