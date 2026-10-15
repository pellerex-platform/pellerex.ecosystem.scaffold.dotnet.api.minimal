using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.ClientHelper.Models;
using TUnit.Core;

namespace AcceptanceTests.Run
{
    /// <summary>
    /// What happens after every test, passed or failed: tear-down removes every record the test
    /// registered, then the test's record of requests, tear-down's own included, is kept for the
    /// adapter. A record tear-down could not remove fails the test, so a run never leaves residue
    /// on a live app quietly.
    /// </summary>
    public static class AfterEachTest
    {
        /// <summary>Tears the test's records down, keeps its record of requests, and fails the test if anything is left behind.</summary>
        [AfterEvery(Test)]
        public static async Task TearDownAndKeepRecordAsync(TestContext context)
        {
            var leftBehind = new List<string>();
            foreach (var removal in TestData.TakeRemovalsFor(context))
            {
                var reason = await RemoveAsync(removal);
                if (reason.Length == 0)
                {
                    context.Output.WriteLine($"Tear-down: removed {removal.Name}");
                    continue;
                }

                context.Output.WriteLine($"Tear-down: could not remove {removal.Name}: {reason}");
                leftBehind.Add(removal.Name);
            }

            KeepRecord(context);

            if (leftBehind.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Tear-down left {string.Join(", ", leftBehind)} behind; support can find each by the run id {TestRun.RunId}."
                );
            }
        }

        // Answers why the record was not removed, or nothing when it was, with the failure's first
        // cause, such as a connection reset, so support can tell the network from the app. The
        // removal is the test's own code calling the app, and one that throws must not stop the
        // others: the one place this class catches.
        private static async Task<string> RemoveAsync(PendingRemoval removal)
        {
            try
            {
                using var client = TestRun.Client();
                using var response = await removal.Remove(client);
                var removed =
                    response.IsSuccessStatusCode
                    || response.StatusCode == HttpStatusCode.NotFound
                    || response.StatusCode == HttpStatusCode.Gone;
                return removed ? string.Empty : $"the app answered {(int)response.StatusCode}";
            }
            catch (Exception exception)
            {
                var cause = exception.GetBaseException();
                return cause == exception
                    ? exception.Message
                    : $"{exception.Message} {cause.Message}";
            }
        }

        private static void KeepRecord(TestContext context)
        {
            var folder = TestRun.Inputs().RecordingsFolder;
            if (folder.Length == 0)
            {
                return;
            }

            var errorCode = new RecordingsFolder(folder).WriteTestRecording(
                new TestRecording
                {
                    ClassName = context.Metadata.TestDetails.ClassType.FullName,
                    TestName = context.Metadata.DisplayName,
                    Requests = TestExchangeLog.Instance.TakeFor(context),
                }
            );
            if (errorCode.Length > 0)
            {
                RunProblems.Report(
                    $"{context.Metadata.DisplayName}: {PackConstants.ErrorCodes.Messages[errorCode]}"
                );
            }
        }
    }
}
