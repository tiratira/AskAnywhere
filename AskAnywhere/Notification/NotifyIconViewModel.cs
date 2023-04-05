using AskAnywhere.Common;
using AskAnywhere.Settings;
using System.Windows;
using System.Windows.Input;

namespace AskAnywhere
{
    internal class NotifyIconViewModel
    {
        private SettingsWindow _settingsWindow;

        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => _settingsWindow == null || _settingsWindow.IsActive == false,
                    CommandAction = () =>
                    {
                        _settingsWindow = new SettingsWindow();
                        var vm = new SettingsViewModel();
                        _settingsWindow.DataContext = vm;
                        _settingsWindow.Show();
                    }
                };
            }
        }

        /// <summary>
        /// Hides the main window. This command is only enabled if a window is open.
        /// </summary>
        public ICommand HideWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => Application.Current.MainWindow.Close(),
                    CanExecuteFunc = () => Application.Current.MainWindow != null
                };
            }
        }


        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
            }
        }
    }
}
