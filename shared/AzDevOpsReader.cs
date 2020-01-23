using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzDevOpsWiReader.Shared
{
    public class AzDevOpsReader
    {
        public static AzDevOpsResult ReadWIs(Config c)
        {
            var fieldList = new List<string>(c.Fields);
            if (!fieldList.Contains("System.Title"))
            {
                fieldList.Add("System.Title");
            }

            var tasks = new List<Task<ConcurrentDictionary<long, Dictionary<string, string>>>>();
            foreach (var orgWithPAT in c.OrgsWithPATs)
            {
                foreach (var org in orgWithPAT.Orgs)
                {
                    var or = new OrgReader(org, orgWithPAT.Pat, c.Query, fieldList, c.LinkType);
                    tasks.Add(or.ReadWIs());
                }
            }

            ConcurrentDictionary<long, Dictionary<string, string>>[] results = Task.WhenAll(tasks).Result;

            if (fieldList.Contains("System.AssignedTo"))
            {
                fieldList.Remove("System.AssignedTo");
                fieldList.Add("System.AssignedTo.DisplayName");
                fieldList.Add("System.AssignedTo.UniqueName");
            }
            fieldList.Add("ParentTitle");

            return new AzDevOpsResult()
            {
                FieldList = fieldList,
                WIs = results
            };
        }
    }

    public class AzDevOpsResult
    {
        public List<string> FieldList { get; set; }
        public ConcurrentDictionary<long, Dictionary<string, string>>[] WIs { get; set; }
    }
}