using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AzDevOpsWiReader.Shared
{
    public class CurrencyReader
    {
        private static Currency _currency;
        public static async Task<Currency> ReadCurrency()
        {
            Console.WriteLine($"Reading currency info");
            if (_currency == null)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("https://api.exchangeratesapi.io");
                    httpClient.DefaultRequestHeaders.Clear();
                    var currencyResult = await httpClient.GetAsync("latest?symbols=USD");
                    var currencyResultContent = await currencyResult.Content.ReadAsStringAsync();
                    _currency = JsonConvert.DeserializeObject<Currency>(currencyResultContent);
                }
            }
            return _currency;
        }
    }
}