using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AzDevOpsWiReader.Shared;
using Microsoft.Extensions.Configuration;

namespace AzDevOpsWiReader.Web.Data
{
    public interface IAzDevOpsReaderService
    {
        public Task<Dictionary<string, DataTable>> GetAzDevOpsResult(Config config);
    }

    public class AzDevOpsReaderService : IAzDevOpsReaderService
    {
        public async Task<Dictionary<string, DataTable>> GetAzDevOpsResult(Config config)
        {
            if (config.Mode == Mode.Users)
                return await AzDevOpsReader.ReadUsers(config);
            else if (config.Mode == Mode.History)
                return new Dictionary<string, DataTable>() {
                    { "History", await AzDevOpsReader.ReadHistory(config) }
                };
            else
                return new Dictionary<string, DataTable>() {
                    { "WorkItems", await AzDevOpsReader.ReadWIs(config) }
                };
        }
    }
}