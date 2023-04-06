using System;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using AskAnywhere.Services;
using AskAnywhere.Commands;
using AskAnywhere.Properties;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using NHotkey.Wpf;
using NHotkey;


namespace AskAnywhere
{
    public class AskApp
    {
        private readonly System.Windows.Application _hostApp;
        private TaskbarIcon? _notifyIcon;
        private AskCommands? _commands;
        private AskDialogViewModel? _vm;

        private IAskBackend? _backendService;

        private IntPtr _hWnd;

        private AskDialog? _dialog;
        private int _cachedX;
        private int _cachedY;
        private int _cachedWidth;
        private int _cachedHeight;
        private double _dpiRatio = 1.0f;

        public AskApp(System.Windows.Application hostApp)
        {
            _hostApp = hostApp;
        }

        public void Startup()
        {
            // check if the backend is properly set, if not then spawn a settings wizzard dialog.
            if (string.IsNullOrEmpty(Properties.Settings.Default.BackendType))
            {
                var settingDialog = new SettingsWindow();
                settingDialog.ShowDialog();

                // if user closes dialog without setting any api settings then quit app.
                if (string.IsNullOrEmpty(Properties.Settings.Default.BackendType)) _hostApp.Shutdown();
            }

            // create taskicon for app
            _notifyIcon = (TaskbarIcon)_hostApp.FindResource("NotifyIcon");
            _notifyIcon.ForceCreate(true);

            // read all commands from database file commands.json
            _commands = JsonConvert.DeserializeObject<AskCommands>(Resource.CommandList);

            // Load dll from file which contains caret and window helper functions
            var loc = typeof(App).Assembly.Location;
            var path = Path.GetDirectoryName(loc);
            Assembly.LoadFile(path + "/AskAnywhere.Utils.dll");

            // finish setup by enabling a global hook to capture hotkey
            EnableKeyboardHook();
        }

        public void ShutDown()
        {
            DisableKeyboardHook();
            _notifyIcon?.Dispose();
        }

        /// <summary>
        /// set up a global hook listening for hotkeys.
        /// </summary>
        private void EnableKeyboardHook()
        {
            HotkeyManager.Current.AddOrReplace("AskAnything",
                new KeyGesture(Key.OemQuestion, ModifierKeys.Control | ModifierKeys.Alt), OnAsk);
        }

        /// <summary>
        /// disable app hotkey hooking.
        /// </summary>
        private void DisableKeyboardHook()
        {
            HotkeyManager.Current.Remove("AskAnything");
        }

        private void OnAsk(object? sender, HotkeyEventArgs e)
        {
            DisableKeyboardHook();
            if (!Utils.GetCaretPosition(out _cachedX, out _cachedY, out _cachedWidth, out _cachedHeight, out _hWnd))
            {
                Debug.WriteLine("no caret found or error occurs.");
                return;
            }
            Debug.WriteLine($"caret pos: {_cachedX}, {_cachedY}");

            // We get the target backend type name here from settings.
            var backendTypeName = Properties.Settings.Default.BackendType;

            if (string.IsNullOrEmpty(backendTypeName))
            {
                throw new Exception("ERROR: no backend type specified!");
            }

            // Try create a backend instance here by type name.
            try
            {
                _backendService = (IAskBackend)typeof(AskApp).Assembly.CreateInstance(backendTypeName)!;
                _backendService.SetAuthorizationKey(Properties.Settings.Default.BackendAuthKey);
            }
            catch (Exception err)
            {
                throw new Exception("ERROR: no backend type found!");
            }

            if (_backendService == null)
            {
                throw new Exception("ERROR: no backend instance created!");
            }

            // If backend service instance created successfully, then we can properly set essential callbacks to work.
            _backendService.SetTextReceivedCallback(text =>
            {
                if (!Utils.SetActiveWindowAndCaret(_hWnd, _cachedX, _cachedY))
                {
                    Debug.WriteLine("set caret failed, please check out the code.");
                    return;
                }

                SendText(text);

                if (!Utils.GetCaretPosition(out _cachedX, out _cachedY, out _cachedWidth, out _cachedHeight, out _hWnd))
                {
                    Debug.WriteLine("no caret found or error occurs.");
                    return;
                }

                _dialog?.MoveTo((_cachedX - 20) / _dpiRatio, (_cachedY + _cachedHeight - 22) / _dpiRatio);
            });

            _backendService.SetFinishedCallback(async () =>
            {
                if (_vm != null) _vm.CurrentState = InputState.FINISH;
                _dialog?.ChangeSize(112, _dialog.Height);
                await Task.Delay(1000);
                _dialog?.Close();
            });

            // while no caret focused on screen, 0,0 returned thus we dont need to spawn ask dialog.
            if (_cachedX == 0 && _cachedY == 0)
                return;
            _vm = new AskDialogViewModel
            {
                AskCommands = _commands,
                ConfirmCommand = new RelayCommand(() =>
                {
                    Debug.WriteLine($"mode:{_vm.AskMode}, target:{_vm.AskTarget}, prompt:{_vm.Prompt}");
                    _vm.CurrentState = InputState.OUTPUT;
                    _dialog?.ChangeSize(140, 80);
                    _backendService.Ask(_vm.AskMode, _vm.AskTarget, _vm.Prompt);

                    if (!Utils.SetActiveWindowAndCaret(_hWnd, _cachedX, _cachedY))
                    {
                        Debug.WriteLine("set caret failed, please check out the code.");
                        return;
                    }
                }),
                CancelCommand = new RelayCommand(() => { _dialog?.Close(); }),
            };

            _dialog = new AskDialog();
            var dpiYProperty =
                typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);
            _dpiRatio = (int)(dpiYProperty?.GetValue(null, null) ?? 96.0) / 96.0;
            _dialog.DataContext = _vm;

            //_dialog.OnCommand += _vm.HandleCommand;
            //_dialog.OnConfirm += SubmitPrompt;
            _dialog.Closed += Dialog_Closed;
            _dialog.InputBox.LostKeyboardFocus += Dialog_LostFocus;
            _dialog.Left = (_cachedX - 20) / _dpiRatio;
            _dialog.Top = (_cachedY + _cachedHeight - 22) / _dpiRatio;
            _dialog.Show();
            _dialog.Activate();
            _dialog.Topmost = false; //HACK: focus issue fix
            _dialog.Topmost = true; //HACK: focus issue fix
            _dialog.Focus();

            e.Handled = true;
        }

        private void Dialog_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.CurrentState == InputState.INPUT) _dialog?.Close();
            }
            catch (Exception)
            {
            }
        }

        private void Dialog_Closed(object? sender, EventArgs e)
        {
            EnableKeyboardHook();
        }

        /// <summary>
        /// a tricky method to input texts into target caret position.
        /// we copy text parts into copyboard, and simulate a 'ctrl+v' key input on target caret place.
        /// </summary>
        /// <param name="data"></param>
        public void SendText(string data) 
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            if (!Utils.SendTextToCaret(_hWnd, data))
            {
                Debug.WriteLine("ERROR: can not send text!");
            }

            //System.Windows.Clipboard.SetDataObject(data, true);
            //SendKeys.SendWait("^v");
        }
    }
}