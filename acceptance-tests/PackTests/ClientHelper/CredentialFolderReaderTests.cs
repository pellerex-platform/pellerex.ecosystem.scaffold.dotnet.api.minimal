using System;
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
    public class CredentialFolderReaderTests
    {
        [Test]
        public async Task Every_file_of_the_folder_is_a_credential_by_its_name_without_its_line_break()
        {
            using var folder = new TemporaryFolder();
            File.WriteAllText(folder.Inside(Inputs.AccessTokenFileName), "token-value\n");
            File.WriteAllText(folder.Inside("ClientSecret"), "  secret-value  ");

            var (credentials, errorCode) = new CredentialFolderReader().Read(folder.Path);

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(credentials.Count).IsEqualTo(2);
            await Assert.That(credentials[Inputs.AccessTokenFileName]).IsEqualTo("token-value");
            await Assert.That(credentials["ClientSecret"]).IsEqualTo("secret-value");
        }

        [Test]
        public async Task The_mounts_own_bookkeeping_is_not_a_credential()
        {
            using var folder = new TemporaryFolder();
            File.WriteAllText(folder.Inside(Inputs.AccessTokenFileName), "token-value");
            File.WriteAllText(folder.Inside("..2026_10_14"), "bookkeeping");
            Directory.CreateDirectory(folder.Inside("..data"));

            var (credentials, _) = new CredentialFolderReader().Read(folder.Path);

            await Assert.That(credentials.Count).IsEqualTo(1);
        }

        [Test]
        public async Task An_empty_folder_is_an_unauthenticated_run()
        {
            using var folder = new TemporaryFolder();

            var (credentials, errorCode) = new CredentialFolderReader().Read(folder.Path);

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(credentials.Count).IsEqualTo(0);
        }

        [Test]
        public async Task A_folder_that_does_not_exist_is_refused()
        {
            using var folder = new TemporaryFolder();

            var (credentials, errorCode) = new CredentialFolderReader().Read(
                folder.Inside("missing")
            );

            await Assert.That(credentials).IsNull();
            await Assert.That(errorCode).IsEqualTo(ErrorCodes.CredentialsFolderMissingCode);
        }

        [Test]
        public async Task A_file_that_cannot_be_read_is_refused()
        {
            // File permissions do not stop root, and Windows has none of this kind.
            if (OperatingSystem.IsWindows() || Environment.UserName == "root")
            {
                return;
            }

            using var folder = new TemporaryFolder();
            var path = folder.Inside(Inputs.AccessTokenFileName);
            File.WriteAllText(path, "token-value");
            File.SetUnixFileMode(path, UnixFileMode.None);

            var (credentials, errorCode) = new CredentialFolderReader().Read(folder.Path);

            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            await Assert.That(credentials).IsNull();
            await Assert.That(errorCode).IsEqualTo(ErrorCodes.CredentialUnreadableCode);
        }
    }
}
