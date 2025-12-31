using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using TranslatorOCR.Models;

namespace TranslatorOCR
{
    /// <summary>
    /// Fullscreen region selector window matching Python game_translator_v7.py behavior.
    /// Covers entire screen including system UI (Live Captions, taskbar, etc.).
    /// </summary>
    public partial class RegionSelectorWindow : Window
    {
        private Point? _startScreen;  // Screen coordinates (like Python winfo_pointerx/y)
        private Point? _startLocal;   // Local window coordinates for drawing
        public event Action<Region?>? RegionSelected;

        public RegionSelectorWindow()
        {
            InitializeComponent();
            
            // Event handlers
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;
            this.KeyDown += OnKeyDown;
            this.Opened += OnOpened;
            
            // Critical: Use FullScreen (not Maximized) to cover system UI like Live Captions
            // This matches Python's root.attributes('-fullscreen', True)
            this.WindowState = WindowState.FullScreen;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            
            // Crosshair cursor like Python's cursor="cross"
            this.Cursor = new Cursor(StandardCursorType.Cross);
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            // Center the instruction label horizontally
            if (InstructionLabel != null && RootCanvas != null)
            {
                var screenWidth = this.Bounds.Width;
                Canvas.SetLeft(InstructionLabel, (screenWidth - 300) / 2);
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RegionSelected?.Invoke(null);
                Close();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Get both screen and local coordinates
            // Screen coords are used for the final Region (like Python's winfo_pointerx/y)
            // Local coords are used for drawing the rectangle
            var localPos = e.GetCurrentPoint(this).Position;
            var screenPos = this.PointToScreen(localPos);
            
            _startScreen = new Point(screenPos.X, screenPos.Y);
            _startLocal = localPos;
            
            SelectionRect.IsVisible = true;
            Canvas.SetLeft(SelectionRect, localPos.X);
            Canvas.SetTop(SelectionRect, localPos.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_startLocal == null) return;
            
            var p = e.GetCurrentPoint(this).Position;
            var x1 = Math.Min(_startLocal.Value.X, p.X);
            var y1 = Math.Min(_startLocal.Value.Y, p.Y);
            var x2 = Math.Max(_startLocal.Value.X, p.X);
            var y2 = Math.Max(_startLocal.Value.Y, p.Y);

            Canvas.SetLeft(SelectionRect, x1);
            Canvas.SetTop(SelectionRect, y1);
            SelectionRect.Width = x2 - x1;
            SelectionRect.Height = y2 - y1;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_startScreen == null)
            {
                RegionSelected?.Invoke(null);
                Close();
                return;
            }

            // Get end position in screen coordinates
            var localPos = e.GetCurrentPoint(this).Position;
            var screenPos = this.PointToScreen(localPos);
            var endScreen = new Point(screenPos.X, screenPos.Y);
            
            // Calculate region in screen coordinates (matching Python behavior)
            var x1 = (int)Math.Min(_startScreen.Value.X, endScreen.X);
            var y1 = (int)Math.Min(_startScreen.Value.Y, endScreen.Y);
            var x2 = (int)Math.Max(_startScreen.Value.X, endScreen.X);
            var y2 = (int)Math.Max(_startScreen.Value.Y, endScreen.Y);

            _startScreen = null;
            _startLocal = null;
            SelectionRect.IsVisible = false;

            // Minimum size check (Python uses 30x15, we use similar)
            if (x2 - x1 < 30 || y2 - y1 < 15)
            {
                RegionSelected?.Invoke(null);
            }
            else
            {
                // Return screen coordinates region
                RegionSelected?.Invoke(new Region(x1, y1, x2 - x1, y2 - y1));
            }

            Close();
        }

        /// <summary>
        /// Shows the selector window in fullscreen topmost mode.
        /// </summary>
        public void ShowFullScreenTopMost()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            this.Position = new PixelPoint(0, 0);
            this.WindowState = WindowState.FullScreen;
            this.Topmost = true;
            Show();
            this.Activate();  // Ensure window gets focus
        }
        
        /// <summary>
        /// Legacy method for compatibility.
        /// </summary>
        public void ShowCenteredTopMost() => ShowFullScreenTopMost();
    }
}
