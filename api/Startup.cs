using CommonLibrary;
using CommonLibrary.Abstract;
using CommonLibrary.Attributes;
using CommonLibrary.Extensions;
using CommonLibrary.Repository.Abstract;
using CommonLibrary.Services;
using CommonLibrary.Utils;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RepoUniqueIdentifier.Configuration;
using Serilog;
using System;
using System.IO;

namespace RepoUniqueIdentifier
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment CurrentEnvironment { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            AppSettings appSettings = SetupConfiguration(services);

            SetupMonitoring(services, appSettings);

            services.AddCors(options =>
            {
                options.AddPolicy("CORSPolicy",
                builder =>
                {
                    builder.WithOrigins(
                            appSettings.
                            AllowedOrigins.
                            Split(","))
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            SetupDI(appSettings, services);

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.AddHealthChecks()
                .AddCheck<ApiHealthCheck>("Check API Health");

            services.AddResponseCaching();

            ConfigureVersioning(services);

            ConfigureSwagger(services);

            ConfigureFluentValidation(services);
        }

        private static void ConfigureFluentValidation(IServiceCollection services)
        {
            services.AddMvcCore()
                .AddApiExplorer();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(ValidateModelStateAttribute));
            })
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());
        }

        private void SetupMonitoring(IServiceCollection services, AppSettings appSettings)
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = appSettings.Monitoring.AzureApplicationConnectionString;
            });
        }

        private void ConfigureVersioning(IServiceCollection services)
        {
            services.AddControllersWithViews(o =>
            {
                o.UseGeneralRoutePrefix("/v{version:apiVersion}");
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = 1;
                options.SerializerSettings.DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFKZ";
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                // camelCase keys + emit default-valued members (e.g. a false bool), matching the
                // Pellerex .NET convention (IdentityApi/JobRunnerApi) and the other-language
                // scaffolds' camelCase output.
                options.SerializerSettings.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include;
                options.SerializerSettings.ContractResolver =
                    new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            });

            services.AddApiVersioning(o => o.ReportApiVersions = true);

            services.AddVersionedApiExplorer(
                options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";
                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    options.SubstituteApiVersionInUrl = true;
                });
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
                { 
                    Title = "RepoUniqueIdentifier API", 
                    Version = "v1",
                    Description = "Pellerex API for RepoUniqueIdentifier"
                });

                // Include XML comments if available
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }

        private AppSettings SetupConfiguration(IServiceCollection services)
        {
            var environmentName = Environment.GetEnvironmentVariable(Constants.EnvironmentVariable);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var appSettings = configuration.Get<AppSettings>();
            services.AddSingleton(appSettings);
            return appSettings;
        }

        private void SetupDI(AppSettings settings, IServiceCollection services)
        {
            //Functions
            services.AddScoped<RequestContext>();
            services.AddScoped<INetworkingService, NetworkingServices>();

            services.AddHttpClient("LanguageProcessingApi", c =>
            {
                c.DefaultRequestHeaders.Add("User-Agent", "Pellerex.ManagedApiService.SampleApi");
                c.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                c.Timeout = TimeSpan.FromMinutes(1);
            });

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CurrentEnvironment = env;

            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RepoUniqueIdentifier API v1");
                    c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
                });
            }
            else
            {
            }

            //Below order matters
            app.UseRouting();

            app.UseCors("CORSPolicy");

            app.UseResponseCaching();

            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromMinutes(10)
                    };
                context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
                    new string[] { "Accept-Encoding" };

                await next();
            });

            app.UseSerilogRequestLogging();
            app.UseMiddleware<LogContextEnrichment>();

            app.ConfigureCustomExceptionMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/startup", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}