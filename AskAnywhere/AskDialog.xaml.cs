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

            var animY = new DoubleAnimation(Height, y, new Duration(TimeSpan.FromMilliseconds(250)));
            animY.EasingFunction = easing;


            BeginAnimation(WidthProperty, animX);
            BeginAnimation(HeightProperty, animY);
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.WriteLine(InputBox.ActualWidth);
            if (InputBox.ActualWidth + TargetBlock.ActualWidth >= 110)
            {
                Width = InputBox.ActualWidth + TargetBlock.ActualWidth + 140;
            }

            if (InputBox.LineCount >= 1)
            {
                Height = InputBox.LineCount * 16 + 64;
            }
        }
    }
}
