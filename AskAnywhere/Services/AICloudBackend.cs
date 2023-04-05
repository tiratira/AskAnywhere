using AskAnywhere.OpenAI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AskAnywhere.Services
{
    public class AICloudBackend : IAskBackend
    {
        private static string AICLOUD_API = "https://ai.intersight.co/aicenter";

        private Action<string>? _onError;
        private Action? _onFinished;
        private Action<string>? _onTextReceived;
        private string _aicloudKey;

        private bool _terminateFlag = false;

        public async void Ask(AskMode mode, string target, string prompt)
        {
            _terminateFlag = false;

            var completionData = CreateTextCompleteData(mode, target, prompt);

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{AICLOUD_API}/text/chat");
                var data = new AskRequest()
                {
                    Messages = completionData.Messages,
                    OpenId = _aicloudKey,
                    Mode = "ask"
                };
                var dataStr = JsonConvert.SerializeObject(data);
                try
                {
                    request.Content = new StringContent(dataStr, Encoding.UTF8, "application/json");
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseStr = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine(responseStr);
                        var resObject = JsonConvert.DeserializeObject<AiServerResponse<AskSessionData>>(responseStr);
                        if (resObject.Result)
                        {
                            await RetrieveDataAsync(resObject.Data.SessionId);
                        }
                    }
                    else
                    {
                        _onError?.Invoke($"ERROR: connection failed, code {response.StatusCode}");
                    }
                }
                catch (Exception e)
                {
                    _onError?.Invoke($"ERROR: connection failed, {e.Message}");
                    return;
                }

            }
        }

        private async Task RetrieveDataAsync(string sessionId)
        {
            while (!_terminateFlag)
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{AICLOUD_API}/text/get?openid={_aicloudKey}&session={sessionId}");

                    try
                    {
                        var response = await client.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseStr = await response.Content.ReadAsStringAsync();
                            Debug.WriteLine(responseStr);
                            var resObject = JsonConvert.DeserializeObject<AiServerResponse<AskChunkData>>(responseStr);
                            if (resObject.Result)
                            {
                                var text = resObject.Data.Text;
                                if (text.Length > 0)
                                {
                                    if (text.EndsWith("[DONE]"))
                                    {
                                        text = text.Substring(0, text.Length - 6);

                                    }
                                    _onTextReceived?.Invoke(text);
                                }

                                if (resObject.Data.Finish)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _onError?.Invoke($"ERROR: connection interrupt, {e.Message}");
                        return;
                    }
                }
            }
            _onFinished?.Invoke();
        }

        public void SetErrorCallback(Action<string> onError)
        {
            _onError = onError;
        }

        public void SetFinishedCallback(Action onFinished)
        {
            _onFinished = onFinished;
        }

        public void SetTextReceivedCallback(Action<string> onTextReceived)
        {
            _onTextReceived = onTextReceived;
        }

        private ChatCompletionData CreateTextCompleteData(AskMode mode, string target, string prompt)
        {
            var systemMsg = "";
            if (mode == AskMode.ASK) systemMsg = prompt;
            if (mode == AskMode.CODE) systemMsg =
                    $"you are code generator, using {target} to output codes or functions." +
                    " please generate code ONLY, no other explanations, no markdown blocks, just code with comments."
                    + $"code request: {prompt}\"";
            if (mode == AskMode.TRANSLATION) systemMsg =
                    $" you are a translator, translate the sentence into {target}: {prompt}";


            var data = new ChatCompletionData()
            {
                Model = "gpt-3.5-turbo",
                Messages = new List<ChatMessage>() {
            //new ChatMessage() {Role = "system", Content = systemMsg},
            new ChatMessage() {Role = "user", Content = systemMsg}
            },
                Stream = true
            };
            return data;
        }

        public void Terminate()
        {
            _terminateFlag = true;
        }

        public void SetAuthorizationKey(string key)
        {
            _aicloudKey = key;
        }

        public class AskRequest
        {
            [JsonProperty("openid")]
            public string OpenId { get; set; }

            [JsonProperty("mode")]
            public string Mode { get; set; }

            [JsonProperty("messages")]
            public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        }

        public class AiServerResponse<T>
        {
            [JsonProperty("result")]
            public bool Result { get; set; }

            [JsonProperty("data")]
            public T Data { get; set; }
        }

        public class AskSessionData
        {
            [JsonProperty("openid")]
            public string OpenId { get; set; }

            [JsonProperty("sessionId")]
            public string SessionId { get; set; }
        }

        public class AskChunkData
        {
            [JsonProperty("finish")]
            public bool Finish { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }
    }
}
