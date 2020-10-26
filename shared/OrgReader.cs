using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzDevOpsWiReader.Shared
{
    public class OrgReader
    {
        private readonly string _org;
        private readonly string _pat;
        private readonly string _query;
        private readonly string _linkType;
        private readonly List<FieldWithLabel> _fields;
        private readonly HttpClient _httpClient;

        public OrgReader(string org, string pat, string query, List<FieldWithLabel> fields, string linkType = null)
        {
            _org = org;
            _pat = pat;
            _query = query;
            _linkType = linkType;
            _fields = fields;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://dev.azure.com/");
            _httpClient.DefaultRequestHeaders.Clear();
            var authByteArray = Encoding.ASCII.GetBytes($"username:{_pat}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authByteArray));
        }

        public async Task<ConcurrentDictionary<Guid, Dictionary<string, string>>> ReadWIsWithTimeChange()
        {
            var dict = await ReadWIs(true);
            var retDict = new ConcurrentDictionary<Guid, Dictionary<string, string>>();
            var userMail = "";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                var authByteArray = Encoding.ASCII.GetBytes($"username:{_pat}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authByteArray));
                var response = await httpClient.GetAsync("https://app.vsaex.visualstudio.com/_apis/User/User");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<AuthedUserResponse>(responseBody);
                userMail = user.Mail;
            }

            foreach (var wiId in dict.Keys)
            {
                var currDict = dict[wiId];
                var time = await GetCompletedWorkChanges(currDict["System.TeamProject"], wiId, userMail);
                foreach (var timeKey in time.Keys)
                {
                    var newDict = new Dictionary<string, string>(currDict);
                    newDict.Add("TimeChangedAt", timeKey);
                    newDict.Add("TimeChange", time[timeKey].ToString());
                    retDict[Guid.NewGuid()] = newDict;
                }
            }
            return retDict;
        }

        public async Task<ConcurrentDictionary<long, Dictionary<string, string>>> ReadWIs(bool readTree = false)
        {
            Console.WriteLine($"Reading WIs for org {_org}");
            var allIDs = new ConcurrentDictionary<long, Dictionary<string, string>>();
            var childParentIDs = new Dictionary<long, long?>();
            var queryContent = new StringContent($"{{\"query\": \"{_query}\"}}", Encoding.UTF8, "application/json");
            var wiqlResult = await _httpClient.PostAsync($"/{_org}/_apis/wit/wiql?api-version=5.1", queryContent);
            wiqlResult.EnsureSuccessStatusCode();
            var wiqlResultContent = await wiqlResult.Content.ReadAsStringAsync();
            var wiqlResponse = JsonConvert.DeserializeObject<QueryResponse>(wiqlResultContent);
            if (wiqlResponse.QueryResultType == "workItem")
            {
                // flat list
                Console.WriteLine($"Found {wiqlResponse.WorkItems.Count()} WorkItems in org {_org}");
                foreach (var wi in wiqlResponse.WorkItems)
                {
                    allIDs[wi.Id] = null;
                    childParentIDs[wi.Id] = null;
                }
            }
            else if (wiqlResponse.QueryResultType == "workItemLink")
            {
                // tree list
                Console.WriteLine($"Found {wiqlResponse.WorkItemRelations.Count()} WorkItemRelations in org {_org}");
                var parentChilds = wiqlResponse.WorkItemRelations.Where(w => w.Rel == _linkType);
                foreach (var parentChild in parentChilds)
                {
                    allIDs[parentChild.Target.Id] = null;
                    childParentIDs[parentChild.Target.Id] = parentChild.Source.Id;
                    if (!allIDs.ContainsKey(parentChild.Source.Id))
                    {
                        allIDs[parentChild.Source.Id] = null;
                    }
                }

                var childsOnly = wiqlResponse.WorkItemRelations.Where(w => w.Rel == null);
                foreach (var child in childsOnly)
                {
                    if (!allIDs.ContainsKey(child.Target.Id))
                    {
                        allIDs[child.Target.Id] = null;
                    }
                    if (!childParentIDs.ContainsKey(child.Target.Id))
                    {
                        childParentIDs[child.Target.Id] = null;
                    }
                }
            }
            else
            {
                // unknown
            }

            var sliceSize = 200;
            var keyArray = allIDs.Keys.ToArray();
            var tasks = new List<Task<Dictionary<long, Dictionary<string, object>>>>();
            for (int start = 0; start < keyArray.Length; start += sliceSize)
            {
                int end = start + sliceSize;
                if (end > (keyArray.Length))
                {
                    end = keyArray.Length;
                }
                Range r = start..end;
                var requestedIDs = keyArray[r];
                tasks.Add(GetWIDetails(requestedIDs));
            }

            var allWIs = await Task.WhenAll(tasks);

            foreach (var wiDicts in allWIs)
            {
                foreach (var wiDict in wiDicts)
                {
                    var fieldDict = new Dictionary<string, string>();
                    foreach (var kvp in wiDict.Value)
                    {
                        if (kvp.Key == "System.AssignedTo")
                        {
                            var valueObject = kvp.Value as JObject;
                            fieldDict["System.AssignedTo.DisplayName"] = valueObject.GetValue("displayName").ToString();
                            fieldDict["System.AssignedTo.UniqueName"] = valueObject.GetValue("uniqueName").ToString();
                        }
                        else
                        {
                            fieldDict[kvp.Key] = kvp.Value.ToString();
                        }
                        fieldDict["ID"] = wiDict.Key.ToString();
                    }
                    allIDs[wiDict.Key] = fieldDict;
                }
            }

            foreach (var parentId in childParentIDs.Values)
            {
                if (parentId.HasValue && allIDs.ContainsKey(parentId.Value))
                {
                    foreach (var childId in childParentIDs.Where(e => e.Value == parentId))
                    {
                        allIDs[childId.Key]["ParentTitle"] = allIDs[parentId.Value]["System.Title"];
                        allIDs[childId.Key]["ParentURL"] = $"https://dev.azure.com/{_org}/_workitems/edit/{parentId.Value}";
                        if (readTree && childParentIDs[parentId.Value].HasValue)
                        {
                            allIDs[childId.Key]["ParentParentTitle"] = allIDs[childParentIDs[parentId.Value].Value]["System.Title"];
                            allIDs[childId.Key]["ParentParentURL"] = $"https://dev.azure.com/{_org}/_workitems/edit/{childParentIDs[parentId.Value].Value}";
                        }
                    }
                    if (!readTree)
                    {
                        Dictionary<string, string> outDict;
                        allIDs.Remove(parentId.Value, out outDict);
                    }
                }
            }
            return allIDs;
        }

        private async Task<Dictionary<long, Dictionary<string, object>>> GetWIDetails(long[] wiIds)
        {
            var definition = new Definition { Ids = wiIds, expand = "Fields" };
            var wibatchContent = new StringContent(JsonConvert.SerializeObject(definition), Encoding.UTF8, "application/json");
            var wibatchResult = await _httpClient.PostAsync($"/{_org}/_apis/wit/workitemsbatch?api-version=5.1", wibatchContent);
            wibatchResult.EnsureSuccessStatusCode();
            var wibatchResultContent = await wibatchResult.Content.ReadAsStringAsync();
            var wibatchResponse = JsonConvert.DeserializeObject<WorkitemsbatchResponse>(wibatchResultContent);
            var dict = new Dictionary<long, Dictionary<string, object>>();
            var fields = _fields.Select(f => f.Id);
            foreach (var wi in wibatchResponse.Workitems)
            {
                var fieldsResult = new Dictionary<string, object>();
                fieldsResult["URL"] = $"https://dev.azure.com/{_org}/_workitems/edit/{wi.Id}";
                fieldsResult["Organization"] = $"{_org}";
                foreach (var field in wi.Fields)
                {
                    if (fields.Contains(field.Key))
                    {
                        fieldsResult[field.Key] = field.Value;
                    }
                }
                dict.Add(wi.Id, fieldsResult);

            }
            return dict;
        }

        private async Task<Dictionary<string, float>> GetCompletedWorkChanges(string project, long wiId, string relevantRevisedByUniqueName)
        {
            try
            {
                var wiupdateResult = await _httpClient.GetAsync($"/{_org}/{project}/_apis/wit/workItems/{wiId}/updates?api-version=6.0");
                wiupdateResult.EnsureSuccessStatusCode();
                var wiupdateResultContent = await wiupdateResult.Content.ReadAsStringAsync();
                var wiupdateResponse = JsonConvert.DeserializeObject<WorkitemsUpdateResponse>(wiupdateResultContent);
                var relevantChanges = wiupdateResponse.WorkitemsUpdates.Where(u => u.Fields != null && u.Fields.ContainsKey("Microsoft.VSTS.Scheduling.CompletedWork")).Where(v => v.RevisedBy != null && v.RevisedBy.UniqueName == relevantRevisedByUniqueName);
                var completedWorkChanges = new Dictionary<string, float>();
                if (relevantChanges != null)
                {
                    foreach (var relevantChange in relevantChanges)
                    {
                        float origVal = 0f;
                        float newVal = 0f;
                        var completedWorkChange = relevantChange.Fields["Microsoft.VSTS.Scheduling.CompletedWork"];
                        if (completedWorkChange.OldValue != null)
                            float.TryParse(completedWorkChange.OldValue.ToString(), out origVal);
                        if (completedWorkChange.NewValue != null)
                            float.TryParse(completedWorkChange.NewValue.ToString(), out newVal);
                        completedWorkChanges.Add(DateTime.Parse(relevantChange.Fields["System.ChangedDate"].NewValue.ToString()).ToString("yyyy-MM-dd HH:mm:ss"), newVal - origVal);
                    }
                }
                return completedWorkChanges;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"happened here: {ex.Message}");
                return null;
            }
        }
    }

    public class Definition
    {
        public long[] Ids { get; set; }

        [JsonProperty("$expand")]
        public string expand { get; set; }
    }
}