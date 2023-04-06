using AskAnywhere.Common;
using AskAnywhere.Settings.Pages;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace AskAnywhere.Settings
{
    public enum ConnectionMode
    {
        OPENAI_DIRECT = 0,
        OPENAI_PROXY_SERVER = 1,
        AICLOUD
    }

    public enum SettingPage
    {
        LANG_SETTING = 0,
        BACKEND_SETTING,
        PROXY_SETTING
    }


    public class SettingsViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;

        public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.OPENAI_DIRECT;

        public SettingPage CurrentPage { get; set; } = SettingPage.BACKEND_SETTING;

        public string PageUri { get; set; }

        private static Dictionary<SettingPage, string> PAGE_MAP = new Dictionary<SettingPage, string>()
        {
            {SettingPage.LANG_SETTING, "pack://application:,,,/AskAnywhere;component/Settings/Pages/LanguagePage.xaml" },
            {SettingPage.BACKEND_SETTING, "pack://application:,,,/AskAnywhere;component/Settings/Pages/BackendPage.xaml" },
            {SettingPage.PROXY_SETTING, "pack://application:,,,/AskAnywhere;component/Settings/Pages/ProxyPage.xaml" }
        };

        public ICommand ChangePageCommand { get; set; }

        public string OpenAIApiKey { get; set; } = "";

        public string AICloudKey { get; set; } = "";

        public string OpenAIProxyServerUrl { get; set; } = "";

        public string OpenAIProxyServerSecret { get; set; } = "";

        public ICommand ConfirmCommand { get; set; }

        public SettingsViewModel()
        {
            PageUri = PAGE_MAP[CurrentPage];

            if (Properties.Settings.Default.BackendType == "AskAnywhere.Services.OpenAIBackend")
                ConnectionMode = ConnectionMode.OPENAI_DIRECT;

            if (Properties.Settings.Default.BackendType == "AskAnywhere.Services.AICloudBackend")
                ConnectionMode = ConnectionMode.AICLOUD;

            if (ConnectionMode == ConnectionMode.OPENAI_DIRECT)
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
                if (ConnectionMode == ConnectionMode.OPENAI_DIRECT && string.IsNullOrEmpty(OpenAIApiKey)) return false;
                if (ConnectionMode == ConnectionMode.AICLOUD && string.IsNullOrEmpty(AICloudKey)) return false;
                return true;
            };

            command.CommandAction = (_) =>
            {
                if (ConnectionMode == ConnectionMode.OPENAI_DIRECT)
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

            var changePageCommand = new DelegateCommand();
            changePageCommand.CommandAction = value =>
            {
                var indexString = (string)value;
                Debug.WriteLine($"changing to page: {indexString}");
                var page = (SettingPage)int.Parse(indexString);
                if (CurrentPage != page)
                {
                    PageUri = PAGE_MAP[page];
                    CurrentPage = page;
                }
            };

            changePageCommand.CanExecuteFunc = () => true;

            ChangePageCommand = changePageCommand;
        }
    }
}
