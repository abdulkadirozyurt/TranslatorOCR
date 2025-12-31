using System;
using System.IO;
using System.Text.Json;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Settings;

public class SettingsModel
{
    public string? TessdataPath { get; set; }
    public string? OcrLanguage { get; set; }
}

public class SettingsService : ISettingsService
{
    private readonly string _filePath;
    private SettingsModel _model = new SettingsModel();

    public SettingsService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TranslatorOCR");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    public string? TessdataPath
    {
        get => _model.TessdataPath;
        set => _model.TessdataPath = value;
    }

    public string? OcrLanguage
    {
        get => _model.OcrLanguage;
        set => _model.OcrLanguage = value;
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var txt = File.ReadAllText(_filePath);
            var m = JsonSerializer.Deserialize<SettingsModel>(txt);
            if (m != null) _model = m;
        }
        catch
        {
            // ignore — fallback to defaults
        }
    }

    public void Save()
    {
        try
        {
            var txt = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, txt);
        }
        catch
        {
            // ignore write errors for now
        }
    }
}
