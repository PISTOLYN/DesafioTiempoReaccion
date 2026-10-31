using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesafioTiempoReaccion;

public partial class MainWindow : Window
{
    enum St { Idle, Wait, Go, Finished }

    static readonly string[] Names = { "ROJO", "AZUL" };
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly Brush BoxNormal = new SolidColorBrush(Color.FromRgb(0x06, 0x14, 0x3A));
    static readonly Brush BoxGo = new SolidColorBrush(Color.FromRgb(0x0B, 0x7A, 0x2A));
    static readonly Brush BoxFalse = new SolidColorBrush(Color.FromRgb(0x8A, 0x10, 0x10));

    readonly Settings cfg;
    readonly Stopwatch sw = new();
    readonly DispatcherTimer tick = new() { Interval = TimeSpan.FromMilliseconds(10) };
    readonly DispatcherTimer waitT = new();
    readonly DispatcherTimer clockTick = new() { Interval = TimeSpan.FromMilliseconds(30) }; // Para la manecilla
    readonly Random rnd = new();
    readonly List<MediaPlayer> players = new();
    readonly TextBlock[] disp, status, scoreTb;

    SpeechSynthesizer synth;
    St st = St.Idle;
    readonly int[] score = new int[2];
    readonly double?[] t = new double?[2];
    readonly int[] cnt = new int[2];
    string capturing;
    string page = "main";

    // Tiempo Objetivo (Modificable con flechas)
    double targetTime = 10.00;

    public MainWindow()
    {
        InitializeComponent();
        cfg = Settings.Load();
        disp = new[] { DispRojo, DispAzul };
        status = new[] { StatusRojo, StatusAzul };
        scoreTb = new[] { ScoreRojo, ScoreAzul };

        tick.Tick += OnTick;
        waitT.Tick += OnWaitDone;
        clockTick.Tick += ClockTick_Tick;

        try
        {
            synth = new SpeechSynthesizer();
            try { synth.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new CultureInfo("es-MX")); }
            catch { }
        }
        catch { synth = null; }

        try
        {
            Directory.CreateDirectory(Settings.SoundDir);
            var readme = Path.Combine(Settings.SoundDir, "LEEME.txt");
            if (!File.Exists(readme))
                File.WriteAllText(readme,
                    "Coloca aquí tus sonidos (mp3 o wav) con estos nombres:\r\n" +
                    "inicio, senal, ganador, error\r\n" +
                    "Ejemplo: senal.mp3\r\nSi no hay archivo se usa un sonido de Windows.");
        }
        catch { }

        ApplyMode();
        ApplyFull();
        Loaded += (_, _) => Focus();
    }

    // ---------- Utilidades ----------
    Key K(string id) => Enum.TryParse(cfg.Keys[id], out Key k) ? k : Key.None;
    string Fmt(double s) => Math.Max(0, Math.Min(s, 99.99)).ToString("00.00", Inv);
    string Zero() => cfg.Mode == 2 ? "0000" : "00.00";

    void Play(string name)
    {
        try
        {
            foreach (var ext in new[] { ".mp3", ".wav" })
            {
                var p = Path.Combine(Settings.SoundDir, name + ext);
                if (!File.Exists(p)) continue;
                var mp = new MediaPlayer { Volume = cfg.Volume };
                mp.MediaEnded += (_, _) => players.Remove(mp);
                players.Add(mp);
                mp.Open(new Uri(p));
                mp.Play();
                return;
            }
            if (name == "senal") SystemSounds.Exclamation.Play();
            else if (name == "ganador") SystemSounds.Asterisk.Play();
            else if (name == "error") SystemSounds.Hand.Play();
        }
        catch { }
    }

    void Speak(string text)
    {
        if (!cfg.Voice || synth == null) return;
        try
        {
            synth.Volume = (int)(cfg.Volume * 100);
            synth.SpeakAsyncCancelAll();
            synth.SpeakAsync(text.Replace("¡", "").Replace("!", ""));
        }
        catch { }
    }

    void UpdScores()
    {
        for (int i = 0; i < 2; i++) scoreTb[i].Text = score[i].ToString();
    }

    // ---------- Modo / pantalla ----------
    void ApplyMode()
    {
        var n = cfg.ModeNames[cfg.Mode];
        LblModo.Text = n; ModoTxt.Text = n; TitleModo.Text = n;
        Reset(false);
        cfg.Save();
    }

    void ChangeMode(int d)
    {
        if (st != St.Idle) return;
        cfg.Mode = (cfg.Mode + d + 3) % 3;
        ApplyMode();
    }

    void ApplyFull()
    {
        if (cfg.FullScreen) { WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; }
        else { WindowStyle = WindowStyle.SingleBorderWindow; WindowState = WindowState.Normal; }
    }

    void ModoPrev_Click(object s, RoutedEventArgs e) => ChangeMode(-1);
    void ModoNext_Click(object s, RoutedEventArgs e) => ChangeMode(1);

    // ---------- Lógica de juego ----------
    void Reset(bool clearScores)
    {
        tick.Stop(); waitT.Stop(); clockTick.Stop(); sw.Reset();
        st = St.Idle;
        t[0] = t[1] = null; cnt[0] = cnt[1] = 0;
        for (int i = 0; i < 2; i++) { disp[i].Text = Zero(); status[i].Text = cfg.TxtBoton; }
        DispCentro.Text = Fmt(targetTime);
        CenterBox.Background = BoxNormal;
        MsgCentro.Text = cfg.ModeHelp[cfg.Mode] + "\n\n" + cfg.TxtListo;
        if (clearScores) { score[0] = score[1] = 0; UpdScores(); }
        HandRotation.Angle = 0;
    }

    void StartRound()
    {
        if (st != St.Idle) return;
        Reset(false);
        Play("inicio");
        st = St.Wait;
        MsgCentro.Text = "ESPERA LA SEÑAL...";
        Speak("3");
        
        // Conteo regresivo simple
        var countdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        int count = 2;
        countdown.Tick += (s, e) =>
        {
            if (count > 0)
            {
                Speak(count.ToString());
                count--;
            }
            else if (count == 0)
            {
                Speak("Ahora");
                countdown.Stop();
                st = St.Go;
                MsgCentro.Text = cfg.TxtYa;
                CenterBox.Background = BoxGo;
                Play("senal");
                sw.Restart(); tick.Start(); clockTick.Start();
            }
        };
        countdown.Start();
    }

    // Animación de la manecilla
    void ClockTick_Tick(object s, EventArgs e)
    {
        if (st == St.Go)
        {
            HandRotation.Angle = (HandRotation.Angle + 10) % 360;
        }
    }

    void OnWaitDone(object s, EventArgs e)
    {
        // Ya no se usa este método para el conteo, pero lo dejamos por compatibilidad
    }

    void OnTick(object s, EventArgs e)
    {
        double el = sw.Elapsed.TotalSeconds;
        
        // Muestra el tiempo transcurrido en el centro
        DispCentro.Text = Fmt(el);

        // Modo 0: Tiempo de reacción clásico (el primero que presiona gana)
        if (cfg.Mode == 0)
        {
            if (el >= 10) Finish(-1, cfg.TxtNadie);
            return;
        }

        // Modo 1: Detener el tiempo exacto
        if (cfg.Mode == 1)
        {
            if (el >= 99.99) { Resolve1(); } // Límite de seguridad
        }
        
        // Modo 2: Contar clics
        if (cfg.Mode == 2)
        {
             if (el >= 10) { Resolve2(); }
        }
    }

    void Press(int team)
    {
        if (st == St.Idle) return;

        // Modo 0: Reacción rápida
        if (cfg.Mode == 0)
        {
            if (st == St.Wait) // Presionó antes de tiempo
            {
                CenterBox.Background = BoxFalse;
                Finish(1 - team, $"¡FALSO! {Names[1 - team]} GANA");
            }
            else if (st == St.Go) // Presionó a tiempo
            {
                double el = sw.Elapsed.TotalSeconds;
                disp[team].Text = Fmt(el);
                Finish(team, string.Format(cfg.TxtGana, Names[team]));
            }
        }
        // Modo 1: Detener el tiempo exacto
        else if (cfg.Mode == 1)
        {
            if (st != St.Go || t[team] != null) return;
            
            double el = sw.Elapsed.TotalSeconds;
            t[team] = el;
            disp[team].Text = Fmt(el);
            
            if (t[0] != null && t[1] != null) Resolve1();
        }
        // Modo 2: Contar clics
        else if (cfg.Mode == 2)
        {
            if (st != St.Go) return;
            cnt[team]++;
            disp[team].Text = cnt[team].ToString("0000");
        }
    }

    // Regla estricta para Modo 1 (Solo gana si es exacto)
    void Resolve1()
    {
        tick.Stop(); clockTick.Stop();
        st = St.Finished;
        
        if (t[0] == null && t[1] == null) { Finish(-1, "NADIE PRESIONÓ"); return; }
        
        // Margen de error: 0.01 segundos (10 ms)
        double margin = 0.01;
        
        bool redExact = t[0].HasValue && Math.Abs(t[0].Value - targetTime) <= margin;
        bool blueExact = t[1].HasValue && Math.Abs(t[1].Value - targetTime) <= margin;

        if (redExact && blueExact) Finish(-1, "¡EMPATE EXACTO!");
        else if (redExact) Finish(0, "¡GANÓ ROJO! TIEMPO EXACTO");
        else if (blueExact) Finish(1, "¡GANÓ AZUL! TIEMPO EXACTO");
        else Finish(-1, "NADIE ACERTÓ EL TIEMPO EXACTO");
    }

    void Resolve2()
    {
        tick.Stop(); clockTick.Stop();
        st = St.Finished;
        if (cnt[0] == cnt[1]) { Finish(-1, cfg.TxtEmpate); return; }
        int w = cnt[0] > cnt[1] ? 0 : 1;
        Finish(w, string.Format(cfg.TxtGana, Names[w]));
    }

    void Finish(int winner, string msg)
    {
        st = St.Idle;
        tick.Stop(); waitT.Stop(); clockTick.Stop(); sw.Stop();
        
        if (winner >= 0)
        {
            score[winner]++; UpdScores();
            status[winner].Text = cfg.TxtGanador;
            Play("ganador");
        }
        else Play("error");
        
        MsgCentro.Text = msg;
        Speak(msg);
    }

    void BtnRojo_Down(object s, MouseButtonEventArgs e) { Press(0); }
    void BtnAzul_Down(object s, MouseButtonEventArgs e) { Press(1); }

    void AddScore(int team, int d)
    {
        score[team] = Math.Max(0, score[team] + d);
        UpdScores();
    }

    // ---------- Teclado ----------
    void Window_PreviewKeyDown(object s, KeyEventArgs e)
    {
        var k = e.Key == Key.System ? e.SystemKey : e.Key;
        if (e.IsRepeat) { e.Handled = true; return; }

        if (capturing != null)
        {
            if (k != Key.Escape) { cfg.Keys[capturing] = k.ToString(); cfg.Save(); }
            capturing = null;
            BuildMenu();
            e.Handled = true;
            return;
        }

        // Cambiar Tiempo Objetivo con Flechas (Solo en Idle)
        if (st == St.Idle)
        {
            if (k == Key.Right) { targetTime += 5.00; if (targetTime > 99.00) targetTime = 99.00; DispCentro.Text = Fmt(targetTime); e.Handled = true; return; }
            if (k == Key.Left) { targetTime -= 5.00; if (targetTime < 5.00) targetTime = 5.00; DispCentro.Text = Fmt(targetTime); e.Handled = true; return; }
        }

        if (k == K("Menu")) { ToggleMenu(); e.Handled = true; return; }

        if (MenuOverlay.Visibility == Visibility.Visible)
        {
            if (k == Key.Escape) { ToggleMenu(); e.Handled = true; }
            return;
        }

        if (k == K("Pantalla")) { cfg.FullScreen = !cfg.FullScreen; ApplyFull(); cfg.Save(); }
        else if (k == K("Iniciar")) StartRound();
        else if (k == K("Reiniciar")) Reset(true);
        else if (k == K("Rojo")) Press(0);
        else if (k == K("Azul")) Press(1);
        else if (k == K("RojoMas")) AddScore(0, 1);
        else if (k == K("RojoMenos")) AddScore(0, -1);
        else if (k == K("AzulMas")) AddScore(1, 1);
        else if (k == K("AzulMenos")) AddScore(1, -1);
        else if (k == Key.Escape && cfg.FullScreen) { cfg.FullScreen = false; ApplyFull(); cfg.Save(); }
        else return;
        e.Handled = true;
    }

    // ---------- Menú ----------
    void ToggleMenu()
    {
        if (MenuOverlay.Visibility == Visibility.Visible)
        {
            MenuOverlay.Visibility = Visibility.Collapsed;
            Focus();
        }
        else
        {
            page = "main";
            BuildMenu();
            MenuOverlay.Visibility = Visibility.Visible;
        }
    }

    Button Mk(string text, Action a)
    {
        var b = new Button { Content = text, Style = (Style)FindResource("MenuBtn") };
        b.Click += (_, _) => a();
        return b;
    }

    void BuildMenu()
    {
        MenuStack.Children.Clear();
        if (page == "keys")
        {
            MenuTitle.Text = "CONFIGURAR TECLADO";
            var items = new (string id, string label)[]
            {
                ("Rojo", "Botón equipo ROJO"), ("Azul", "Botón equipo AZUL"),
                ("Iniciar", "Iniciar ronda"), ("Reiniciar", "Reiniciar marcadores"),
                ("RojoMas", "Punto ROJO  +1"), ("RojoMenos", "Punto ROJO  -1"),
                ("AzulMas", "Punto AZUL  +1"), ("AzulMenos", "Punto AZUL  -1"),
                ("Menu", "Abrir/cerrar menú"), ("Pantalla", "Pantalla completa")
            };
            foreach (var (id, label) in items)
            {
                var txt = capturing == id ? "Pulsa una tecla…" : cfg.Keys[id];
                var cid = id;
                MenuStack.Children.Add(Mk($"{label}:  {txt}", () => { capturing = cid; BuildMenu(); }));
            }
            MenuStack.Children.Add(Mk("Restablecer teclas", () => { cfg.Keys = Settings.Defaults(); cfg.Save(); BuildMenu(); }));
            MenuStack.Children.Add(Mk("← Volver", () => { capturing = null; page = "main"; BuildMenu(); }));
            return;
        }

        MenuTitle.Text = "MENÚ";
        MenuStack.Children.Add(Mk($"1. Modo de juego: {cfg.ModeNames[cfg.Mode]}", () => { ChangeMode(1); BuildMenu(); }));
        MenuStack.Children.Add(Mk("2. Configurar teclado", () => { page = "keys"; BuildMenu(); }));
        MenuStack.Children.Add(Mk("3. Sonidos (abrir carpeta mp3/wav)", () =>
            Process.Start(new ProcessStartInfo("explorer.exe", Settings.SoundDir) { UseShellExecute = true })));
        MenuStack.Children.Add(Mk("4. Editar textos (config.json)", () =>
            Process.Start(new ProcessStartInfo("notepad.exe", Settings.FilePath) { UseShellExecute = true })));
        MenuStack.Children.Add(Mk("    ↻ Recargar textos tras editar", () => ReloadTexts()));
        MenuStack.Children.Add(Mk($"5. Volumen: {(int)Math.Round(cfg.Volume * 100)}%", () =>
        {
            cfg.Volume = cfg.Volume >= 0.99 ? 0.0 : Math.Round(cfg.Volume + 0.1, 1);
            cfg.Save(); BuildMenu();
        }));
        MenuStack.Children.Add(Mk($"6. Voz: {(cfg.Voice ? "SÍ" : "NO")}", () => { cfg.Voice = !cfg.Voice; cfg.Save(); BuildMenu(); }));
        MenuStack.Children.Add(Mk("7. Pantalla completa", () => { cfg.FullScreen = !cfg.FullScreen; ApplyFull(); cfg.Save(); }));
        MenuStack.Children.Add(Mk("8. Reiniciar marcadores", () => { Reset(true); }));
        MenuStack.Children.Add(Mk("9. Cerrar menú", ToggleMenu));
        MenuStack.Children.Add(Mk("10. Apagar PC", () =>
        {
            if (MessageBox.Show("¿Apagar la computadora?", "Apagar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo("shutdown", "/s /t 5") { CreateNoWindow = true, UseShellExecute = false });
        }));
        MenuStack.Children.Add(Mk("11. Cerrar programa", Close));
    }

    void ReloadTexts()
    {
        var n = Settings.Load();
        foreach (var p in typeof(Settings).GetProperties())
            if (p.CanWrite) p.SetValue(cfg, p.GetValue(n));
        ApplyMode();
        BuildMenu();
    }

    protected override void OnClosed(EventArgs e)
    {
        try { synth?.Dispose(); } catch { }
        base.OnClosed(e);
    }
}
