using System;
using System.Collections.Generic;

namespace AskAnywhere.Services
{
    public enum AskMode
    {
        ASK = 0,
        CODE,
        TRANSLATION
    }

    public class ResultChunk
    {
        public enum ChunkType
        {
            DATA = 0,
            FINISH,
            ERROR
        }
        public ChunkType Type { get; set; } = ChunkType.DATA;
        public string Data { get; set; } = "";

        public ResultChunk(ChunkType type, string data)
        {
            Type = type;
            Data = data;
        }
    }

    /// <summary>
    /// An IAskBackend interface is a service for Ask Anywhere's retrieval of responses to questions.
    /// </summary>
    public interface IAskBackend
    {

        /// <summary>
        /// This is used to set authorization information for the target backend service.
        /// </summary>
        /// <param name="key">auth key, typicaly API key.</param>
        //public void SetAuthorizationKey(string key);

        /// <summary>
        /// Send an asking request.
        /// </summary>
        /// <param name="mode">The request mode of this asking request, supposed to be one of ASK, CODE and TRANSLATION</param>
        /// <param name="target">The target string indicates a secondary infomation that the request contains.</param>
        /// <param name="prompt">The main prompt to be sent to backend.</param>
        /// <returns>the answering text stream</returns>
        public IAsyncEnumerable<ResultChunk> Ask(AskMode mode, string target, string prompt);

        /// <summary>
        /// Set up a callback function to trigger when receiving a text reply.
        /// </summary>
        /// <param name="onTextReceived">Callback function when text message received.</param>
        public void SetTextReceivedCallback(Action<string> onTextReceived);

        /// <summary>
        /// Set up a callback function to trigger when text message ends.
        /// </summary>
        /// <param name="onFinished">Callback function when text message ends.</param>
        public void SetFinishedCallback(Action onFinished);

        /// <summary>
        /// Set up a callback function to trigger when error occurred.
        /// </summary>
        /// <param name="onError">Callback that triggered when there is a backend error.</param>
        public void SetErrorCallback(Action<string> onError);

        /// <summary>
        /// This is used to terminate process of current text output.
        /// </summary>
        public void Terminate();
    }
}
