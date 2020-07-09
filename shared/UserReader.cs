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
        private readonly HttpClient _vsaexHttpClient;
        private readonly HttpClient _groupHttpClient;

        public UserReader(string org, string pat)
        {
            _org = org;
            _pat = pat;

            _vsaexHttpClient = new HttpClient();
            _vsaexHttpClient.BaseAddress = new Uri("https://vsaex.dev.azure.com");
            _vsaexHttpClient.DefaultRequestHeaders.Clear();
            var authByteArray = Encoding.ASCII.GetBytes($"username:{_pat}");
            _vsaexHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authByteArray));

            _groupHttpClient = new HttpClient();
            _groupHttpClient.BaseAddress = new Uri("https://vssps.dev.azure.com");
            _groupHttpClient.DefaultRequestHeaders.Clear();
            _groupHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authByteArray));
        }

        public async Task<ConcurrentDictionary<string, Dictionary<string, string>>> ReadUsers()
        {
            Console.WriteLine($"Reading users for org {_org}");
            var allUsers = new ConcurrentDictionary<string, Dictionary<string, string>>();
            var userResult = await _vsaexHttpClient.GetAsync($"/{_org}/_apis/userentitlements?api-version=5.1-preview.2&top=10000");
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
            var lsResult = await _vsaexHttpClient.GetAsync($"/{_org}/_apis/userentitlementsummary?api-version=4.1-preview.1");
            lsResult.EnsureSuccessStatusCode();
            var lsResultContent = await lsResult.Content.ReadAsStringAsync();
            var ls = JsonConvert.DeserializeObject<LicenseSummary>(lsResultContent);
            ls.Organization = _org;
            return ls;
        }

        public async Task<KeyValuePair<string, string>> ReadEntity()
        {
            Console.WriteLine($"Reading entity information for org {_org}");
            var adminGroups = await GetGroupsByDisplayName("Project Collection Administrators");
            if (adminGroups != null)
            {
                var adminGroup = adminGroups[0];
                var groupMembershipResult = await _groupHttpClient.GetAsync($"{_org}/_apis/graph/Memberships/{adminGroup.Descriptor}?direction=down&api-version=5.1-preview.1");
                groupMembershipResult.EnsureSuccessStatusCode();
                var groupMembershipContent = await groupMembershipResult.Content.ReadAsStringAsync();
                var groupMembershipList = JsonConvert.DeserializeObject<GraphGroupMembershipList>(groupMembershipContent);

                foreach (var groupMembership in groupMembershipList.GraphGroupMemberships)
                {
                    if (groupMembership.Links.Member.Href.AbsoluteUri.Contains("_apis/Graph/Groups/aadgp"))
                    {
                        var groupResult = await _groupHttpClient.GetAsync(groupMembership.Links.Member.Href.PathAndQuery);
                        groupResult.EnsureSuccessStatusCode();
                        var groupContent = await groupResult.Content.ReadAsStringAsync();
                        var group = JsonConvert.DeserializeObject<GraphGroup>(groupContent);
                        if (group.DisplayName.StartsWith("ACL_") && group.DisplayName.EndsWith("_devops_ppi_admins"))
                        {
                            return new KeyValuePair<string, string>(_org, group.DisplayName.Substring(4, group.DisplayName.Length - 22));
                        }
                    }
                }
            }

            return new KeyValuePair<string, string>(_org, "Unassigned");
        }

        private async Task<GraphGroup[]> GetGroupsByDisplayName(string displayName)
        {

            Console.WriteLine($"Get groups in org {_org} with display name {displayName}");
            var groupsResult = await _groupHttpClient.GetAsync($"{_org}/_apis/graph/groups?subjectTypes=vssgp&api-version=5.1-preview.1");
            groupsResult.EnsureSuccessStatusCode();
            var groupsContent = await groupsResult.Content.ReadAsStringAsync();
            var groupList = JsonConvert.DeserializeObject<GraphGroupList>(groupsContent);

            return groupList.GraphGroups.Where(g => g.DisplayName.CompareTo(displayName) == 0).ToArray();
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