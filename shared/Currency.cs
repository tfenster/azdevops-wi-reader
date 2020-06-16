// Generated by https://quicktype.io

namespace AzDevOpsWiReader.Shared
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class Currency
    {
        [JsonProperty("rates")]
        public Rates Rates { get; set; }

        [JsonProperty("base")]
        public string Base { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
    }

    public partial class Rates
    {
        [JsonProperty("USD")]
        public double Usd { get; set; }
    }

    public partial class Currency
    {
        public static Currency FromJson(string json) => JsonConvert.DeserializeObject<Currency>(json, CurrencyConverter.Settings);
    }

    public static class CurrencySerialize
    {
        public static string ToJson(this Currency self) => JsonConvert.SerializeObject(self, CurrencyConverter.Settings);
    }

    internal static class CurrencyConverter
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
