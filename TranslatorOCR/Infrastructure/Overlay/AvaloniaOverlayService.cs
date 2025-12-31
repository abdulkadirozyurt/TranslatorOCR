using System;
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
        private bool _locked;
        private bool _dragging;
        private PixelPoint _dragStartPointer;
        private PixelPoint _windowStart;
        private bool _wasVisibleBeforeTempHide;

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

                UpdateSizeAndPosition();
                _window?.Show();
            });

            return Task.CompletedTask;
        }

        public Task TempHideAsync(CancellationToken cancellationToken)
        {
            if (_window == null) return Task.CompletedTask;

            Dispatcher.UIThread.Post(() =>
            {
                _wasVisibleBeforeTempHide = _window.IsVisible;
                if (_wasVisibleBeforeTempHide)
                    _window.Hide();
            });

            return Task.CompletedTask;
        }

        public Task TempShowAsync(CancellationToken cancellationToken)
        {
            if (_window == null) return Task.CompletedTask;

            Dispatcher.UIThread.Post(() =>
            {
                if (_wasVisibleBeforeTempHide && _window != null)
                    _window.Show();
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

            // Dragging
            _window.PointerPressed += (s, e) =>
            {
                if (_locked) return;
                var p = e.GetCurrentPoint(_window).Position;
                _dragStartPointer = new PixelPoint((int)p.X, (int)p.Y);
                _windowStart = _window.Position;
                _dragging = true;
                e.Pointer.Capture(_window);
            };

            _window.PointerMoved += (s, e) =>
            {
                if (!_dragging || _locked) return;
                var p = e.GetCurrentPoint(_window).Position;
                var current = new PixelPoint((int)p.X, (int)p.Y);
                var dx = current.X - _dragStartPointer.X;
                var dy = current.Y - _dragStartPointer.Y;
                var newPos = new PixelPoint(_windowStart.X + dx, _windowStart.Y + dy);
                _window.Position = newPos;
            };

            _window.PointerReleased += (s, e) =>
            {
                if (_dragging)
                {
                    _dragging = false;
                    try { e.Pointer.Capture(null); } catch { }
                }
            };

            // Double-click toggles lock
            _window.DoubleTapped += (s, e) =>
            {
                _locked = !_locked;
                if (_locked)
                {
                    // optionally change style when locked
                    border.Background = new SolidColorBrush(Color.Parse("#99000000"));
                }
                else
                {
                    border.Background = new SolidColorBrush(Color.Parse("#66000000"));
                }
            };
        }

        private void UpdateSizeAndPosition()
        {
            if (_window == null || _textBlock == null) return;
            // Measure desired size
            _textBlock.Measure(Size.Infinity);
            var desired = _textBlock.DesiredSize;
            var width = Math.Max(300, desired.Width + 40);
            var height = Math.Max(50, desired.Height + 30);
            _window.Width = width;
            _window.Height = height;
            // keep within screen bounds
            var screen = _window.Screens.Primary.WorkingArea;
            var x = Math.Clamp(_window.Position.X, 0, (int)(screen.Width - width));
            var y = Math.Clamp(_window.Position.Y, 0, (int)(screen.Height - height));
            _window.Position = new PixelPoint(x, y);
        }
    }
}
