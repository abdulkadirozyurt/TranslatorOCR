using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using TranslatorOCR.Models;

namespace TranslatorOCR
{
    public partial class RegionSelectorWindow : Window
    {
        private Point? _start;
        public event Action<Region?>? RegionSelected;

        public RegionSelectorWindow()
        {
            InitializeComponent();
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;
            this.KeyDown += RegionSelectorWindow_KeyDown;
        }

        private void RegionSelectorWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RegionSelected?.Invoke(null);
                Close();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var p = e.GetCurrentPoint(this).Position;
            _start = p;
            SelectionRect.IsVisible = true;
            Canvas.SetLeft(SelectionRect, p.X);
            Canvas.SetTop(SelectionRect, p.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_start == null) return;
            var p = e.GetCurrentPoint(this).Position;
            var x1 = Math.Min(_start.Value.X, p.X);
            var y1 = Math.Min(_start.Value.Y, p.Y);
            var x2 = Math.Max(_start.Value.X, p.X);
            var y2 = Math.Max(_start.Value.Y, p.Y);

            Canvas.SetLeft(SelectionRect, x1);
            Canvas.SetTop(SelectionRect, y1);
            SelectionRect.Width = x2 - x1;
            SelectionRect.Height = y2 - y1;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_start == null)
            {
                RegionSelected?.Invoke(null);
                Close();
                return;
            }

            var p = e.GetCurrentPoint(this).Position;
            var x1 = (int)Math.Min(_start.Value.X, p.X);
            var y1 = (int)Math.Min(_start.Value.Y, p.Y);
            var x2 = (int)Math.Max(_start.Value.X, p.X);
            var y2 = (int)Math.Max(_start.Value.Y, p.Y);

            _start = null;

            SelectionRect.IsVisible = false;

            // minimal size
            if (x2 - x1 < 10 || y2 - y1 < 8)
            {
                RegionSelected?.Invoke(null);
            }
            else
            {
                RegionSelected?.Invoke(new Region(x1, y1, x2 - x1, y2 - y1));
            }

            Close();
        }

        public void ShowCenteredTopMost()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Show();
        }
    }
}
