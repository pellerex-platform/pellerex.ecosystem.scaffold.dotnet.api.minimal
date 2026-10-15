using System.IO;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.ClientHelper.Models;
using AcceptanceTests.PackTests.StandIns;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.PackTests.ClientHelper
{
    public class RecordingsFolderTests
    {
        [Test]
        public async Task A_tests_record_is_read_back_as_it_was_written()
        {
            using var folder = new TemporaryFolder();
            var records = new RecordingsFolder(folder.Path);
            var recording = new TestRecording
            {
                ClassName = "AcceptanceTests.Examples.SampleRouteTests",
                TestName = "The_sample_route_answers_with_its_score",
                Requests =
                [
                    new RecordedExchange
                    {
                        Request = new RecordedRequest
                        {
                            Method = "GET",
                            Url = "/v1/hello",
                            Headers = new() { ["Accept"] = "*/*" },
                            Body = "",
                        },
                        Response = null,
                    },
                ],
            };

            var writeError = records.WriteTestRecording(recording);
            var (read, readError) = records.ReadTestRecordings();

            await Assert.That(writeError).IsEqualTo(string.Empty);
            await Assert.That(readError).IsEqualTo(string.Empty);
            await Assert.That(read.Count).IsEqualTo(1);
            await Assert.That(read[0].TestName).IsEqualTo(recording.TestName);
            await Assert.That(read[0].Requests[0].Request.Headers["Accept"]).IsEqualTo("*/*");
            await Assert.That(read[0].Requests[0].Response).IsNull();
        }

        [Test]
        public async Task Each_record_and_each_problem_is_a_file_of_its_own()
        {
            using var folder = new TemporaryFolder();
            var records = new RecordingsFolder(folder.Path);

            records.WriteTestRecording(
                new TestRecording
                {
                    ClassName = "A",
                    TestName = "One",
                    Requests = [],
                }
            );
            records.WriteTestRecording(
                new TestRecording
                {
                    ClassName = "A",
                    TestName = "Two",
                    Requests = [],
                }
            );
            records.WriteProblem("first problem");
            records.WriteProblem("second problem");

            var (recordings, _) = records.ReadTestRecordings();
            var (problems, _) = records.ReadProblems();
            await Assert.That(recordings.Count).IsEqualTo(2);
            await Assert.That(problems.Count).IsEqualTo(2);
            await Assert.That(Directory.GetFiles(folder.Path).Length).IsEqualTo(4);
        }

        [Test]
        public async Task A_record_that_is_not_the_packs_record_is_unreadable()
        {
            using var folder = new TemporaryFolder();
            File.WriteAllText(
                folder.Inside(Recordings.TestFilePrefix + "broken" + Recordings.TestFileExtension),
                "{ not json"
            );
            File.WriteAllText(
                folder.Inside(Recordings.TestFilePrefix + "empty" + Recordings.TestFileExtension),
                "{}"
            );

            var (recordings, errorCode) = new RecordingsFolder(folder.Path).ReadTestRecordings();

            await Assert.That(recordings).IsNull();
            await Assert.That(errorCode).IsEqualTo(ErrorCodes.RecordingUnreadableCode);
        }

        [Test]
        public async Task A_record_without_its_test_or_its_requests_is_unreadable()
        {
            using var folder = new TemporaryFolder();
            File.WriteAllText(
                folder.Inside(Recordings.TestFilePrefix + "partial" + Recordings.TestFileExtension),
                "{\"ClassName\":\"A\",\"TestName\":\"One\"}"
            );

            var (_, errorCode) = new RecordingsFolder(folder.Path).ReadTestRecordings();

            await Assert.That(errorCode).IsEqualTo(ErrorCodes.RecordingUnreadableCode);
        }

        [Test]
        public async Task A_folder_that_does_not_exist_can_be_neither_written_nor_read()
        {
            using var folder = new TemporaryFolder();
            var records = new RecordingsFolder(folder.Inside("missing"));

            var writeError = records.WriteTestRecording(
                new TestRecording
                {
                    ClassName = "A",
                    TestName = "One",
                    Requests = [],
                }
            );
            var problemError = records.WriteProblem("a problem");
            var (_, readError) = records.ReadProblems();
            var (recordings, recordingsError) = records.ReadTestRecordings();

            await Assert.That(writeError).IsEqualTo(ErrorCodes.RecordingUnwritableCode);
            await Assert.That(problemError).IsEqualTo(ErrorCodes.ProblemUnwritableCode);
            await Assert.That(readError).IsEqualTo(ErrorCodes.RecordsFolderUnreadableCode);
            await Assert.That(recordings).IsNull();
            await Assert.That(recordingsError).IsEqualTo(ErrorCodes.RecordsFolderUnreadableCode);
        }
    }
}
