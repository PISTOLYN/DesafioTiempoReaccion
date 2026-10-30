using System.IO;
using System.Text.Json;
namespace DesafioTiempoReaccion.GameEngine;

public class GameSettings
{
    public string TituloDesafio { get; set; } = "TIEMPO DE REACCIÓN";
    public int TiempoLimiteSegundos { get; set; } = 10;   // tiempo máximo para presionar tras la señal
    public string BotonRojo { get; set; } = "A";
    public string BotonAzul { get; set; } = "L";
    public string BotonReinicio { get; set; } = "R";

    public int CuentaRegresivaMs { get; set; } = 1500;
    public int EsperaMinMs { get; set; } = 2000;
    public int EsperaMaxMs { get; set; } = 5000;

    private static string FilePath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DesafioTiempoReaccion", "settings.json");

    public static GameSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(FilePath));
                if (s != null) { s.Normalize(); return s; }
            }
        }
        catch { /* archivo dañado: se usan los valores por defecto */ }
        return new GameSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* sin permisos de escritura: se ignora */ }
    }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(TituloDesafio)) TituloDesafio = "TIEMPO DE REACCIÓN";
        TiempoLimiteSegundos = Math.Clamp(TiempoLimiteSegundos, 3, 60);
        if (string.IsNullOrWhiteSpace(BotonRojo)) BotonRojo = "A";
        if (string.IsNullOrWhiteSpace(BotonAzul)) BotonAzul = "L";
        if (string.IsNullOrWhiteSpace(BotonReinicio)) BotonReinicio = "R";
        if (CuentaRegresivaMs < 0) CuentaRegresivaMs = 1500;
        if (EsperaMinMs < 0 || EsperaMaxMs < EsperaMinMs) { EsperaMinMs = 2000; EsperaMaxMs = 5000; }
    }
}
