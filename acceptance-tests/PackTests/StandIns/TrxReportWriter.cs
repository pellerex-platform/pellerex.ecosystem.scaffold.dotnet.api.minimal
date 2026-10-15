using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AcceptanceTests.PackTests.StandIns
{
    /// <summary>Writes a TRX report in the shape the testing platform writes one, for the adapter's tests.</summary>
    public static class TrxReportWriter
    {
        private static readonly XNamespace Trx =
            "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        /// <summary>The report's text for the given tests, in the given order.</summary>
        public static string Write(IEnumerable<TrxEntry> entries)
        {
            var results = new XElement(Trx + "Results");
            var definitions = new XElement(Trx + "TestDefinitions");
            foreach (var entry in entries)
            {
                var testId = Guid.NewGuid().ToString();
                var executionId = Guid.NewGuid().ToString();
                var output = new XElement(Trx + "Output");
                if (entry.StdOut.Length > 0)
                {
                    output.Add(new XElement(Trx + "StdOut", entry.StdOut));
                }

                if (entry.Message.Length > 0 || entry.StackTrace.Length > 0)
                {
                    output.Add(
                        new XElement(
                            Trx + "ErrorInfo",
                            new XElement(Trx + "Message", entry.Message),
                            new XElement(Trx + "StackTrace", entry.StackTrace)
                        )
                    );
                }

                results.Add(
                    new XElement(
                        Trx + "UnitTestResult",
                        new XAttribute("executionId", executionId),
                        new XAttribute("testId", testId),
                        new XAttribute("testName", entry.TestName),
                        new XAttribute("duration", entry.Duration),
                        new XAttribute("outcome", entry.Outcome),
                        output.HasElements ? output : null
                    )
                );
                definitions.Add(
                    new XElement(
                        Trx + "UnitTest",
                        new XAttribute("name", entry.TestName),
                        new XAttribute("id", testId),
                        new XElement(Trx + "Execution", new XAttribute("id", executionId)),
                        new XElement(
                            Trx + "Properties",
                            entry.Types.Select(type => new XElement(
                                Trx + "Property",
                                new XElement(Trx + "Key", "Type"),
                                new XElement(Trx + "Value", type)
                            ))
                        ),
                        new XElement(
                            Trx + "TestMethod",
                            new XAttribute("className", entry.ClassName),
                            new XAttribute("name", entry.TestName)
                        )
                    )
                );
            }

            return new XDocument(new XElement(Trx + "TestRun", results, definitions)).ToString();
        }
    }
}
