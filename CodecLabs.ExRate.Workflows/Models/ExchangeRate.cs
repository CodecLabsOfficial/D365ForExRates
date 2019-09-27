using Newtonsoft.Json;
using System.Collections.Generic;

namespace CodecLabs.ExRate.Workflows.Models
{
    //Exchange Rate class to get the Json response
    public class ExchangeRate
    {
        [JsonProperty("rates")]
        public Dictionary<string, string> rates { get; set; }
    }
}
