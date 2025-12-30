using System.Threading;
using System.Threading.Tasks;

namespace TranslatorOCR.Services;

public interface IOverlayService
{
    Task ShowTextAsync(string text, CancellationToken cancellationToken);
    Task HideAsync(CancellationToken cancellationToken);
}
