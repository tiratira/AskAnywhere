using AskAnywhere.Common;
using AskAnywhere.i18n;
using AskAnywhere.Settings.Pages;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AskAnywhere.Settings
{
    public enum Language
    {
        CHINESE = 0,
        ENGLISH
    }

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

        #region Language settings

        public Language DisplayLanguage { get; set; } = Language.CHINESE;

        public ICommand ChangeLanguageCommand { get; set; }

        #endregion


        #region Backend settings

        public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.OPENAI_DIRECT;

        public SettingPage CurrentPage { get; set; } = SettingPage.BACKEND_SETTING;

        public string OpenAIApiKey { get; set; } = "";

        public string AICloudKey { get; set; } = "";

        public string OpenAIProxyServerUrl { get; set; } = "";

        public string OpenAIProxyServerSecret { get; set; } = "";

        #endregion


        #region Proxy settings

        public bool UseProxy { get; set; } = false;

        public string ProxyAddress { get; set; } = "";

        public int ProxyPort { get; set; } = 8080;

        #endregion

        public ICommand ConfirmCommand { get; set; }


        public SettingsViewModel()
        {
            DisplayLanguage = Language.ENGLISH;

            if (Properties.Settings.Default.Language == "en-us")
                DisplayLanguage = Language.ENGLISH;

            if (Properties.Settings.Default.Language == "zh-cn")
                DisplayLanguage = Language.CHINESE;

            UseProxy = Properties.Settings.Default.UseProxy;
            ProxyAddress = Properties.Settings.Default.ProxyAddress;
            ProxyPort = Properties.Settings.Default.ProxyPort;

            if (Properties.Settings.Default.BackendType == "AskAnywhere.Services.OpenAIBackend")
                ConnectionMode = ConnectionMode.OPENAI_DIRECT;

            if (Properties.Settings.Default.BackendType == "AskAnywhere.Services.AICloudBackend")
                ConnectionMode = ConnectionMode.AICLOUD;

            if (Properties.Settings.Default.BackendType == "AskAnywhere.Services.CustomProxyServer")
                ConnectionMode = ConnectionMode.OPENAI_PROXY_SERVER;

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

                Properties.Settings.Default.UseProxy = UseProxy;
                Properties.Settings.Default.ProxyAddress = ProxyAddress;
                Properties.Settings.Default.ProxyPort = ProxyPort;

                Properties.Settings.Default.Save();
            };

            ConfirmCommand = command;

            var changeLanguageCmd = new DelegateCommand();
            changeLanguageCmd.CanExecuteFunc = () => true;
            changeLanguageCmd.CommandAction = (value) =>
            {
                var indexString = (string)value;
                var lang = (Language)int.Parse(indexString);
                Debug.WriteLine($"change language to {lang}");
                switch (lang)
                {
                    case Language.CHINESE:
                        Properties.Settings.Default.Language = "zh-cn";
                        LanguageSwitcher.Change("zh-cn");
                        break;
                    case Language.ENGLISH:
                        Properties.Settings.Default.Language = "en-us";
                        LanguageSwitcher.Change("en-us");
                        break;
                    default:
                        LanguageSwitcher.Change("en-us");
                        break;
                }
                AskApp.RefreshTaskIcon();
            };
            ChangeLanguageCommand = changeLanguageCmd;
        }
    }
}
