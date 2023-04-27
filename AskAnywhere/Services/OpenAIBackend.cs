using AskAnywhere.OpenAI;
using AskAnywhere.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AskAnywhere.Services
{
    public class OpenAIBackend : IAskBackend
    {
        private static string OPENAI_API = "https://api.openai.com/v1/chat/completions";

        private Action<string>? _onError;
        private Action? _onFinished;
        private Action<string>? _onTextReceived;
        private string _apiKey;

        private CancellationTokenSource _cancellationToken;

        public OpenAIBackend()
        {
            _apiKey = SettingsManager.Get<string>("OpenAIApiKey");
            _cancellationToken = new CancellationTokenSource();
        }

        public async IAsyncEnumerable<ResultChunk> Ask(AskMode mode, string target, string prompt)
        {
            _cancellationToken = new CancellationTokenSource();
            await foreach (var item in GenerateAnswerStream(mode, target, prompt, _cancellationToken.Token))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Special credit to iamlovedit. Generate OpenAI stream answer.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="target"></param>
        /// <param name="prompt"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<ResultChunk> GenerateAnswerStream(AskMode mode, string target, string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            HttpClient httpClient;
            if (SettingsManager.Get<bool>("UseProxy"))
            {
                var address = SettingsManager.Get<string>("ProxyAddress");
                var port = SettingsManager.Get<int>("ProxyPort");
                Debug.WriteLine($"using proxy: http://{address}:{port}");
                var proxy = new WebProxy()
                {
                    Address = new Uri($"http://{address}:{port}"),
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = true,
                };
                var handler = new HttpClientHandler()
                {
                    Proxy = proxy,
                };
                handler.SslProtocols = System.Security.Authentication.SslProtocols.None;
                httpClient = new HttpClient(handler);
            }
            else httpClient = new HttpClient();

            //httpClient.Timeout = TimeSpan.FromSeconds(10);

            var request = new HttpRequestMessage(HttpMethod.Post, OPENAI_API);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var completionData = CreateTextCompleteData(mode, target, prompt);

            var bodyJson = JsonConvert.SerializeObject(completionData);
            var strContent = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            request.Content = strContent;

            HttpResponseMessage? response = null;
            Exception? err = null;
            try
            {
                Debug.WriteLine("sending msg to OpenAI...");
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Connection error: {e.Message}");
                err = e;
            }

            if (err != null)
            {
                yield return new ResultChunk(ResultChunk.ChunkType.ERROR, $"ERR: {err.Message}");
                yield break;
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                Debug.WriteLine("receiving data...");
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var buffer = new byte[1024];
                int bytes;
                string stringBuffer = "";
                while ((bytes = await stream.ReadAsync(buffer, cancellationToken)) > 0 || !string.IsNullOrEmpty(stringBuffer))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string responseText = "";
                    if(bytes > 0)
                    {
                        responseText = Encoding.UTF8.GetString(buffer, 0, bytes);
                    }

                    if (string.IsNullOrEmpty(stringBuffer) || (!string.IsNullOrEmpty(responseText) && !responseText.StartsWith("data:")))
                    {
                        stringBuffer += responseText;
                        continue;
                    }

                    var parts = stringBuffer.Split("data: ");
                    stringBuffer = responseText;

                    foreach (var part in parts)
                    {
                        if (string.IsNullOrEmpty(part)) continue;
                        if (part.StartsWith("[DONE]"))
                        {
                            yield return new ResultChunk(ResultChunk.ChunkType.FINISH, "");
                            yield break;
                        }
                        ChatCompletionChunk? chunk = null;
                        try
                        {
                            chunk = JsonConvert.DeserializeObject<ChatCompletionChunk>(part);
                        }
                        catch (Exception)
                        {
                            chunk = null;
                        }
                        if (chunk == null)
                        {
                            yield return new ResultChunk(ResultChunk.ChunkType.ERROR, "ERR: Data incomplete");
                            yield break;
                        }
                        var content = chunk.Choices[0].Delta.Content;
                        if (!string.IsNullOrEmpty(content))
                        {
                            yield return new ResultChunk(ResultChunk.ChunkType.DATA, content);
                            continue;
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine(response.ToString());
                yield return new ResultChunk(ResultChunk.ChunkType.ERROR, $"网络请求错误 {response.StatusCode}");
                yield break;
            }
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
            //new ChatMessage() {Role = "system", Content = "This is a conversation between user and AI assistant."},
            new ChatMessage() {Role = "user", Content = systemMsg}
            },
                Stream = true
            };
            return data;
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

        public void Terminate()
        {
            _cancellationToken.Cancel();
        }
    }
}
