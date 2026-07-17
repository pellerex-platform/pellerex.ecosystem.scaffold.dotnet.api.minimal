using CommonLibrary.Abstract;
using CommonLibrary.Types;
using CommonLibrary.Utils;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Threading.Tasks;

namespace RepoUniqueIdentifier.Configuration
{
    public class LogContextEnrichment
    {
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
            diagnosticContext.Set(CommonLibrary.Constants.HeaderTypes.CorrelationId, requestContext.CorrelationId);
            await next(context);
        }
    }
}