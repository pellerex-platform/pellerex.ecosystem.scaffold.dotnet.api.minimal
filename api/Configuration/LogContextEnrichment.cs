using CommonLibrary.Abstract;
using CommonLibrary.Extensions;
using CommonLibrary.Types;
using CommonLibrary.Utils;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System;
using System.Threading.Tasks;

namespace RepoUniquePascalIdentifier.Configuration
{
    public class LogContextEnrichment
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";
        private const string CorrelationIdLogProperty = "CorrelationId";
        private const int MaximumCorrelationIdLength = 128;

        private readonly RequestDelegate next;

        public LogContextEnrichment(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, RequestContext requestContext, IDiagnosticContext diagnosticContext, INetworkingService networkingService)
        {
            diagnosticContext.Set("Environment", GeneralFunctions.RequestEnvironment.GetEnumStringValue());
            diagnosticContext.Set("ApplicationName", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            diagnosticContext.Set("ApplicationVersion", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            diagnosticContext.Set("ClientIPAddress", networkingService.GetClientIp(context));
            diagnosticContext.Set("ClientPort", networkingService.GetClientPort(context));
            diagnosticContext.Set(CommonLibrary.Constants.HeaderTypes.UserAgent, requestContext.UserAgent);

            // The request's correlation id: the one its caller sent in X-Correlation-Id (the platform's
            // proxy always sends one), or a new one for a call that arrives without. It goes back on the
            // reply and onto every log line of the request, under the name the portal's Logs tab reads.
            var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();

            if (!IsAcceptableCorrelationId(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            context.Response.Headers[CorrelationIdHeader] = correlationId;
            diagnosticContext.Set(CorrelationIdLogProperty, correlationId);

            using (LogContext.PushProperty(CorrelationIdLogProperty, correlationId))
            {
                await next(context);
            }
        }

        // The id comes from outside and is written to the logs and the reply, so only a short run of
        // letters, digits, hyphens, underscores and dots is taken as it is.
        private static bool IsAcceptableCorrelationId(string correlationId)
        {
            if (correlationId.IsEmpty() || correlationId.Length > MaximumCorrelationIdLength)
            {
                return false;
            }

            foreach (var character in correlationId)
            {
                if (!char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_' && character != '.')
                {
                    return false;
                }
            }

            return true;
        }
    }
}