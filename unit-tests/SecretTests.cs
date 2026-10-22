using System.IO;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace RepoUniquePascalIdentifier.UnitTests
{
    /// <summary>
    /// The app's secrets. Deployed, each one arrives as a file of its own, named after the secret,
    /// in the folder the app reads in container mode (<see cref="Program"/>); here a folder of the
    /// test's own stands in for it. The sample route says whether the app has its database
    /// connection string and never shows it, since anyone can call that route.
    /// </summary>
    public class SecretTests
    {
        // The secret's name is written out on purpose: it is the file the vault's secret is
        // mounted as, so a rename on either side must fail a test.
        private const string SecretFileName = "DbConnectionString";
        private const string SecretValue = "Server=db;Database=orders;";
        private const string SecretsFolderPrefix = "secrets-";
        private const string SampleRoute = "v1/hello";

        private DirectoryInfo secretsFolder;

        [Before(Test)]
        public void MakeTheSecretsFolder()
        {
            this.secretsFolder = Directory.CreateTempSubdirectory(SecretsFolderPrefix);
        }

        [After(Test)]
        public void RemoveTheSecretsFolder()
        {
            this.secretsFolder.Delete(recursive: true);
        }

        [Test]
        public async Task A_secret_is_read_from_its_own_file_and_the_sample_route_does_not_show_it()
        {
            await File.WriteAllTextAsync(
                Path.Combine(this.secretsFolder.FullName, SecretFileName),
                SecretValue
            );
            using var factory = new WebApplicationFactory<Program>();
            using var app = factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration(
                    (_, configuration) => configuration.AddKeyPerFile(this.secretsFolder.FullName)
                )
            );
            using var client = app.CreateClient();
            using var reply = await client.GetAsync(SampleRoute);
            var body = await reply.Content.ReadAsStringAsync();

            await Assert.That(reply.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert
                .That(JsonNode.Parse(body)?["dbConnectionStringConfigured"]?.GetValue<bool>())
                .IsTrue();
            await Assert.That(body).DoesNotContain(SecretValue);
        }
    }
}
