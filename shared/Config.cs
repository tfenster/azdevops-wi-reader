using Newtonsoft.Json;

namespace AzDevOpsWiReader.Shared
{
    public enum Mode { WorkItems, Users, History }
    public class Config
    {
        public OrgsWithPAT[] OrgsWithPATs { get; set; }
        public string Query { get; set; }
        public Mode Mode { get; set; }
        public string LinkType { get; set; }
        public string InternalDomain { get; set; }
        public FieldWithLabel[] Fields { get; set; }

        [JsonIgnore]
        public string Content
        {
            get
            {
                if (this.OrgsWithPATs == null || this.OrgsWithPATs.Length == 0)
                    return Config.DEFAULT_WI;
                else
                    return JsonConvert.SerializeObject(this, Formatting.Indented);
            }

            set
            {
                Config _config = JsonConvert.DeserializeObject<Config>(value);
                this.OrgsWithPATs = _config.OrgsWithPATs;
                this.Query = _config.Query;
                this.LinkType = _config.LinkType;
                this.Fields = _config.Fields;
                this.Mode = _config.Mode;
                this.InternalDomain = _config.InternalDomain;
            }
        }

        public const string DEFAULT_WI = @"
{
  ""OrgsWithPATs"": [
    {
      ""Pat"": ""<put-your-pat-here>"",
      ""Orgs"": [
        ""<org1>"",
        ""<org2>""
      ]
    }
  ],
  ""Query"": ""SELECT [System.Id] FROM workitemLinks WHERE ([Source].[System.WorkItemType] IN ('User Story', 'Bug') AND [Source].[System.State] IN ('Active', 'Resolved') ) AND ([Target].[System.WorkItemType] = 'Task' AND NOT [Target].[System.State] IN ('Closed') ) ORDER BY [System.Id] MODE (MayContain)"",
  ""Mode"": 0,
  ""LinkType"": ""System.LinkTypes.Hierarchy-Forward"",
  ""Fields"": [
    {
      ""Id"": ""System.WorkItemType"",
      ""Label"": ""Type""
    },
    {
      ""Id"": ""System.AssignedTo"",
      ""Label"": ""AssignedTo""
    },
    {
      ""Id"": ""System.State"",
      ""Label"": ""State""
    },
    {
      ""Id"": ""System.Tags"",
      ""Label"": ""Tags""
    },
    {
      ""Id"": ""System.TeamProject"",
      ""Label"": ""Project""
    }
  ],
  ""InternalDomain"": null
} 
";

        public const string DEFAULT_USER = @"
{
  ""OrgsWithPATs"": [
    {
      ""Pat"": ""<put-your-pat-here>"",
      ""Orgs"": [
        ""<org1>"",
        ""<org2>""
      ]
    }
  ],
  ""Query"": null,
  ""Mode"": 1,
  ""InternalDomain"": ""cosmoconsult.com"",
  ""LinkType"": null,
  ""Fields"": null
} 
";


        public const string DEFAULT_HISTORY = @"
{
  ""OrgsWithPATs"": [
    {
      ""Pat"": ""<put-your-pat-here>"",
      ""Orgs"": [
        ""<org1>"",
        ""<org2>""
      ]
    }
  ],
  ""Query"": ""SELECT [System.Id] FROM workitemLinks WHERE ([Source].[System.WorkItemType] IN ('Feature', 'User Story')) AND ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') AND ([Target].[System.WorkItemType] = 'Task' AND [Target].[System.ChangedDate] > @startOfDay('-14d')) ORDER BY [System.ChangedDate] DESC MODE (Recursive, ReturnMatchingChildren)"",
  ""Mode"": 2,
  ""LinkType"": ""System.LinkTypes.Hierarchy-Forward"",
  ""Fields"": [],
  ""InternalDomain"": null
} 
";
    }

    public class OrgsWithPAT
    {
        public string Pat { get; set; }
        public string[] Orgs { get; set; }
    }

    public class FieldWithLabel
    {
        public string Id { get; set; }
        public string Label { get; set; }
    }
}