using System.IO;
using System.Windows.Media;

namespace DesafioTiempoReaccion.Ui;

/// <summary>Música de fondo (MP3) que suena en bucle durante la carrera.</summary>
public sealed class Musica
{
    private readonly MediaPlayer _p = new();

    public Musica()
    {
        _p.MediaEnded += (_, _) => { _p.Position = TimeSpan.Zero; _p.Play(); };
    }

    public void Reproducir(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta)) return;
        try
        {
            _p.Open(new Uri(ruta));
            _p.Volume = 0.8;
            _p.Play();
        }
        catch { }
    }

    public void Detener()
    {
        try { _p.Stop(); _p.Close(); } catch { }
    }
}
