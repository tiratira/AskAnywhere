using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AskAnywhere
{
    /// <summary>
    /// AskDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AskDialog : Window
    {
        //public event EventHandler<string> OnCommand;
        //public event EventHandler OnConfirm;

        public AskDialog()
        {
            InitializeComponent();
            InputBox.Focus();
        }

        public void MoveTo(double x, double y)
        {
            var easing = new QuadraticEase();
            easing.EasingMode = EasingMode.EaseOut;

            var animX = new DoubleAnimation(Left, x, new Duration(TimeSpan.FromMilliseconds(250)));
            animX.EasingFunction = easing;

            var animY = new DoubleAnimation(Top, y, new Duration(TimeSpan.FromMilliseconds(250)));
            animY.EasingFunction = easing;


            BeginAnimation(LeftProperty, animX);
            BeginAnimation(TopProperty, animY);
        }

        public void ChangeSize(double x, double y)
        {
            var easing = new QuadraticEase();
            easing.EasingMode = EasingMode.EaseOut;

            var animX = new DoubleAnimation(Width, x, new Duration(TimeSpan.FromMilliseconds(250)));
            animX.EasingFunction = easing;
            animX.FillBehavior = FillBehavior.Stop;

            var animY = new DoubleAnimation(Height, y, new Duration(TimeSpan.FromMilliseconds(250)));
            animY.EasingFunction = easing;
            animY.FillBehavior = FillBehavior.Stop;

            BeginAnimation(WidthProperty, animX);
            BeginAnimation(HeightProperty, animY);
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InputBox.Text.Length > 0)
            {
                HintBox.Visibility = Visibility.Collapsed;
            }
            else if (InputBox.Visibility == Visibility.Visible)
            {
                HintBox.Visibility = Visibility.Visible;
            }

            if (InputBox.LineCount > 1)
            {
                Height = InputBox.LineCount * 16 + 64;
                Width = InputBox.MaxWidth + TargetBlock.ActualWidth + ModeBlock.ActualWidth + 64;
                return;
            }

            Height = 80;
            var width = InputBox.ActualWidth + TargetBlock.ActualWidth + ModeBlock.ActualWidth + 64;
            width = width >= 300 ? width : 300;

            Width = width;
        }

        private void InputBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (InputBox.Visibility == Visibility.Collapsed)
            {
                HintBox.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeSize(300, 80);
        }
    }
}
