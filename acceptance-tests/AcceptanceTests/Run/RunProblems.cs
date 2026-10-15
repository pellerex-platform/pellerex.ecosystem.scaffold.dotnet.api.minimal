using System;
using System.IO;
using AcceptanceTests.ClientHelper;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.Run
{
    /// <summary>
    /// Reports a problem with the run itself, such as a test that carries no type: the adapter reads
    /// it from the records folder and makes the run an error, so a broken pack never passes for a
    /// red one, or for a green one. A run outside run.sh has no records folder and is told on its
    /// error output instead.
    /// </summary>
    public static class RunProblems
    {
        /// <summary>Reports one problem in a sentence a person can act on.</summary>
        public static void Report(string problem)
        {
            var folder =
                Environment.GetEnvironmentVariable(Inputs.RecordingsVariable) ?? string.Empty;
            if (folder.Length == 0 || !Directory.Exists(folder))
            {
                Console.Error.WriteLine(problem);
                return;
            }

            var errorCode = new RecordingsFolder(folder).WriteProblem(problem);
            if (errorCode.Length > 0)
            {
                Console.Error.WriteLine($"{problem} ({ErrorCodes.Messages[errorCode]})");
            }
        }
    }
}
