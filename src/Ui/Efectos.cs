using System.IO;
using System.Windows.Media;

namespace DesafioTiempoReaccion.Ui;

/// <summary>
/// Efectos de sonido. Cada efecto (cuenta, ahora, detener, ganador) tiene un sonido por defecto
/// (se genera como .wav la primera vez) que se puede reemplazar por cualquier MP3/WAV.
/// </summary>
public sealed class Efectos
{
    private readonly List<MediaPlayer> _vivos = new();

    public static string Carpeta => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesafioTiempoReaccion", "sonidos");

    public static string RutaPorDefecto(string slot) => System.IO.Path.Combine(Carpeta, slot + ".wav");

    public Efectos()
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            Crear("cuenta", new (double, double)[] { (660, 160) });
            Crear("ahora", new (double, double)[] { (880, 120), (1320, 450) });
            Crear("detener", new (double, double)[] { (1000, 70), (750, 120) });
            Crear("ganador", new (double, double)[] { (523, 150), (659, 150), (784, 150), (1047, 450) });
        }
        catch { }
    }

    public void Reproducir(string slot, string? personalizado)
    {
        string ruta = !string.IsNullOrWhiteSpace(personalizado) && File.Exists(personalizado)
            ? personalizado
            : RutaPorDefecto(slot);
        if (!File.Exists(ruta)) return;

        try
        {
            var p = new MediaPlayer();
            _vivos.Add(p);
            void Fin(object? s, EventArgs e) { try { p.Close(); } catch { } _vivos.Remove(p); }
            p.MediaEnded += Fin;
            p.MediaFailed += (s, e) => Fin(s, EventArgs.Empty);
            p.Open(new Uri(ruta));
            p.Volume = 1.0;
            p.Play();
        }
        catch { }
    }

    private static void Crear(string slot, (double hz, double ms)[] notas)
    {
        string path = RutaPorDefecto(slot);
        if (File.Exists(path)) return;

        const int sr = 44100;
        var muestras = new List<short>();
        foreach (var (hz, ms) in notas)
        {
            int n = (int)(sr * ms / 1000.0);
            for (int i = 0; i < n; i++)
            {
                double env = Math.Min(1.0, Math.Min(i, n - i) / (sr * 0.01));   // fade para evitar "clics"
                muestras.Add((short)(Math.Sin(2 * Math.PI * hz * i / sr) * 0.5 * env * short.MaxValue));
            }
        }

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        int datos = muestras.Count * 2;
        bw.Write(new[] { 'R', 'I', 'F', 'F' });
        bw.Write(36 + datos);
        bw.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
        bw.Write(16);
        bw.Write((short)1);          // PCM
        bw.Write((short)1);          // mono
        bw.Write(sr);
        bw.Write(sr * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(new[] { 'd', 'a', 't', 'a' });
        bw.Write(datos);
        foreach (var m in muestras) bw.Write(m);
    }
}
