using System.Globalization;
using System.Speech.Synthesis;

namespace DesafioTiempoReaccion.Ui;

/// <summary>Voz del sistema (Windows). Si no hay voz disponible, simplemente no suena.</summary>
public sealed class Voz : IDisposable
{
    private SpeechSynthesizer? _s;

    public Voz()
    {
        try
        {
            _s = new SpeechSynthesizer();
            foreach (var cultura in new[] { "es-MX", "es-ES", "es-US" })
            {
                try { _s.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new CultureInfo(cultura)); break; }
                catch { /* esa voz no está instalada: probar la siguiente */ }
            }
        }
        catch { _s = null; }
    }

    public void Decir(string texto)
    {
        try
        {
            _s?.SpeakAsyncCancelAll();
            _s?.SpeakAsync(texto);
        }
        catch { }
    }

    public void Dispose()
    {
        try { _s?.Dispose(); } catch { }
    }
}
