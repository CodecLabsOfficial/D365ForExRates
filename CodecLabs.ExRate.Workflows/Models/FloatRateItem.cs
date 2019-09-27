using Newtonsoft.Json;
using System;

namespace CodecLabs.ExRate.Workflows.Models
{
    public class FloatRateItem
    {
        [JsonProperty("code")]
        public string code { get; set; }
        [JsonProperty("alphaCode")]
        public string alphaCode { get; set; }
        [JsonProperty("date")]
        public DateTime date { get; set; }
        [JsonProperty("inverseRate")]
        public decimal inverseRate { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("numericCode")]
        public string numericCode { get; set; }
        [JsonProperty("rate")]
        public decimal rate { get; set; }
    }
}
