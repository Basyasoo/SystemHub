using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SystemHub.Services;

namespace SystemHub.Views
{
    public partial class PasswordPromptWindow : Window
    {
        private TaskCompletionSource<bool>? _tcs;
        private string _processName = "";

        public PasswordPromptWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Task<bool> ShowPromptAsync(string processName, int pid)
        {
            var tcs = new TaskCompletionSource<bool>();

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var window = new PasswordPromptWindow
                    {
                        _tcs = tcs,
                        _processName = processName
                    };

                    window.UpdateLabels();
                    window.Closed += (s, e) =>
                    {
                        tcs.TrySetResult(false);
                    };

                    window.Show();
                    
                    // Focus PasswordBox after window renders
                    Dispatcher.UIThread.Post(() =>
                    {
                        var pBox = window.FindControl<TextBox>("PasswordBox");
                        pBox?.Focus();
                    }, DispatcherPriority.Input);
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(false);
                }
            });

            return tcs.Task;
        }

        private void UpdateLabels()
        {
            var subtext = this.FindControl<TextBlock>("SubtextText");
            if (subtext != null)
            {
                subtext.Text = string.Format(LocalizationService.Instance.AppLockPromptFormat, _processName);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            _tcs?.TrySetResult(false);
            Close();
        }

        private void OnUnlockClick(object? sender, RoutedEventArgs e)
        {
            var pBox = this.FindControl<TextBox>("PasswordBox");
            var errText = this.FindControl<TextBlock>("ErrorText");
            if (pBox == null) return;

            string enteredPassword = pBox.Text ?? "";
            if (AppLockService.Instance.VerifyPassword(enteredPassword))
            {
                _tcs?.TrySetResult(true);
                Close();
            }
            else
            {
                if (errText != null)
                {
                    errText.IsVisible = true;
                }
                pBox.Text = "";
                pBox.Focus();
            }
        }

        private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnUnlockClick(sender, new RoutedEventArgs());
            }
        }
    }
}

