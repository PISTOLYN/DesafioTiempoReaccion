using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DesafioTiempoReaccion;

/// <summary>Configuración editable. Se guarda en %AppData%\DesafioTiempoReaccion\config.json</summary>
public class Settings
{
    public int Mode { get; set; } = 0;
    public double Volume { get; set; } = 0.8;
    public bool Voice { get; set; } = true;
    public bool FullScreen { get; set; } = false;

    public string[] ModeNames { get; set; } = { "TIEMPO DE REACCIÓN", "CUENTA REGRESIVA", "BOTÓN RÁPIDO" };
    public string[] ModeHelp { get; set; } =
    {
        "ESPERA LA SEÑAL\nY PRESIONA EL BOTÓN\nLO MÁS RÁPIDO POSIBLE",
        "DETÉN TU CRONÓMETRO\nLO MÁS CERCA DE 00.00\nSIN PASARTE",
        "PRESIONA TU BOTÓN\nLAS VECES QUE PUEDAS\nEN 10 SEGUNDOS"
    };

    // Textos editables ({0} = nombre del equipo)
    public string TxtListo { get; set; } = "PULSA ESPACIO PARA COMENZAR";
    public string TxtPrepara { get; set; } = "PREPARADOS...";
    public string TxtEspera { get; set; } = "ESPERA LA SEÑAL...";
    public string TxtYa { get; set; } = "¡AHORA!";
    public string TxtGana { get; set; } = "¡GANA EL EQUIPO {0}!";
    public string TxtFalso { get; set; } = "SALIDA EN FALSO. Gana {0}";
    public string TxtEmpate { get; set; } = "¡EMPATE!";
    public string TxtNadie { get; set; } = "NADIE PULSÓ";
    public string TxtBoton { get; set; } = "PRESIONA EL BOTÓN";
    public string TxtGanador { get; set; } = "¡GANADOR!";

    // Teclas editables desde el menú (tecla M)
    public Dictionary<string, string> Keys { get; set; } = Defaults();

    public static Dictionary<string, string> Defaults() => new()
    {
        ["Rojo"] = "A", ["Azul"] = "L", ["Iniciar"] = "Space", ["Reiniciar"] = "R",
        ["RojoMas"] = "Q", ["RojoMenos"] = "Z", ["AzulMas"] = "P", ["AzulMenos"] = "N",
        ["Menu"] = "M", ["Pantalla"] = "F11"
    };

    public static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesafioTiempoReaccion");
    public static string SoundDir => Path.Combine(Dir, "sonidos");
    public static string FilePath => Path.Combine(Dir, "config.json");

    static readonly JsonSerializerOptions Opt = new() { WriteIndented = true };

    public static Settings Load()
    {
        Settings s;
        try
        {
            s = File.Exists(FilePath)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings()
                : new Settings();
        }
        catch { s = new Settings(); }

        foreach (var kv in Defaults())
            if (!s.Keys.ContainsKey(kv.Key)) s.Keys[kv.Key] = kv.Value;
        if (s.ModeNames == null || s.ModeNames.Length < 3) s.ModeNames = new Settings().ModeNames;
        if (s.ModeHelp == null || s.ModeHelp.Length < 3) s.ModeHelp = new Settings().ModeHelp;
        if (s.Mode < 0 || s.Mode > 2) s.Mode = 0;
        s.Save();
        return s;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Opt));
        }
        catch { }
    }
}
