using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AzDevOpsWiReader.Shared;
using Microsoft.Extensions.Configuration;

namespace AzDevOpsWiReader.Web.Data
{
    public class AzDevOpsReaderService
    {
        private readonly IConfiguration _configuration;

        public AzDevOpsReaderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DataTable GetAzDevOpsResult()
        {
            var c = _configuration.Get<Config>();
            return AzDevOpsReader.ReadWIs(c);
        }
    }
}