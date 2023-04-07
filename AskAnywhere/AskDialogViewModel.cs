using AskAnywhere.Commands;
using AskAnywhere.Services;
using CommunityToolkit.Mvvm.Input;
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

namespace AskAnywhere
{
    public enum InputState
    {
        INPUT = 0,
        OUTPUT,
        FINISH,
        ERROR
    }

    public class AskDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public InputState CurrentState { get; set; } = InputState.INPUT;

        public AskCommands? AskCommands { get; set; }

        public AskMode AskMode { get; set; } = AskMode.ASK;

        public string AskTarget { get; set; }

        public string Prompt { get; set; }

        public ICommand? ModeCommand { get; set; }

        public ICommand? ConfirmCommand { get; set; }
        public ICommand? CancelCommand { get; set; }

        //public Action? ConfirmAction { get; set; }

        public AskDialogViewModel()
        {
            ConfirmCommand = null;
            CancelCommand = null;
            AskTarget = string.Empty;
            Prompt = string.Empty;
            ModeCommand = new RelayCommand<TextBox>(HandleCommand);
            //ConfirmCommand = new RelayCommand<TextBox>(HandleConfirm);
        }

        public void HandleCommand(TextBox? textBox)
        {
            if (textBox == null) return;
            var value = textBox.Text;
            if (value == null) { return; }
            if (value.Length > 0)
            {
                if (value[0] == '/')
                {
                    var spaceIndex = value.IndexOf(' ');
                    string? command;
                    if (spaceIndex > 1)
                    {
                        command = value.Substring(1, spaceIndex);
                    }
                    else
                    {
                        command = value.Substring(1);
                    }
                    if (!ProcessCommand(command)) Prompt = $"{value} "; else Prompt = "";
                    textBox.Text = Prompt;
                    textBox.CaretIndex = int.MaxValue;
                    return;
                }
                Prompt = $"{value} ";
            }
            textBox.Text = Prompt;
            textBox.CaretIndex = int.MaxValue;
        }

        private bool ProcessCommand(string command)
        {
            if (AskCommands == null) return false;
            foreach (var item in AskCommands.Commands!)
            {
                if (item.Command == command)
                {
                    if (item.Depend == -1 || item.Depend == (int)AskMode)
                    {
                        AskMode = (AskMode)item.Mode;
                        AskTarget = item.Target!;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
