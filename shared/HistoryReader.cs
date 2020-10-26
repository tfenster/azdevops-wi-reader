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
    public class HistoryReader
    {
        private readonly string _org;
        private readonly string _pat;
        private readonly string _query;
        private readonly string _linkType;
        private readonly List<FieldWithLabel> _fields;
        private readonly HttpClient _httpClient;

        public HistoryReader(string org, string pat, string query, List<FieldWithLabel> fields, string linkType = null)
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

        public async Task<ConcurrentDictionary<long, Dictionary<string, string>>> ReadWIs()
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
                    }
                    Dictionary<string, string> outDict;
                    allIDs.Remove(parentId.Value, out outDict);
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
    }
}