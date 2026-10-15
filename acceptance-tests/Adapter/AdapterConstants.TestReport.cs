using System.Collections.Generic;

namespace AcceptanceTests.Adapter
{
    public static partial class AdapterConstants
    {
        /// <summary>
        /// The test framework's own report: the TRX report the testing platform writes, and the exit
        /// codes the test run ends with. Only 0 and 2 are a finished run; every other code means the
        /// run itself broke before a verdict existed.
        /// </summary>
        public static class TestReport
        {
            /// <summary>The XML namespace of a TRX report.</summary>
            public const string Namespace =
                "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            /// <summary>The report's word for a test that passed; any other word but <see cref="NotExecutedOutcome"/> is a failure.</summary>
            public const string PassedOutcome = "Passed";

            /// <summary>The report's word for a test that was skipped and so never executed.</summary>
            public const string NotExecutedOutcome = "NotExecuted";

            /// <summary>The test run's exit code when every selected test passed.</summary>
            public const int AllPassedExitCode = 0;

            /// <summary>The test run's exit code when at least one test failed.</summary>
            public const int SomeFailedExitCode = 2;

            /// <summary>The test run's exit code when no test was selected.</summary>
            public const int NoTestsExitCode = 8;

            /// <summary>Exit codes above this one mean the test process was stopped by a signal, the code less this one.</summary>
            public const int SignalExitCodeBase = 128;

            /// <summary>The highest exit code a process can end with.</summary>
            public const int HighestExitCode = 255;

            /// <summary>What each other exit code of the test run means, for the error result's diagnostics.</summary>
            public static readonly IReadOnlyDictionary<int, string> ExitCodeMeanings =
                new Dictionary<int, string>
                {
                    [1] = "an unknown error stopped the test run",
                    [3] = "the test session was stopped before it finished",
                    [4] = "the test run's extensions are set up wrongly",
                    [5] = "the test run was given command-line arguments it does not take",
                    [7] = "the test session could not finish and likely crashed",
                    [8] = "no test was selected",
                    [9] = "fewer tests ran than the run required",
                    [10] =
                        "the test framework could not run the tests, for a reason outside the tests",
                    [13] = "the test run stopped after too many failures",
                };
        }
    }
}
