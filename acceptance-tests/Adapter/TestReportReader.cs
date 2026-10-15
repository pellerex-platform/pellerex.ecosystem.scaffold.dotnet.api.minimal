using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using AcceptanceTests.Adapter.Models;
using AcceptanceTests.ClientHelper;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.Adapter
{
    /// <summary>
    /// Reads the test framework's own report: the TRX report the testing platform writes when the
    /// tests run with --report-trx. It lists every selected test with its outcome, time, output and
    /// failure, and, under each test's definition, the test's properties, which carry its types.
    /// </summary>
    public class TestReportReader
    {
        private static readonly XNamespace Trx = TestReport.Namespace;

        /// <summary>Answers every test of the report, in the report's order, or none and why.</summary>
        public (IReadOnlyList<ReportedTest> tests, string errorCode) Read(string path)
        {
            if (!File.Exists(path))
            {
                return (null, ErrorCodes.TestReportMissingCode);
            }

            var report = Load(path);
            if (report?.Root is null || report.Root.Name != Trx + "TestRun")
            {
                return (null, ErrorCodes.TestReportUnreadableCode);
            }

            var definitions = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
            foreach (
                var definition in report
                    .Root.Elements(Trx + "TestDefinitions")
                    .Elements(Trx + "UnitTest")
            )
            {
                var id = (string)definition.Attribute("id");
                if (id is null || !definitions.TryAdd(id, definition))
                {
                    return (null, ErrorCodes.TestReportUnreadableCode);
                }
            }

            var tests = new List<ReportedTest>();
            foreach (
                var result in report.Root.Elements(Trx + "Results").Elements(Trx + "UnitTestResult")
            )
            {
                var test = ReadResult(result, definitions);
                if (test is null)
                {
                    return (null, ErrorCodes.TestReportUnreadableCode);
                }

                tests.Add(test);
            }

            return (tests, string.Empty);
        }

        // A result is read only with its definition, which carries the class and the types; a result
        // without one, or without a name, an outcome or a readable time, makes the report unreadable.
        private static ReportedTest ReadResult(
            XElement result,
            Dictionary<string, XElement> definitions
        )
        {
            var testId = (string)result.Attribute("testId");
            var testName = (string)result.Attribute("testName");
            var outcome = (string)result.Attribute("outcome");
            if (
                testId is null
                || testName is null
                || outcome is null
                || !definitions.TryGetValue(testId, out var definition)
            )
            {
                return null;
            }

            var className = (string)definition.Element(Trx + "TestMethod")?.Attribute("className");
            if (
                className is null
                || !TimeSpan.TryParse(
                    (string)result.Attribute("duration"),
                    CultureInfo.InvariantCulture,
                    out var duration
                )
            )
            {
                return null;
            }

            var output = result.Element(Trx + "Output");
            return new ReportedTest
            {
                ClassName = className,
                TestName = testName,
                Outcome = outcome,
                DurationMs = (long)Math.Round(duration.TotalMilliseconds),
                Types = definition
                    .Elements(Trx + "Properties")
                    .Elements(Trx + "Property")
                    .Where(property =>
                        (string)property.Element(Trx + "Key")
                        == PackConstants.TestTypes.PropertyName
                    )
                    .Select(property => (string)property.Element(Trx + "Value") ?? string.Empty)
                    .ToList(),
                Output = (string)output?.Element(Trx + "StdOut") ?? string.Empty,
                FailureMessage =
                    (string)output?.Element(Trx + "ErrorInfo")?.Element(Trx + "Message")
                    ?? string.Empty,
                StackTrace =
                    (string)output?.Element(Trx + "ErrorInfo")?.Element(Trx + "StackTrace")
                    ?? string.Empty,
            };
        }

        // The report is written by the test framework, outside the pack's control, so it may be cut
        // short or not XML at all, which the reader reports by throwing: the one place this class
        // catches. A document type is refused, so the report cannot make the reader fetch anything.
        private static XDocument Load(string path)
        {
            try
            {
                using var reader = XmlReader.Create(
                    path,
                    new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                    }
                );
                return XDocument.Load(reader);
            }
            catch (Exception exception)
                when (exception is XmlException or IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
