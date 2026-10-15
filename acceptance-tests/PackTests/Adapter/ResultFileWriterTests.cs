using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AcceptanceTests.Adapter;
using AcceptanceTests.Adapter.Dtos;
using AcceptanceTests.ClientHelper.Models;
using AcceptanceTests.PackTests.StandIns;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.PackTests.Adapter
{
    public class ResultFileWriterTests
    {
        // Every field of the run header, in the contract's order, then the test results and the app's logs.
        private const string HeaderFields =
            "TestResultFormatVersion,Id,PipelineRunId,ProductId,CommitId,DeploymentMode,TestType,TargetUrl,StartedAt,FinishedAt,"
            + "Result,SuccessfulTests,FailedTests,TotalTestsRan";

        private const string EntryFields =
            "Name,TestType,Outcome,DurationMs,Logs,FailureReason,StackTrace,Requests";

        [Test]
        public async Task A_verdict_is_written_with_every_field_of_the_contract_in_its_order()
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("result.json");

            var errorCode = new ResultFileWriter().Write(path, Verdict());

            var written = JsonNode.Parse(File.ReadAllText(path)).AsObject();
            var entry = written["TestResults"][0].AsObject();
            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert
                .That(string.Join(",", written.Select(field => field.Key)))
                .IsEqualTo(HeaderFields + ",TestResults,AppLogs");
            await Assert
                .That(string.Join(",", entry.Select(field => field.Key)))
                .IsEqualTo(EntryFields);
            await Assert.That(entry["Requests"][0]["Response"]).IsNull();
            await Assert.That(entry["Requests"][0].AsObject().ContainsKey("Response")).IsTrue();
        }

        [Test]
        public async Task An_error_is_written_with_its_diagnostics_in_place_of_the_test_results()
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("result.json");
            var error = Verdict();
            error.Result = ResultFile.ErrorResult;
            error.TestResults = null;
            error.Diagnostics = ["The pack did not build, so no test ran."];

            new ResultFileWriter().Write(path, error);

            var written = JsonNode.Parse(File.ReadAllText(path)).AsObject();
            await Assert
                .That(string.Join(",", written.Select(field => field.Key)))
                .IsEqualTo(HeaderFields + ",AppLogs,Diagnostics");
        }

        [Test]
        public async Task The_file_is_indented_and_keeps_its_text_as_written()
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("result.json");
            var result = Verdict();
            result.TestResults[0].Logs = ["Calling <the> app & waiting"];

            new ResultFileWriter().Write(path, result);

            var text = File.ReadAllText(path);
            await Assert.That(text).StartsWith("{\n  \"TestResultFormatVersion\": 1,");
            await Assert.That(text).Contains("Calling <the> app & waiting");
        }

        [Test]
        public async Task A_path_that_cannot_be_written_is_refused()
        {
            using var folder = new TemporaryFolder();

            var errorCode = new ResultFileWriter().Write(
                folder.Inside(Path.Combine("missing", "result.json")),
                Verdict()
            );

            await Assert.That(errorCode).IsEqualTo(ErrorCodes.ResultUnwritableCode);
        }

        private static TestRunResultDto Verdict()
        {
            return new TestRunResultDto
            {
                TestResultFormatVersion = 1,
                Id = "run-9f3k2m7q1x",
                PipelineRunId = string.Empty,
                ProductId = ResultFile.ProductId,
                CommitId = "b7e2c1a",
                DeploymentMode = "Local",
                TestType = "Acceptance",
                TargetUrl = "http://127.0.0.1:9000",
                StartedAt = "2026-12-03T09:41:07Z",
                FinishedAt = "2026-12-03T09:41:31Z",
                Result = "Green",
                SuccessfulTests = 1,
                FailedTests = 0,
                TotalTestsRan = 1,
                TestResults =
                [
                    new TestResultEntryDto
                    {
                        Name = "Sample",
                        TestType = ["Acceptance"],
                        Outcome = "Passed",
                        DurationMs = 42,
                        Logs = [],
                        FailureReason = string.Empty,
                        StackTrace = string.Empty,
                        Requests =
                        [
                            new RecordedExchange
                            {
                                Request = new RecordedRequest
                                {
                                    Method = "GET",
                                    Url = "/v1/hello",
                                    Headers = [],
                                    Body = "",
                                },
                                Response = null,
                            },
                        ],
                    },
                ],
                AppLogs = [],
            };
        }
    }
}
