using Avalonia.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TranslatorOCR.Application;
using TranslatorOCR.Models;
using TranslatorOCR.Services;

namespace TranslatorOCR
{
    public partial class MainWindow : Window
    {
        private AppController? _controller;
        private CancellationTokenSource? _cts;
        private Region? _region;
        private ISettingsService? _settings;

        public MainWindow()
        {
            InitializeComponent();

            SelectRegionButton.Click += SelectRegionButton_Click;
            StartStopButton.Click += StartStopButton_Click;

            if (App.Services != null)
            {
                _controller = App.Services.GetService<AppController>();
                _settings = App.Services.GetService<ISettingsService>();

                if (_settings != null)
                {
                    // set LangCombo based on settings if available
                    if (!string.IsNullOrWhiteSpace(_settings.OcrLanguage))
                    {
                        var items = LangCombo.Items;
                        if (items != null)
                        {
                            int idx = 0;
                            foreach (var it in items)
                            {
                                if (it is ComboBoxItem cbi && cbi.Content?.ToString() == _settings.OcrLanguage)
                                {
                                    LangCombo.SelectedIndex = idx;
                                    break;
                                }
                                idx++;
                            }
                        }
                    }

                    // show resolved tessdata path (effective path used by TesseractOcrService)
                    var resolved = _settings.TessdataPath ?? Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? System.IO.Path.Combine(AppContext.BaseDirectory, "tessdata");
                    TessdataPathLabel.Text = resolved;
                }
            }

            // no tessdata selection in UI anymore
            // Key bindings: F1=SelectRegion, F2=Start/Stop, F3=Exit
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.F1)
            {
                SelectRegionButton_Click(this, null);
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.F2)
            {
                StartStopButton_Click(this, null);
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.F3)
            {
                // exit application
                var lifetime = (App.Current.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime);
                lifetime?.Shutdown();
                e.Handled = true;
            }
        }

        // tessdata selection removed — tessdata is loaded from AppContext.BaseDirectory/tessdata or TESSDATA_PREFIX

        private void SelectRegionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var selector = new RegionSelectorWindow();
            selector.RegionSelected += r =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (r != null)
                    {
                        _region = r;
                        RegionLabel.Text = $"{_region.X}, {_region.Y}, {_region.Width}x{_region.Height}";
                    }
                    else
                    {
                        RegionLabel.Text = "Not selected";
                    }
                });
            };

            selector.ShowCenteredTopMost();
        }

        private async void StartStopButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_controller == null)
            {
                StatusLabel.Text = "Controller not available";
                return;
            }

            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
                StartStopButton.Content = "Start";
                StatusLabel.Text = "Stopped";
                return;
            }

            if (_region == null)
            {
                StatusLabel.Text = "Select a region first";
                return;
            }

            if (!double.TryParse(IntervalBox.Text, out var seconds) || seconds <= 0)
                seconds = 0.5;

            var langItem = LangCombo.SelectedItem as ComboBoxItem;
            var lang = langItem?.Content?.ToString() ?? "en";

            _cts = new CancellationTokenSource();
            StartStopButton.Content = "Stop";
            StatusLabel.Text = "Running";

            try
            {
                await Task.Run(() => _controller.StartLoopAsync(_region, TimeSpan.FromSeconds(seconds), lang, _cts.Token));
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _cts = null;
                StartStopButton.Content = "Start";
                StatusLabel.Text = "Stopped";
            }
        }

        // settings saved via settings service elsewhere; tessdata path is not user-editable in UI
    }
}