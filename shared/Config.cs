using Newtonsoft.Json;

namespace AzDevOpsWiReader.Shared
{
    public class Config
    {
        public OrgsWithPAT[] OrgsWithPATs { get; set; }
        public string Query { get; set; }
        public string LinkType { get; set; }
        public FieldWithLabel[] Fields { get; set; }

        [JsonIgnore]
        public string Content
        {
            get
            {
                if (this.OrgsWithPATs == null || this.OrgsWithPATs.Length == 0)
                    return Config.DEFAULT;
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
            }
        }

        public const string DEFAULT = @"
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
  ]
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