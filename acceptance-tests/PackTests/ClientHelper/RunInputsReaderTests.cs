using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.PackTests.StandIns;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.PackTests.ClientHelper
{
    public class RunInputsReaderTests
    {
        private const string RunId = "run-9f3k2m7q1x";
        private const string Target = "http://127.0.0.1:9000";

        [Test]
        public async Task The_inputs_run_sh_hands_over_are_read_as_given()
        {
            using var records = new TemporaryFolder();
            var environment = new Dictionary<string, string>
            {
                [Inputs.TargetVariable] = Target,
                [Inputs.RunIdVariable] = RunId,
                [Inputs.RecordingsVariable] = records.Path,
            };

            var (inputs, errorCode) = new RunInputsReader().Read(name =>
                environment.GetValueOrDefault(name)
            );

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(inputs.TargetUrl.AbsoluteUri).IsEqualTo("http://127.0.0.1:9000/");
            await Assert.That(inputs.RunId).IsEqualTo(RunId);
            await Assert.That(inputs.RecordingsFolder).IsEqualTo(records.Path);
            await Assert.That(inputs.Credentials.Count).IsEqualTo(0);
        }

        [Test]
        public async Task A_target_with_a_path_keeps_it_so_tests_stay_under_it()
        {
            var (inputs, _) = new RunInputsReader().Read(
                Only(Inputs.TargetVariable, "https://api.example.test/product/qualityassurance")
            );

            await Assert
                .That(inputs.TargetUrl.AbsoluteUri)
                .IsEqualTo("https://api.example.test/product/qualityassurance/");
            await Assert
                .That(new System.Uri(inputs.TargetUrl, "v1/hello").AbsoluteUri)
                .IsEqualTo("https://api.example.test/product/qualityassurance/v1/hello");
        }

        [Test]
        public async Task A_run_started_without_a_run_id_makes_its_own()
        {
            var (inputs, _) = new RunInputsReader().Read(Only(Inputs.TargetVariable, Target));

            await Assert.That(RunNaming.IsRunId(inputs.RunId)).IsTrue();
            await Assert.That(inputs.RecordingsFolder).IsEqualTo(string.Empty);
        }

        [Test]
        public async Task The_credential_folder_is_read_when_the_invoker_names_one()
        {
            using var credentials = new TemporaryFolder();
            File.WriteAllText(credentials.Inside(Inputs.AccessTokenFileName), "token-value");
            var environment = new Dictionary<string, string>
            {
                [Inputs.TargetVariable] = Target,
                [Inputs.CredentialsVariable] = credentials.Path,
            };

            var (inputs, _) = new RunInputsReader().Read(name =>
                environment.GetValueOrDefault(name)
            );

            await Assert
                .That(inputs.Credentials[Inputs.AccessTokenFileName])
                .IsEqualTo("token-value");
        }

        [Test]
        [Arguments("", ErrorCodes.TargetMissingCode)]
        [Arguments("localhost:9000", ErrorCodes.TargetInvalidCode)]
        [Arguments("ftp://localhost", ErrorCodes.TargetInvalidCode)]
        [Arguments("/v1/hello", ErrorCodes.TargetInvalidCode)]
        [Arguments("http://127.0.0.1:9000/route?debug=1", ErrorCodes.TargetInvalidCode)]
        [Arguments("http://127.0.0.1:9000/route#top", ErrorCodes.TargetInvalidCode)]
        public async Task A_target_that_is_not_an_http_address_is_refused(
            string target,
            string expected
        )
        {
            var (inputs, errorCode) = new RunInputsReader().Read(
                Only(Inputs.TargetVariable, target)
            );

            await Assert.That(inputs).IsNull();
            await Assert.That(errorCode).IsEqualTo(expected);
        }

        [Test]
        public async Task A_run_id_that_breaks_the_rule_is_refused()
        {
            var environment = new Dictionary<string, string>
            {
                [Inputs.TargetVariable] = Target,
                [Inputs.RunIdVariable] = "run-9f3k2m",
            };

            var (_, errorCode) = new RunInputsReader().Read(name =>
                environment.GetValueOrDefault(name)
            );

            await Assert.That(errorCode).IsEqualTo(ErrorCodes.RunIdInvalidCode);
        }

        [Test]
        public async Task A_records_folder_or_credential_folder_that_does_not_exist_is_refused()
        {
            using var folder = new TemporaryFolder();
            var missing = Path.Combine(folder.Path, "missing");
            var withRecords = new Dictionary<string, string>
            {
                [Inputs.TargetVariable] = Target,
                [Inputs.RecordingsVariable] = missing,
            };
            var withCredentials = new Dictionary<string, string>
            {
                [Inputs.TargetVariable] = Target,
                [Inputs.CredentialsVariable] = missing,
            };

            var (_, recordsError) = new RunInputsReader().Read(name =>
                withRecords.GetValueOrDefault(name)
            );
            var (_, credentialsError) = new RunInputsReader().Read(name =>
                withCredentials.GetValueOrDefault(name)
            );

            await Assert.That(recordsError).IsEqualTo(ErrorCodes.RecordingsFolderMissingCode);
            await Assert.That(credentialsError).IsEqualTo(ErrorCodes.CredentialsFolderMissingCode);
        }

        private static System.Func<string, string> Only(string name, string value)
        {
            return asked => asked == name ? value : null;
        }
    }
}
