using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskAnywhere.OpenAI
{
    public class ChatCompletionData
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "gpt-3.5-turbo";

        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        [JsonProperty("temperature")]
        public float Temperature { get; set; } = 0.5f;

        [JsonProperty("stream")]
        public bool Stream { get; set; } = true;
    }

    public class ChatMessage
    {
        [JsonProperty("role")]
        public string? Role { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }
    }

    public class ChatCompletionChunk
    {
        [JsonProperty("choices")]
        public List<ChatChoice> Choices;
    }

    public class ChatChoice
    {
        [JsonProperty("delta")]
        public ChatMessage Delta { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }
}
