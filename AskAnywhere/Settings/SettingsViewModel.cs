using AskAnywhere.Common;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AskAnywhere.Settings
{
    public enum ConnectionMode
    {
        OPENAI = 0,
        AICLOUD
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ConnectionMode ConnectionMode { get; set; }
        public string OpenAIApiKey { get; set; }
        public string AICloudKey { get; set; }
        public ICommand ConfirmCommand { get; set; }

        public SettingsViewModel()
        {
            ConnectionMode = 0;
            OpenAIApiKey = "";
            AICloudKey = "";

            if (Properties.Settings.Default.BackendType == "AskAnywhere.Services.OpenAIBackend")
                ConnectionMode = ConnectionMode.OPENAI;

            if (Properties.Settings.Default.BackendType == "AskAnywhere.Services.AICloudBackend")
                ConnectionMode = ConnectionMode.AICLOUD;

            if (ConnectionMode == ConnectionMode.OPENAI)
            {
                OpenAIApiKey = Properties.Settings.Default.BackendAuthKey;
                AICloudKey = "";
            }
            if (ConnectionMode == ConnectionMode.AICLOUD)
            {
                AICloudKey = Properties.Settings.Default.BackendAuthKey;
                OpenAIApiKey = "";
            }

            var command = new DelegateCommand();

            command.CanExecuteFunc = () =>
            {
                if (ConnectionMode == ConnectionMode.OPENAI && string.IsNullOrEmpty(OpenAIApiKey)) return false;
                if (ConnectionMode == ConnectionMode.AICLOUD && string.IsNullOrEmpty(AICloudKey)) return false;
                return true;
            };

            command.CommandAction = () =>
            {
                if (ConnectionMode == ConnectionMode.OPENAI)
                {
                    Properties.Settings.Default.BackendAuthKey = OpenAIApiKey;
                    Properties.Settings.Default.BackendType = "AskAnywhere.Services.OpenAIBackend";
                }
                if (ConnectionMode == ConnectionMode.AICLOUD)
                {
                    Properties.Settings.Default.BackendAuthKey = AICloudKey;
                    Properties.Settings.Default.BackendType = "AskAnywhere.Services.AICloudBackend";
                }

                Properties.Settings.Default.Save();
            };

            ConfirmCommand = command;
        }
    }
}
