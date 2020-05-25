using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AzDevOpsWiReader.Shared;
using Microsoft.Extensions.Configuration;

namespace AzDevOpsWiReader.Web.Data
{
    public interface IAzDevOpsReaderService
    {
        public Task<DataTable> GetAzDevOpsResult(Config config);
    }

    public class AzDevOpsReaderService : IAzDevOpsReaderService
    {
        public async Task<DataTable> GetAzDevOpsResult(Config config)
        {
            return await AzDevOpsReader.ReadWIs(config);
        }
    }
}