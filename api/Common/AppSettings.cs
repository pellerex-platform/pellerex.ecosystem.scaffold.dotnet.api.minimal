namespace RepoUniqueIdentifier
{
    public class AppSettings
    {
        public bool ContainerMode { get; set; }
        public string AllowedOrigins { get; set; }

        //Used for monitoring this API (DotNet API Scaffolding itself)
        public ApplicationInsightsSettings Monitoring { get; set; }

        public class ApplicationInsightsSettings
        {
            public string AzureApplicationInsightsInstrumentationKey { get; set; }
            public string AzureApplicationConnectionString { get; set; }
        }
    }
}