using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
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

        public List<ExpandoObject> GetAzDevOpsResult()
        {
            var c = _configuration.Get<Config>();
            return AzDevOpsReader.ReadWIs(c);
        }
    }
}