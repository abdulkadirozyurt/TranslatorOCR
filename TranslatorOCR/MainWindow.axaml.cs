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
            BrowseTessdataButton.Click += BrowseTessdataButton_Click;

            if (App.Services != null)
            {
                _controller = App.Services.GetService<AppController>();
                _settings = App.Services.GetService<ISettingsService>();

                if (_settings != null)
                {
                    TessdataBox.Text = _settings.TessdataPath ?? string.Empty;
                    // try to set lang combo to settings if available
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
                }
            }

            SaveSettingsButton.Click += SaveSettingsButton_Click;
        }

        private async void BrowseTessdataButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog();
            var result = await dlg.ShowAsync(this);
            if (!string.IsNullOrWhiteSpace(result))
            {
                TessdataBox.Text = result;
                StatusLabel.Text = "Selected tessdata folder";
            }
        }

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

        private void SaveSettingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settings == null) return;
            var path = TessdataBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(path) && !System.IO.Directory.Exists(path))
            {
                StatusLabel.Text = "Tessdata path does not exist";
                return;
            }

            // Save OCR language setting from LangCombo selection (this represents OCR language configuration)
            var langItem = LangCombo.SelectedItem as ComboBoxItem;
            var lang = langItem?.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                // map UI language short codes to tessdata traineddata names when necessary
                var trained = lang switch
                {
                    "en" => "eng",
                    "tr" => "tur",
                    _ => lang
                };

                if (!string.IsNullOrWhiteSpace(path) && !System.IO.File.Exists(System.IO.Path.Combine(path, trained + ".traineddata")))
                {
                    StatusLabel.Text = $"Missing traineddata: {trained}.traineddata";
                    return;
                }

                _settings.OcrLanguage = lang;
            }

            _settings.TessdataPath = path;
            _settings.Save();
            StatusLabel.Text = "Settings saved";
        }
    }
}