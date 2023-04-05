using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskAnywhere.Services
{
    public class OpenAIBackend : IAskBackend
    {
        public void Ask(AskMode mode, string target, string prompt)
        {
            throw new NotImplementedException();
        }

        public void SetAuthorizationKey(string key)
        {
            throw new NotImplementedException();
        }

        public void SetErrorCallback(Action<string> onError)
        {
            throw new NotImplementedException();
        }

        public void SetFinishedCallback(Action onFinished)
        {
            throw new NotImplementedException();
        }

        public void SetTextReceivedCallback(Action<string> onTextReceived)
        {
            throw new NotImplementedException();
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
