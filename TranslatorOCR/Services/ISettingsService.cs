using System.Threading.Tasks;

namespace TranslatorOCR.Services;

public interface ISettingsService
{
    string? TessdataPath { get; set; }
    string? OcrLanguage { get; set; }

    void Load();
    void Save();
}
