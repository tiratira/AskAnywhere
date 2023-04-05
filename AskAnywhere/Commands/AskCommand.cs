using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskAnywhere.Commands
{
    public class AskCommands
    {
        [JsonProperty("commands")]
        public List<AskCommand>? Commands { get; set; }
    }
    public class AskCommand
    {
        [JsonProperty("command")]
        public string? Command { get; set; }

        [JsonProperty("depend")]
        public int Depend { get; set; }

        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("target")]
        public string? Target { get; set; }
    }
}
