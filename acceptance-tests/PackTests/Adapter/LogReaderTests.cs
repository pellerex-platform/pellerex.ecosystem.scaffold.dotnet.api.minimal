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
    public class LogReaderTests
    {
        [Test]
        public async Task A_failed_builds_error_lines_are_kept_once_each()
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("build.log");
            File.WriteAllLines(
                path,
                [
                    "Determining projects to restore...",
                    "  /work/Examples/HelloTests.cs(12,5): error CS1002: ; expected [/work/AcceptanceTests.csproj]",
                    "  /work/Examples/HelloTests.cs(12,5): error CS1002: ; expected [/work/AcceptanceTests.csproj]",
                    "Build FAILED.",
                ]
            );

            var (lines, errorCode) = new LogReader().ReadBuildErrors(path);

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(lines.Count).IsEqualTo(1);
            await Assert
                .That(lines[0])
                .StartsWith("/work/Examples/HelloTests.cs(12,5): error CS1002");
        }

        [Test]
        public async Task A_build_log_with_no_error_line_gives_its_last_lines()
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("build.log");
            File.WriteAllLines(path, Enumerable.Range(1, 40).Select(number => $"line {number}"));

            var (lines, _) = new LogReader().ReadBuildErrors(path);

            await Assert.That(lines.Count).IsEqualTo(Logs.LastLines);
            await Assert.That(lines[^1]).IsEqualTo("line 40");
        }

        [Test]
        public async Task A_logs_last_lines_are_read_and_a_missing_log_is_unreadable()
        {
            using var folder = new TemporaryFolder();
            var path = folder.Inside("tests.log");
            File.WriteAllLines(path, ["one", "two"]);

            var (lines, _) = new LogReader().ReadLastLines(path);
            var (missing, errorCode) = new LogReader().ReadLastLines(folder.Inside("absent.log"));
            var (missingBuild, buildErrorCode) = new LogReader().ReadBuildErrors(
                folder.Inside("absent-build.log")
            );

            await Assert.That(string.Join(",", lines)).IsEqualTo("one,two");
            await Assert.That(missing).IsNull();
            await Assert.That(errorCode).IsEqualTo(ErrorCodes.LogUnreadableCode);
            await Assert.That(missingBuild).IsNull();
            await Assert.That(buildErrorCode).IsEqualTo(ErrorCodes.LogUnreadableCode);
        }
    }
}
