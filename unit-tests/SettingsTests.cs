using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace RepoUniquePascalIdentifier.UnitTests
{
    /// <summary>
    /// The app's settings, which <see cref="Startup.BuildConfiguration"/> reads from the settings
    /// file the app ships with, then the environment's own file, then the environment variables a
    /// deployment sets.
    /// </summary>
    public class SettingsTests
    {
        [Test]
        public async Task The_app_takes_its_allowed_origins_from_its_settings_file()
        {
            var settings = Startup.BuildConfiguration().Get<AppSettings>();

            await Assert.That(settings.AllowedOrigins).IsEqualTo("*");
        }
    }
}
