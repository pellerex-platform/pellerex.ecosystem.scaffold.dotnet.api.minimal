using System.Net;
using System.Threading.Tasks;
using CommonLibrary.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace RepoUniquePascalIdentifier.UnitTests
{
    /// <summary>
    /// Swagger: the page that lists the app's routes and lets anyone call them, and the document
    /// it is built from. <see cref="Startup"/> serves both only when the host runs as Development,
    /// which is how the developer's own computer runs the app. A deployment runs it as
    /// Production, Staging or QualityAssurance, and there both must answer 404 while the app's own
    /// routes still answer, so the page stays closed unless someone opens it on purpose. Each test
    /// sets the host's environment, the one <see cref="Startup"/> asks IsDevelopment of and a
    /// deployment sets through ASPNETCORE_ENVIRONMENT. The environment variable the app reads
    /// its settings by stays Development, as <see cref="TestSessionSetUp"/> sets it once for
    /// every test.
    /// </summary>
    public class SwaggerRouteTests
    {
        private const string SampleRoute = "v1/hello";

        private static readonly string[] SwaggerRoutes =
        [
            "swagger/v1/swagger.json",
            "swagger/index.html",
        ];

        [Test]
        public async Task Swagger_IsServed_WhenTheAppRunsInDevelopment()
        {
            using var factory = new WebApplicationFactory<Program>();

            using var app = factory.WithWebHostBuilder(builder =>
                builder.UseEnvironment(Environments.Development)
            );

            using var client = app.CreateClient();

            foreach (var route in SwaggerRoutes)
            {
                using var reply = await client.GetAsync(route);

                await Assert
                    .That(reply.StatusCode)
                    .IsEqualTo(HttpStatusCode.OK)
                    .Because($"{route} is open on the developer's own computer");
            }
        }

        // The deployed environments come from the library's list, the one the app reads its own
        // environment against; the chart sets the same three (infrastructure/Helm).
        [Test]
        [Arguments(CommonLibrary.Constants.Enums.Environments.Production)]
        [Arguments(CommonLibrary.Constants.Enums.Environments.Staging)]
        [Arguments(CommonLibrary.Constants.Enums.Environments.QualityAssurance)]
        public async Task Swagger_IsNotServed_WhenTheAppRunsInADeployedEnvironment(
            CommonLibrary.Constants.Enums.Environments environment
        )
        {
            using var factory = new WebApplicationFactory<Program>();

            using var app = factory.WithWebHostBuilder(builder =>
                builder.UseEnvironment(environment.GetEnumStringValue())
            );

            using var client = app.CreateClient();

            foreach (var route in SwaggerRoutes)
            {
                using var reply = await client.GetAsync(route);

                await Assert
                    .That(reply.StatusCode)
                    .IsEqualTo(HttpStatusCode.NotFound)
                    .Because($"{route} stays closed in {environment}");
            }

            using var sampleReply = await client.GetAsync(SampleRoute);

            // The same app answers its own sample route, so the 404s are Swagger being closed,
            // not an app that answers nothing.
            await Assert
                .That(sampleReply.StatusCode)
                .IsEqualTo(HttpStatusCode.OK)
                .Because($"{SampleRoute} still answers in {environment}");
        }
    }
}
