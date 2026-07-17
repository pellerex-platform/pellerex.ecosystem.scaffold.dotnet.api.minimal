using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;

namespace RepoUniqueIdentifier.Configuration
{
    public class LoggingConfigurations
    {
        public static ILogger GetLogger()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            var telemetryConfiguration = new TelemetryConfiguration(configuration["Monitoring:AzureApplicationInsightsInstrumentationKey"]);

            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            if (CommonLibrary.Utils.GeneralFunctions.RequestEnvironment == CommonLibrary.Constants.Enums.Environments.Production ||
                CommonLibrary.Utils.GeneralFunctions.RequestEnvironment == CommonLibrary.Constants.Enums.Environments.Staging ||
                CommonLibrary.Utils.GeneralFunctions.RequestEnvironment == CommonLibrary.Constants.Enums.Environments.QualityAssurance)
            {
                loggerConfiguration.WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces);
            }

            Log.Logger = loggerConfiguration.CreateLogger();
            return Log.Logger;
        }
    }
}