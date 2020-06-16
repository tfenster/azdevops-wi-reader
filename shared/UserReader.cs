using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzDevOpsWiReader.Shared
{
    public class UserReader
    {
        private readonly string _org;
        private readonly string _pat;
        private readonly HttpClient _httpClient;

        public UserReader(string org, string pat)
        {
            _org = org;
            _pat = pat;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://vsaex.dev.azure.com");
            _httpClient.DefaultRequestHeaders.Clear();
            var authByteArray = Encoding.ASCII.GetBytes($"username:{_pat}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authByteArray));
        }

        public async Task<ConcurrentDictionary<string, Dictionary<string, string>>> ReadUsers()
        {
            Console.WriteLine($"Reading users for org {_org}");
            var allUsers = new ConcurrentDictionary<string, Dictionary<string, string>>();
            var userResult = await _httpClient.GetAsync($"/{_org}/_apis/userentitlements?api-version=5.1-preview.2&top=10000");
            userResult.EnsureSuccessStatusCode();
            var userResultContent = await userResult.Content.ReadAsStringAsync();
            var userResponse = JsonConvert.DeserializeObject<UsersResponse>(userResultContent);

            Console.WriteLine($"Found {userResponse.Members.Count()} users in org {_org}");

            foreach (var m in userResponse.Members)
            {
                allUsers.TryAdd(m.Id, ConvertUserToDict(m));
            }

            return allUsers;
        }

        public async Task<LicenseSummary> ReadLicenseSummary()
        {
            Console.WriteLine($"Reading license summary for org {_org}");
            var lsResult = await _httpClient.GetAsync($"/{_org}/_apis/userentitlementsummary?api-version=4.1-preview.1");
            lsResult.EnsureSuccessStatusCode();
            var lsResultContent = await lsResult.Content.ReadAsStringAsync();
            var ls = JsonConvert.DeserializeObject<LicenseSummary>(lsResultContent);
            ls.Organization = _org;
            return ls;
        }

        private Dictionary<string, string> ConvertUserToDict(Member m)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("ID", m.Id);
            dict.Add("Organization", _org);
            dict.Add("Display Name", m.User.DisplayName);
            dict.Add("Domain", m.User.Domain);
            dict.Add("Mail Address", m.User.MailAddress);
            dict.Add("Subject Kind", m.User.SubjectKind);
            dict.Add("License Type", m.AccessLevel.LicenseDisplayName);
            dict.Add("License Status", m.AccessLevel.Status);
            dict.Add("Last Accessed", m.LastAccessedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            return dict;
        }
    }
}