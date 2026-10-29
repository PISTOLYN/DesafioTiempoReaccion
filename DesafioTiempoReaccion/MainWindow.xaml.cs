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
    enum St { Idle, Wait, Go }

    static readonly string[] Names = { "ROJO", "AZUL" };
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly Brush BoxNormal = new SolidColorBrush(Color.FromRgb(0x06, 0x14, 0x3A));
    static readonly Brush BoxGo = new SolidColorBrush(Color.FromRgb(0x0B, 0x7A, 0x2A));
    static readonly Brush BoxFalse = new SolidColorBrush(Color.FromRgb(0x8A, 0x10, 0x10));

    readonly Settings cfg;
    readonly Stopwatch sw = new();
    readonly DispatcherTimer tick = new() { Interval = TimeSpan.FromMilliseconds(10) };
    readonly DispatcherTimer waitT = new();
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

    public MainWindow()
    {
        InitializeComponent();
        cfg = Settings.Load();
        disp = new[] { DispRojo, DispAzul };
        status = new[] { StatusRojo, StatusAzul };
        scoreTb = new[] { ScoreRojo, ScoreAzul };

        tick.Tick += OnTick;
        waitT.Tick += OnWaitDone;

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
        tick.Stop(); waitT.Stop(); sw.Reset();
        st = St.Idle;
        t[0] = t[1] = null; cnt[0] = cnt[1] = 0;
        for (int i = 0; i < 2; i++) { disp[i].Text = Zero(); status[i].Text = cfg.TxtBoton; }
        DispCentro.Text = "10.00";
        CenterBox.Background = BoxNormal;
        MsgCentro.Text = cfg.ModeHelp[cfg.Mode] + "\n\n" + cfg.TxtListo;
        if (clearScores) { score[0] = score[1] = 0; UpdScores(); }
    }

    void StartRound()
    {
        if (st != St.Idle) return;
        Reset(false);
        Play("inicio");
        if (cfg.Mode == 0)
        {
            st = St.Wait;
            MsgCentro.Text = cfg.TxtEspera;
            Speak(cfg.TxtPrepara);
            waitT.Interval = TimeSpan.FromMilliseconds(rnd.Next(2000, 5500));
            waitT.Start();
        }
        else
        {
            st = St.Go;
            MsgCentro.Text = cfg.Mode == 1 ? "¡DETÉN EL TIEMPO!" : "¡PRESIONA SIN PARAR!";
            Play("senal");
            sw.Restart(); tick.Start();
        }
    }

    void OnWaitDone(object s, EventArgs e)
    {
        waitT.Stop();
        if (st != St.Wait) return;
        st = St.Go;
        MsgCentro.Text = cfg.TxtYa;
        CenterBox.Background = BoxGo;
        Play("senal");
        sw.Restart(); tick.Start();
    }

    void OnTick(object s, EventArgs e)
    {
        double el = sw.Elapsed.TotalSeconds;
        if (cfg.Mode == 0)
        {
            DispCentro.Text = Fmt(el);
            if (el >= 10) Finish(-1, cfg.TxtNadie);
            return;
        }
        double rem = 10 - el;
        if (rem <= 0)
        {
            DispCentro.Text = "00.00";
            if (cfg.Mode == 1) Resolve1(); else Resolve2();
        }
        else DispCentro.Text = Fmt(rem);
    }

    void Press(int team)
    {
        if (st == St.Idle) return;

        if (cfg.Mode == 0)
        {
            if (st == St.Wait)
            {
                waitT.Stop();
                CenterBox.Background = BoxFalse;
                Finish(1 - team, string.Format(cfg.TxtFalso, Names[1 - team]));
            }
            else
            {
                double el = sw.Elapsed.TotalSeconds;
                disp[team].Text = Fmt(el);
                Finish(team, string.Format(cfg.TxtGana, Names[team]));
            }
        }
        else if (cfg.Mode == 1)
        {
            if (t[team] != null) return;
            double rem = Math.Max(0, 10 - sw.Elapsed.TotalSeconds);
            t[team] = rem;
            disp[team].Text = Fmt(rem);
            if (t[0] != null && t[1] != null) Resolve1();
        }
        else
        {
            cnt[team]++;
            disp[team].Text = cnt[team].ToString("0000");
        }
    }

    void Resolve1()
    {
        if (t[0] == null && t[1] == null) { Finish(-1, cfg.TxtNadie); return; }
        int w;
        if (t[0] == null) w = 1;
        else if (t[1] == null) w = 0;
        else if (Math.Abs(t[0].Value - t[1].Value) < 0.005) { Finish(-1, cfg.TxtEmpate); return; }
        else w = t[0] < t[1] ? 0 : 1;
        Finish(w, string.Format(cfg.TxtGana, Names[w]));
    }

    void Resolve2()
    {
        if (cnt[0] == cnt[1]) { Finish(-1, cfg.TxtEmpate); return; }
        int w = cnt[0] > cnt[1] ? 0 : 1;
        Finish(w, string.Format(cfg.TxtGana, Names[w]));
    }

    void Finish(int winner, string msg)
    {
        st = St.Idle;
        tick.Stop(); waitT.Stop(); sw.Stop();
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
