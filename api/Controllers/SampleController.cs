using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace RepoUniqueIdentifier.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    public class SampleController : ControllerBase
    {
        private readonly AppSecrets appSecrets;

        public SampleController(
            IConfiguration configuration)
        {
            this.appSecrets = configuration.Get<AppSecrets>();
        }

        [HttpGet("hello")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<ResponseViewModel>> Process()
        {
            // Report only WHETHER the Key Vault secret was read — never the value
            // itself (this endpoint is anonymous behind the public ingress). Parity
            // with the go/python/node/java scaffolds.
            return Ok(new ResponseViewModel
            {
                Score = 110,
                DbConnectionStringConfigured = !string.IsNullOrEmpty(appSecrets.DbConnectionString)
            });
        }
    }
}