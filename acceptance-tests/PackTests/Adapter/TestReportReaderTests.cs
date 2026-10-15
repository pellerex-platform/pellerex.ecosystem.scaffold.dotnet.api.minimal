using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTests.Adapter;
using AcceptanceTests.PackTests.StandIns;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.PackTests.Adapter
{
    public class TestReportReaderTests
    {
        [Test]
        public async Task A_real_report_of_tunit_is_read_test_by_test()
        {
            var path = Path.Combine(
                System.AppContext.BaseDirectory,
                "Fixtures",
                "tunit-report.trx"
            );

            var (tests, errorCode) = new TestReportReader().Read(path);

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(tests.Count).IsEqualTo(5);
            var failed = tests.Single(test => test.TestName == "Fails_on_purpose");
            await Assert.That(failed.ClassName).IsEqualTo("Sample.Tests.AcceptanceOnly");
            await Assert.That(failed.Outcome).IsEqualTo("Failed");
            await Assert.That(failed.Types.Count).IsEqualTo(1);
            await Assert.That(failed.Types[0]).IsEqualTo("Acceptance");
            await Assert.That(failed.Output).IsEqualTo("before the failure\n");
            await Assert.That(failed.FailureMessage).Contains("Expected to be 3");
            await Assert.That(failed.StackTrace).Contains("Tests.cs:line");
            var both = tests.Single(test => test.TestName == "Both_on_method");
            await Assert.That(string.Join(",", both.Types.Order())).IsEqualTo("Acceptance,Release");
            await Assert.That(both.Output).IsEqualTo("first line\nsecond line\n");
            await Assert
                .That(tests.Single(test => test.TestName == "Skipped_on_purpose").Outcome)
                .IsEqualTo("NotExecuted");
            await Assert.That(tests.Any(test => test.TestName == "Data_driven(1)")).IsTrue();
            await Assert.That(tests.Any(test => test.TestName == "A plain sentence name")).IsTrue();
        }

        [Test]
        public async Task A_tests_time_is_read_in_whole_milliseconds()
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("tests.trx");
            File.WriteAllText(
                path,
                TrxReportWriter.Write([
                    new TrxEntry { TestName = "One", Duration = "00:00:01.2346000" },
                ])
            );

            var (tests, _) = new TestReportReader().Read(path);

            await Assert.That(tests[0].DurationMs).IsEqualTo(1235L);
            await Assert.That(tests[0].FailureMessage).IsEqualTo(string.Empty);
        }

        [Test]
        public async Task A_report_that_is_not_there_is_missing()
        {
            using var folder = new TemporaryFolder();

            var (tests, errorCode) = new TestReportReader().Read(folder.Inside("tests.trx"));

            await Assert.That(tests).IsNull();
            await Assert.That(errorCode).IsEqualTo(ErrorCodes.TestReportMissingCode);
        }

        [Test]
        [Arguments("not xml at all")]
        [Arguments("<TestRun><Results/></TestRun>")]
        [Arguments(
            "<?xml version=\"1.0\"?><!DOCTYPE TestRun [<!ENTITY e SYSTEM \"file:///etc/passwd\">]><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">&e;</TestRun>"
        )]
        [Arguments(
            "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results><UnitTestResult testId=\"x\" testName=\"One\" outcome=\"Passed\" duration=\"00:00:00.1\"/></Results></TestRun>"
        )]
        [Arguments(
            "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results><UnitTestResult testId=\"x\" testName=\"One\" outcome=\"Passed\" duration=\"soon\"/></Results><TestDefinitions><UnitTest id=\"x\"><TestMethod className=\"A\" name=\"One\"/></UnitTest></TestDefinitions></TestRun>"
        )]
        [Arguments(
            "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results/><TestDefinitions><UnitTest id=\"x\"><TestMethod className=\"A\" name=\"One\"/></UnitTest><UnitTest id=\"x\"><TestMethod className=\"A\" name=\"Two\"/></UnitTest></TestDefinitions></TestRun>"
        )]
        [Arguments(
            "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results/><TestDefinitions><UnitTest><TestMethod className=\"A\" name=\"One\"/></UnitTest></TestDefinitions></TestRun>"
        )]
        public async Task A_report_that_is_not_a_whole_trx_report_is_unreadable(string text)
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("tests.trx");
            File.WriteAllText(path, text);

            var (tests, errorCode) = new TestReportReader().Read(path);

            await Assert.That(tests).IsNull();
            await Assert.That(errorCode).IsEqualTo(ErrorCodes.TestReportUnreadableCode);
        }
    }
}
