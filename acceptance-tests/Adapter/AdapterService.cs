using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AcceptanceTests.Adapter.Dtos;
using AcceptanceTests.Adapter.Models;
using AcceptanceTests.ClientHelper;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.Adapter
{
    /// <summary>
    /// The adapter's work: it gathers what the run left behind, makes the result file, writes it,
    /// and answers the exit code that agrees with it. The test identity's credential is read from
    /// the same folder the tests read, so every line of text in the result is redacted with the
    /// same values the tests' records were.
    /// </summary>
    public class AdapterService
    {
        private static readonly IReadOnlyDictionary<string, int> ExitCodeByResult = new Dictionary<
            string,
            int
        >
        {
            [ResultFile.GreenResult] = ExitCodes.Green,
            [ResultFile.RedResult] = ExitCodes.Red,
            [ResultFile.ErrorResult] = ExitCodes.Error,
        };

        private readonly ResultMaker resultMaker;
        private readonly TestReportReader testReportReader;
        private readonly LogReader logReader;
        private readonly ResultFileWriter resultFileWriter;
        private readonly TextWriter log;

        public AdapterService(
            ResultMaker resultMaker,
            TestReportReader testReportReader,
            LogReader logReader,
            ResultFileWriter resultFileWriter,
            TextWriter log
        )
        {
            this.resultMaker = resultMaker;
            this.testReportReader = testReportReader;
            this.logReader = logReader;
            this.resultFileWriter = resultFileWriter;
            this.log = log;
        }

        /// <summary>
        /// Makes and writes the result of one run, and answers its exit code: 0 green, 1 red, 2 an
        /// error, which is also the answer when no result file could be written.
        /// </summary>
        public int Run(AdapterRequest request, Func<string, string> variable)
        {
            var result = this.MakeResult(request, variable);
            var errorCode = this.resultFileWriter.Write(request.OutPath, result);
            if (errorCode.Length > 0)
            {
                this.log.WriteLine(
                    $"adapter: {ErrorCodes.Messages[errorCode]} ({request.OutPath})"
                );
                return ExitCodes.Error;
            }

            this.log.WriteLine(
                $"adapter: {request.TestType} run {request.RunId} is {result.Result}: {result.SuccessfulTests} passed, {result.FailedTests} failed; the result is in {request.OutPath}"
            );
            foreach (var diagnostic in result.Diagnostics ?? [])
            {
                this.log.WriteLine($"adapter: {diagnostic}");
            }

            return ExitCodeByResult[result.Result];
        }

        private TestRunResultDto MakeResult(AdapterRequest request, Func<string, string> variable)
        {
            var noSecrets = new Redaction([]);
            var credentialsFolder =
                variable(PackConstants.Inputs.CredentialsVariable) ?? string.Empty;
            IReadOnlyDictionary<string, string> credentials = new Dictionary<string, string>();
            if (credentialsFolder.Length > 0)
            {
                var (read, credentialsError) = new CredentialFolderReader().Read(credentialsFolder);
                if (credentialsError.Length > 0)
                {
                    return this.resultMaker.Error(
                        request,
                        [
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Diagnostics.ResultCannotBeMadeSafeFormat,
                                PackConstants.ErrorCodes.Messages[credentialsError]
                            ),
                        ],
                        noSecrets
                    );
                }

                credentials = read;
            }

            var redaction = new Redaction(credentials.Values);
            if (request.Command == Arguments.BuildFailed)
            {
                return this.MakeBuildFailedResult(request, redaction);
            }

            var (testOutput, _) = this.logReader.ReadLastLines(request.TestLogPath);
            testOutput ??= [];
            var (tests, reportError) = this.testReportReader.Read(request.TestReportPath);
            if (reportError.Length > 0)
            {
                var diagnostics = new List<string>
                {
                    ErrorCodes.Messages[reportError],
                    ResultMaker.DescribeExitCode(request.TestsExitCode),
                };
                if (testOutput.Count > 0)
                {
                    diagnostics.Add(Logs.TestOutputHeading);
                    diagnostics.AddRange(testOutput);
                }

                return this.resultMaker.Error(request, diagnostics, redaction);
            }

            var records = new RecordingsFolder(request.RecordingsFolder);
            var (recordings, recordingsError) = records.ReadTestRecordings();
            var (problems, problemsError) = records.ReadProblems();
            var unreadable = new[] { recordingsError, problemsError }
                .Where(code => code.Length > 0)
                .Distinct()
                .ToList();
            if (unreadable.Count > 0)
            {
                return this.resultMaker.Error(
                    request,
                    unreadable.Select(code => PackConstants.ErrorCodes.Messages[code]),
                    redaction
                );
            }

            return this.resultMaker.FromTestRun(
                request,
                tests,
                recordings,
                problems,
                testOutput,
                redaction
            );
        }

        private TestRunResultDto MakeBuildFailedResult(AdapterRequest request, Redaction redaction)
        {
            var (lines, errorCode) = this.logReader.ReadBuildErrors(request.BuildLogPath);
            var diagnostics = new List<string> { Diagnostics.PackDidNotBuild };
            diagnostics.AddRange(errorCode.Length > 0 ? [ErrorCodes.Messages[errorCode]] : lines);
            return this.resultMaker.Error(request, diagnostics, redaction);
        }
    }
}
