using System;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using AskAnywhere.Services;
using AskAnywhere.Commands;
using AskAnywhere.Properties;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using NHotkey.Wpf;
using NHotkey;
using AskAnywhere.i18n;
using AskAnywhere.Settings;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace AskAnywhere
{
    public class AskApp
    {
        private readonly System.Windows.Application _hostApp;
        private static TaskbarIcon? _notifyIcon;
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
            string language = Properties.Settings.Default.Language;
            Debug.WriteLine($"ERROR: loading language presets for {language}");

            // set up language
            LanguageSwitcher.Change(language);

            // check if the backend is properly set, if not then spawn a settings wizzard dialog.
            if (string.IsNullOrEmpty(Properties.Settings.Default.BackendType))
            {
                var settingDialog = new SettingsWindow();
                settingDialog.ShowDialog();

                // if user closes dialog without setting any api settings then quit app.
                if (string.IsNullOrEmpty(Properties.Settings.Default.BackendType))
                {
                    _hostApp.Shutdown();
                    return;
                }
            }

            SetupTaskIcon();

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
            DisposeTaskIcon();
        }

        public static void SetupTaskIcon()
        {
            var notifyIconDic = new ResourceDictionary();
            notifyIconDic.Source = new Uri(@"Notification/NotifyIconResources.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Add(notifyIconDic);

            // create taskicon for app
            _notifyIcon = (TaskbarIcon)Application.Current.FindResource("NotifyIcon");
            _notifyIcon.ForceCreate(true);
        }

        public static void RefreshTaskIcon()
        {
            _notifyIcon.UpdateDefaultStyle();
            _notifyIcon.DataContext = new NotifyIconViewModel();
        }

        public static void DisposeTaskIcon()
        {
            _notifyIcon?.Dispose();
        }

        /// <summary>
        /// set up a global hook listening for hotkeys.
        /// </summary>
        private void EnableKeyboardHook()
        {
            HotkeyManager.Current.AddOrReplace("AskAnything",
                new KeyGesture(Key.OemQuestion, ModifierKeys.Control | ModifierKeys.Alt), OnAsk);

            HotkeyManager.Current.AddOrReplace("AskAnythingTerminate", Key.Escape, ModifierKeys.None, OnTerminate);
        }

        /// <summary>
        /// disable app hotkey hooking.
        /// </summary>
        private void DisableKeyboardHook()
        {
            HotkeyManager.Current.Remove("AskAnything");
            HotkeyManager.Current.Remove("AskAnythingTerminate");
        }

        private void OnTerminate(object? sender, HotkeyEventArgs e)
        {
            _backendService?.Terminate();
        }

        private void OnAsk(object? sender, HotkeyEventArgs e)
        {
            // if there is an active dialog, we should quit~
            if (_dialog != null && _dialog.IsActive) return;

            if (!Utils.GetCaretPosition(out _cachedX, out _cachedY, out _cachedWidth, out _cachedHeight, out _hWnd))
            {
                Debug.WriteLine("no caret found or error occurs.");
                return;
            }

            // while no caret focused on screen, 0,0 returned thus we dont need to spawn ask dialog.
            if (_cachedX == 0 && _cachedY == 0)
                return;

            // We get the target backend type name here from settings.
            var backendTypeName = SettingsManager.Get<string>("BackendType");

            if (string.IsNullOrEmpty(backendTypeName))
            {
                throw new Exception("ERROR: no backend type specified!");
            }

            // Try create a backend instance here by type name.
            try
            {
                _backendService = (IAskBackend)typeof(AskApp).Assembly.CreateInstance(backendTypeName)!;
            }
            catch (Exception err)
            {
                throw new Exception("ERROR: no backend type found!");
            }

            if (_backendService == null)
            {
                throw new Exception("ERROR: no backend instance created!");
            }

            // spawn a ask bar with a preset view model.
            _vm = new AskDialogViewModel
            {
                // command list
                AskCommands = _commands,

                // command while confirm triggered (typically ENTER button down)
                ConfirmCommand = new RelayCommand(async () =>
                {
                    Debug.WriteLine($"mode:{_vm.AskMode}, target:{_vm.AskTarget}, prompt:{_vm.Prompt}");

                    if (_dialog == null) return;

                    var requestPrompt = _vm.Prompt;

                    _vm.CurrentState = InputState.OUTPUT;
                    _vm.Prompt = "";
                    //await Task.Delay(50);
                    //var width = _dialog!.CalculateActualWidth();
                    _dialog!.ChangeSize(144, 80);

                    if (!Utils.SetActiveWindowAndCaret(_hWnd, _cachedX, _cachedY))
                    {
                        Debug.WriteLine("set caret failed, please check out the code.");
                        return;
                    }

                    DateTime timeStamp = DateTime.Now;

                    // now we use async stream to receive text
                    try
                    {
                        await foreach (var chunk in _backendService.Ask(_vm.AskMode, _vm.AskTarget, requestPrompt))
                        {

                            if (chunk.Type == ResultChunk.ChunkType.DATA)
                            {
                                if (!Utils.SetActiveWindowAndCaret(_hWnd, _cachedX, _cachedY))
                                {
                                    Debug.WriteLine("set caret failed, please check out the code.");
                                    return;
                                }

                                // hack: to avoid caret being inactive.
                                var currentTime = DateTime.Now;
                                if (currentTime.Subtract(timeStamp).TotalMilliseconds > 4000)
                                {
                                    timeStamp = currentTime;
                                    System.Windows.Forms.SendKeys.SendWait("{LEFT}");
                                    System.Windows.Forms.SendKeys.SendWait("{RIGHT}");
                                }

                                await SendText(chunk.Data);

                                if (!Utils.GetCaretPosition(out _cachedX, out _cachedY, out _cachedWidth, out _cachedHeight, out _hWnd))
                                {
                                    Debug.WriteLine("no caret found or error occurs.");
                                    return;
                                }

                                if (_cachedX > 0 && _cachedY > 0)
                                    _dialog?.MoveTo((_cachedX - 20) / _dpiRatio, (_cachedY + _cachedHeight - 22) / _dpiRatio, false);
                            }

                            if (chunk.Type == ResultChunk.ChunkType.FINISH)
                            {
                                Debug.WriteLine("finish detected!");
                                await CloseDialogWithState(InputState.FINISH);
                                break;
                            }

                            if (chunk.Type == ResultChunk.ChunkType.ERROR)
                            {
                                Debug.WriteLine("err detected!");
                                await CloseDialogWithState(InputState.ERROR);
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        await CloseDialogWithState(InputState.ERROR);
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

            //_dialog.ChangeSize(300, 80);

            e.Handled = true;
        }

        private async Task CloseDialogWithState(InputState state)
        {
            if (_vm != null) _vm.CurrentState = state;
            await Task.Delay(50);
            var width = _dialog!.CalculateActualWidth();
            _dialog!.ChangeSize(width, _dialog.Height);
            await Task.Delay(2000);
            _dialog!.Close();
            _dialog = null;
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
        public async Task SendText(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var parts = data.Split("\n");
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    if (!Utils.SendTextToCaret(_hWnd, parts[i]))
                    {
                        Debug.WriteLine("ERROR: can not send text!");
                    }
                }

                if (i < parts.Length - 1)
                {
                    //Clipboard.SetDataObject(" \n", true);
                    //System.Windows.Forms.SendKeys.SendWait("^v");
                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                    await Task.Delay(50);
                }
            }

            //if (!Utils.SendTextToCaret(_hWnd, data))
            //{
            //    Debug.WriteLine("ERROR: can not send text!");
            //}

            //System.Windows.Clipboard.SetDataObject(data, true);
            //SendKeys.SendWait("^v");
        }
    }
}