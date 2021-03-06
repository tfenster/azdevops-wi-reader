// Generated by https://quicktype.io

namespace AzDevOpsWiReader.Shared
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class LicenseSummary
    {
        public string Organization { get; set; }

        [JsonProperty("licenses")]
        public License[] Licenses { get; set; }

        [JsonProperty("extensions")]
        public object[] Extensions { get; set; }

        [JsonProperty("projectRefs")]
        public ProjectRef[] ProjectRefs { get; set; }

        [JsonProperty("groupOptions")]
        public GroupOption[] GroupOptions { get; set; }

        [JsonProperty("availableAccessLevels")]
        public AccessLevel[] AvailableAccessLevels { get; set; }

        [JsonProperty("defaultAccessLevel")]
        public AccessLevel DefaultAccessLevel { get; set; }
    }

    public partial class GroupOption
    {
        [JsonProperty("group")]
        public Group Group { get; set; }

        [JsonProperty("accessLevel")]
        public AccessLevel AccessLevel { get; set; }
    }

    public partial class Group
    {
        [JsonProperty("groupType")]
        public string GroupType { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }

    public partial class License
    {
        [JsonProperty("licenseName")]
        public string LicenseName { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("msdnLicenseType")]
        public string MsdnLicenseType { get; set; }

        [JsonProperty("accountLicenseType")]
        public string AccountLicenseType { get; set; }

        [JsonProperty("disabled")]
        public long Disabled { get; set; }

        [JsonProperty("totalAfterNextBillingDate")]
        public long TotalAfterNextBillingDate { get; set; }

        [JsonProperty("isPurchasable")]
        public bool IsPurchasable { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("available")]
        public long Available { get; set; }

        [JsonProperty("assigned")]
        public long Assigned { get; set; }

        [JsonProperty("includedQuantity")]
        public long IncludedQuantity { get; set; }
    }

    public partial class ProjectRef
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class LicenseSummary
    {
        public static LicenseSummary FromJson(string json) => JsonConvert.DeserializeObject<LicenseSummary>(json, LicenseSummaryConverter.Settings);
    }

    public static class LicenseSummarySerialize
    {
        public static string ToJson(this LicenseSummary self) => JsonConvert.SerializeObject(self, LicenseSummaryConverter.Settings);
    }

    internal static class LicenseSummaryConverter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
