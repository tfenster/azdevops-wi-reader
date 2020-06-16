using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AzDevOpsWiReader.Shared
{
    public class AzureReader
    {
        private static Pricing _pricing = null;

        public static async Task<Pricing> ReadPricing()
        {
            Console.WriteLine($"Reading pricing info");
            if (_pricing == null)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    // cors-anywhere (https://github.com/Rob--W/cors-anywhere/) as the azure API doesn't set CORS headers
                    var pricingResult = await httpClient.GetAsync("https://cors-anywhere.herokuapp.com/https://azure.microsoft.com/api/v2/pricing/azure-devops/calculator/?culture=de-de&discount=mosp");
                    var pricingResultContent = await pricingResult.Content.ReadAsStringAsync();
                    _pricing = JsonConvert.DeserializeObject<Pricing>(pricingResultContent);
                }
            }
            return _pricing;
        }
    }
}