using CommonLibrary.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;

namespace RepoUniquePascalIdentifier.Configuration
{
    public class LoggingConfigurations
    {
        // Every Application Insights connection string names its instrumentation key. The setting of
        // an environment that was given no log store does not: it is empty or still a placeholder.
        private const string InstrumentationKeyPart = "InstrumentationKey=";

        /// <summary>
        /// Builds the logger from the settings of the environment this API runs in. In Production,
        /// Staging and QualityAssurance its log messages and exceptions also go to that
        /// environment's log store, the one its requests go to.
        /// </summary>
        public static ILogger GetLogger()
        {
            var configuration = Startup.BuildConfiguration();

            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            var hosted =
                CommonLibrary.Utils.GeneralFunctions.RequestEnvironment == CommonLibrary.Constants.Enums.Environments.Production ||
                CommonLibrary.Utils.GeneralFunctions.RequestEnvironment == CommonLibrary.Constants.Enums.Environments.Staging ||
                CommonLibrary.Utils.GeneralFunctions.RequestEnvironment == CommonLibrary.Constants.Enums.Environments.QualityAssurance;

            var connectionString = configuration.Get<AppSettings>().Monitoring?.AzureApplicationConnectionString;
            var hasLogStore = connectionString.IsFull() && connectionString.Contains(InstrumentationKeyPart, StringComparison.OrdinalIgnoreCase);

            if (hosted && hasLogStore)
            {
                var logStore = new TelemetryConfiguration { ConnectionString = connectionString };

                // Stamps each log message with the request it was written in, so a request can be read with its messages
                logStore.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

                loggerConfiguration.WriteTo.ApplicationInsights(logStore, TelemetryConverter.Traces);
            }

            Log.Logger = loggerConfiguration.CreateLogger();

            if (hosted && !hasLogStore)
            {
                Log.Warning("This environment has no log store set (Monitoring:AzureApplicationConnectionString). Log messages stay on the console and in the log files.");
            }

            return Log.Logger;
        }
    }
}