using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Overlay
{
    /// <summary>
    /// Simple Avalonia overlay window for displaying translated text.
    /// This is lightweight and intended for replacement/configuration by the user.
    /// </summary>
    public class AvaloniaOverlayService : IOverlayService
    {
        private Window? _window;
        private TextBlock? _textBlock;

        public Task HideAsync(CancellationToken cancellationToken)
        {
            if (_window == null) return Task.CompletedTask;

            Dispatcher.UIThread.Post(() =>
            {
                _window.Hide();
            });

            return Task.CompletedTask;
        }

        public Task ShowTextAsync(string text, CancellationToken cancellationToken)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_window == null || _textBlock == null)
                    CreateWindow();

                if (_textBlock != null)
                    _textBlock.Text = text;

                _window?.Show();
            });

            return Task.CompletedTask;
        }

        private void CreateWindow()
        {
            var topLevel = new Window
            {
                CanResize = false,
                SystemDecorations = SystemDecorations.None,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                Background = Brushes.Transparent,
                Topmost = true,
                Width = 800,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            var border = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#99000000")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };

            _textBlock = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 20,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MaxWidth = 760,
                Text = string.Empty,
                TextAlignment = Avalonia.Media.TextAlignment.Center
            };

            border.Child = _textBlock;
            topLevel.Content = border;
            _window = topLevel;
        }
    }
}
