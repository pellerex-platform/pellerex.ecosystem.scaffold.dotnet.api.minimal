using System;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.Adapter
{
    /// <summary>The adapter's entry: it reads the command line, makes the result file and ends with the run's exit code.</summary>
    public static class Program
    {
        /// <summary>Runs one command of the adapter, with its log on standard error.</summary>
        public static int Main(string[] arguments)
        {
            var (request, errorCode) = new CommandLineReader().Read(arguments);
            if (errorCode.Length > 0)
            {
                Console.Error.WriteLine($"adapter: {ErrorCodes.Messages[errorCode]}");
                Console.Error.WriteLine(Arguments.Usage);
                return ExitCodes.Error;
            }

            var service = new AdapterService(
                new ResultMaker(TimeProvider.System),
                new TestReportReader(),
                new LogReader(),
                new ResultFileWriter(),
                Console.Error
            );
            return service.Run(request, Environment.GetEnvironmentVariable);
        }
    }
}
