using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Settings;

public class SettingsModel
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TessdataPath { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OcrLanguage { get; set; }
}

public class SettingsService : ISettingsService
{
    private readonly string _filePath;
    private SettingsModel _model = new SettingsModel();

    // Default values (not saved to file unless explicitly changed)
    private const string DefaultLanguage = "eng";

    public SettingsService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TranslatorOCR");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    public string? TessdataPath
    {
        get => string.IsNullOrWhiteSpace(_model.TessdataPath) ? null : _model.TessdataPath;
        set => _model.TessdataPath = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string? OcrLanguage
    {
        get => string.IsNullOrWhiteSpace(_model.OcrLanguage) ? DefaultLanguage : _model.OcrLanguage;
        set => _model.OcrLanguage = string.IsNullOrWhiteSpace(value) ? null : value;
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
            // Don't create file if no custom settings
            if (string.IsNullOrWhiteSpace(_model.TessdataPath) && string.IsNullOrWhiteSpace(_model.OcrLanguage))
            {
                // Delete file if it exists and has no custom settings
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var txt = JsonSerializer.Serialize(_model, options);
            File.WriteAllText(_filePath, txt);
        }
        catch
        {
            // ignore write errors for now
        }
    }
}
