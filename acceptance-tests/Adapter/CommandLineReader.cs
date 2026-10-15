using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AcceptanceTests.Adapter.Models;
using AcceptanceTests.ClientHelper;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.Adapter
{
    /// <summary>
    /// Reads the adapter's command line. Every argument takes one value; the commit and the
    /// pipeline's run id may be empty, since a run outside git or outside a pipeline has neither.
    /// </summary>
    public class CommandLineReader
    {
        private static readonly string[] SharedArguments =
        [
            Arguments.Type,
            Arguments.Target,
            Arguments.RunId,
            Arguments.Out,
            Arguments.StartedAt,
            Arguments.Commit,
            Arguments.PipelineRunId,
        ];

        private static readonly string[] ArgumentsThatMayBeEmpty =
        [
            Arguments.Commit,
            Arguments.PipelineRunId,
        ];

        private static readonly Dictionary<string, string[]> ArgumentsByCommand = new()
        {
            [Arguments.Report] =
            [
                .. SharedArguments,
                Arguments.TestReport,
                Arguments.Recordings,
                Arguments.TestsExitCode,
                Arguments.TestLog,
            ],
            [Arguments.BuildFailed] = [.. SharedArguments, Arguments.BuildLog],
        };

        /// <summary>Answers what the command line asks, or nothing and the first thing wrong with it.</summary>
        public (AdapterRequest request, string errorCode) Read(IReadOnlyList<string> arguments)
        {
            if (
                arguments.Count == 0
                || !ArgumentsByCommand.TryGetValue(arguments[0], out var taken)
            )
            {
                return (null, ErrorCodes.CommandUnknownCode);
            }

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 1; index < arguments.Count; index += 2)
            {
                var name = arguments[index];
                if (!taken.Contains(name))
                {
                    return (null, ErrorCodes.ArgumentUnknownCode);
                }

                if (
                    index + 1 >= arguments.Count
                    || (arguments[index + 1].Length == 0 && !ArgumentsThatMayBeEmpty.Contains(name))
                )
                {
                    return (null, ErrorCodes.ArgumentValueMissingCode);
                }

                if (!values.TryAdd(name, arguments[index + 1]))
                {
                    return (null, ErrorCodes.ArgumentRepeatedCode);
                }
            }

            if (taken.Any(name => !values.ContainsKey(name)))
            {
                return (null, ErrorCodes.ArgumentMissingCode);
            }

            return Validate(arguments[0], values);
        }

        private static (AdapterRequest request, string errorCode) Validate(
            string command,
            Dictionary<string, string> values
        )
        {
            if (!PackConstants.TestTypes.All.Contains(values[Arguments.Type]))
            {
                return (null, ErrorCodes.TestTypeInvalidCode);
            }

            if (!RunNaming.IsRunId(values[Arguments.RunId]))
            {
                return (null, ErrorCodes.RunIdInvalidCode);
            }

            if (
                !DateTimeOffset.TryParseExact(
                    values[Arguments.StartedAt],
                    ResultFile.TimeFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var startedAt
                )
            )
            {
                return (null, ErrorCodes.StartedAtInvalidCode);
            }

            var testsExitCode = 0;
            if (
                command == Arguments.Report
                && (
                    !int.TryParse(
                        values[Arguments.TestsExitCode],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out testsExitCode
                    )
                    || testsExitCode > TestReport.HighestExitCode
                )
            )
            {
                return (null, ErrorCodes.TestsExitCodeInvalidCode);
            }

            return (
                new AdapterRequest
                {
                    Command = command,
                    TestType = values[Arguments.Type],
                    TargetUrl = values[Arguments.Target],
                    RunId = values[Arguments.RunId],
                    OutPath = values[Arguments.Out],
                    StartedAt = startedAt,
                    CommitId = values[Arguments.Commit],
                    PipelineRunId = values[Arguments.PipelineRunId],
                    TestReportPath = values.GetValueOrDefault(Arguments.TestReport, string.Empty),
                    RecordingsFolder = values.GetValueOrDefault(Arguments.Recordings, string.Empty),
                    TestsExitCode = testsExitCode,
                    TestLogPath = values.GetValueOrDefault(Arguments.TestLog, string.Empty),
                    BuildLogPath = values.GetValueOrDefault(Arguments.BuildLog, string.Empty),
                },
                string.Empty
            );
        }
    }
}
