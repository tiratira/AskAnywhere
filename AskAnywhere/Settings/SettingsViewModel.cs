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

        public Language DisplayLanguage
        {
            get
            {
                if (SettingsManager.Get<string>("Language") == "en-us") return Language.ENGLISH;
                if (SettingsManager.Get<string>("Language") == "zh-cn") return Language.CHINESE;
                SettingsManager.Set("Language", "en-us");
                SettingsManager.SaveAll();
                return Language.ENGLISH;
            }
            set
            {
                switch (value)
                {
                    case Language.CHINESE:
                        SettingsManager.Set("Language", "zh-cn");
                        LanguageSwitcher.Change("zh-cn");
                        break;
                    case Language.ENGLISH:
                        SettingsManager.Set("Language", "en-us");
                        LanguageSwitcher.Change("en-us");
                        break;
                    default:
                        SettingsManager.Set("Language", "en-us");
                        LanguageSwitcher.Change("en-us");
                        break;
                }
                SettingsManager.SaveAll();
                AskApp.RefreshTaskIcon();
            }
        }

        #endregion


        #region Backend settings

        public ConnectionMode ConnectionMode
        {
            get
            {
                var backendType = SettingsManager.Get<string>("BackendType");
                if (backendType == "AskAnywhere.Services.OpenAIBackend")
                    return ConnectionMode.OPENAI_DIRECT;

                if (backendType == "AskAnywhere.Services.AICloudBackend")
                    return ConnectionMode.AICLOUD;

                if (backendType == "AskAnywhere.Services.CustomProxyServer")
                    return ConnectionMode.OPENAI_PROXY_SERVER;
                SettingsManager.Set("BackendType", "AskAnywhere.Services.AICloudBackend");
                return ConnectionMode.AICLOUD;
            }
            set
            {
                switch (value)
                {
                    case ConnectionMode.OPENAI_DIRECT:
                        SettingsManager.Set("BackendType", "AskAnywhere.Services.OpenAIBackend");
                        break;
                    case ConnectionMode.OPENAI_PROXY_SERVER:
                        SettingsManager.Set("BackendType", "AskAnywhere.Services.CustomProxyServer");
                        break;
                    case ConnectionMode.AICLOUD:
                        SettingsManager.Set("BackendType", "AskAnywhere.Services.AICloudBackend");
                        break;
                }
            }
        }

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

            UseProxy = SettingsManager.Get<bool>("UseProxy");
            if (UseProxy)
            {
                ProxyAddress = SettingsManager.Get<string>("ProxyAddress");
                ProxyPort = SettingsManager.Get<int>("ProxyPort");
            }

            OpenAIApiKey = SettingsManager.Get<string>("OpenAIApiKey");
            AICloudKey = SettingsManager.Get<string>("AICloudKey");
            OpenAIProxyServerUrl = SettingsManager.Get<string>("OpenAIProxyServerUrl");
            OpenAIProxyServerSecret = SettingsManager.Get<string>("OpenAIProxyServerSecret");

            var command = new DelegateCommand();

            command.CanExecuteFunc = () =>
            {
                if (ConnectionMode == ConnectionMode.OPENAI_DIRECT && string.IsNullOrEmpty(OpenAIApiKey)) return false;
                if (ConnectionMode == ConnectionMode.AICLOUD && string.IsNullOrEmpty(AICloudKey)) return false;
                if (ConnectionMode == ConnectionMode.OPENAI_PROXY_SERVER
                && (string.IsNullOrEmpty(OpenAIProxyServerUrl)
                || string.IsNullOrEmpty(OpenAIProxyServerSecret))) return false;
                if (UseProxy && (string.IsNullOrEmpty(ProxyAddress) || ProxyPort == 0)) return false;
                return true;
            };

            command.CommandAction = (_) =>
            {
                SettingsManager.Set("UseProxy", UseProxy);

                if (UseProxy)
                {
                    SettingsManager.Set("ProxyAddress", ProxyAddress);
                    SettingsManager.Set("ProxyPort", ProxyPort);
                }

                if (ConnectionMode == ConnectionMode.OPENAI_DIRECT) SettingsManager.Set("OpenAIApiKey", OpenAIApiKey);
                if (ConnectionMode == ConnectionMode.OPENAI_PROXY_SERVER)
                {
                    SettingsManager.Set("OpenAIProxyServerUrl", OpenAIProxyServerUrl);
                    SettingsManager.Set("OpenAIProxyServerSecret", OpenAIProxyServerSecret);
                }
                if (ConnectionMode == ConnectionMode.AICLOUD) SettingsManager.Set("AICloudKey", AICloudKey);

                SettingsManager.SaveAll();
            };

            ConfirmCommand = command;
        }
    }
}
