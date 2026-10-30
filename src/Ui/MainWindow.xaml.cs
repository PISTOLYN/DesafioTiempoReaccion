using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using DesafioTiempoReaccion.GameEngine;

namespace DesafioTiempoReaccion.Ui;

public partial class MainWindow : Window
{
    private static readonly Color ColRojo = Color.FromRgb(0xFF, 0x3A, 0x3A);
    private static readonly Color ColAzul = Color.FromRgb(0x2F, 0x7B, 0xFF);
    private static readonly Color ColVerde = Color.FromRgb(0x1F, 0xD8, 0x5A);
    private static readonly Color ColOro = Color.FromRgb(0xFF, 0xD2, 0x1F);
    private static readonly Color ColCian = Color.FromRgb(0x3C, 0x9C, 0xFF);

    private static readonly Key[] TeclasReservadas = { Key.Space, Key.Enter, Key.Escape, Key.F1, Key.F11, Key.Tab, Key.System };

    private readonly GameSettings _settings = GameSettings.Load();
    private readonly GameEngine.GameEngine _engine;
    private readonly DispatcherTimer _timer;
    private bool _fullscreen;

    public MainWindow()
    {
        InitializeComponent();

        _engine = new GameEngine.GameEngine(_settings);
        _engine.StateChanged += OnStateChanged;
        _engine.RoundFinished += OnRoundFinished;
        _engine.TimedOut += OnTimedOut;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();

        AplicarConfiguracion();
        MostrarInicio();
    }

    // ------------------------------------------------------------------
    //  Eventos del motor (llegan siempre en el hilo de la interfaz)
    // ------------------------------------------------------------------
    private void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.CuentaRegresiva:
                ResetDisplays();
                SetBanner("¡PREPÁRATE!", "No presiones hasta ver la señal", ColOro, 46);
                break;
            case GameState.EsperandoSenal:
                SetBanner("ESPERA LA SEÑAL…", "Quien presione antes pierde el punto", ColCian, 40);
                break;
            case GameState.RondaActiva:
                SetBanner("¡AHORA!", "¡PRESIONA YA!", ColVerde, 88, 0xCC);
                System.Media.SystemSounds.Exclamation.Play();
                break;
        }
    }

    private void OnRoundFinished(ReactionRoundResult r)
    {
        TxtPuntosRojo.Text = _engine.PuntosRojo.ToString(CultureInfo.InvariantCulture);
        TxtPuntosAzul.Text = _engine.PuntosAzul.ToString(CultureInfo.InvariantCulture);

        bool rojo = r.Winner == Team.Rojo;
        string nombre = rojo ? "ROJO" : "AZUL";
        Color color = rojo ? ColRojo : ColAzul;

        if (r.FalseStart)
        {
            (r.Infractor == Team.Rojo ? DispRojo : DispAzul).Text = "--.--";
            DispCentral.Text = "--.--";
            SetBanner($"¡GANA EL EQUIPO {nombre}!",
                      $"Salida en falso del equipo {(rojo ? "azul" : "rojo")}\nESPACIO para otra ronda", color, 38, 0x88);
        }
        else
        {
            string t = FormatSeconds(r.ReactionTimeMs);
            (rojo ? DispRojo : DispAzul).Text = t;
            DispCentral.Text = t;
            SetBanner($"¡GANA EL EQUIPO {nombre}!",
                      $"{r.ReactionTimeMs:F0} ms\nESPACIO para otra ronda", color, 38, 0x88);
        }

        PulsarMarcador(r.Winner);
    }

    private void OnTimedOut()
    {
        SetBanner("TIEMPO AGOTADO", "Nadie presionó a tiempo\nESPACIO para otra ronda", ColOro, 40);
    }

    private void OnTick()
    {
        if (_engine.State != GameState.RondaActiva) return;

        double ms = _engine.ElapsedMs;
        DispCentral.Text = FormatSeconds(ms);
        ManoReloj.Angle = ms / (_settings.TiempoLimiteSegundos * 1000.0) * 360.0 % 360.0;
    }

    // ------------------------------------------------------------------
    //  Acciones
    // ------------------------------------------------------------------
    private void IniciarRonda() => _ = _engine.StartRoundAsync();

    private void Reiniciar()
    {
        _engine.ResetGame();
        TxtPuntosRojo.Text = "0";
        TxtPuntosAzul.Text = "0";
        ResetDisplays();
        MostrarInicio();
    }

    private void MostrarInicio() =>
        SetBanner("ESPERA LA SEÑAL Y PRESIONA EL BOTÓN LO MÁS RÁPIDO POSIBLE", "Pulsa ESPACIO para comenzar", ColCian, 28);

    private void ResetDisplays()
    {
        DispRojo.Text = DispAzul.Text = DispCentral.Text = "00.00";
        ManoReloj.Angle = 0;
    }

    private void SetBanner(string titulo, string sub, Color acento, double tamano, byte alfaFondo = 0x55)
    {
        TxtMensaje.Text = titulo;
        TxtMensaje.FontSize = tamano;
        TxtSub.Text = sub;
        CajaSenal.BorderBrush = new SolidColorBrush(acento);
        CajaSenal.Background = new SolidColorBrush(Color.FromArgb(alfaFondo, acento.R, acento.G, acento.B));
        CajaSenal.Effect = new DropShadowEffect { Color = acento, BlurRadius = 30, ShadowDepth = 0, Opacity = 0.8 };
    }

    private void PulsarMarcador(Team t)
    {
        var st = t == Team.Rojo ? ScaleMarcadorRojo : ScaleMarcadorAzul;
        var anim = new DoubleAnimation(1.0, 1.25, TimeSpan.FromMilliseconds(160))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(3)
        };
        st.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    private void VisualBoton(Team t, bool abajo)
    {
        var s = t == Team.Rojo ? ScaleBtnRojo : ScaleBtnAzul;
        s.ScaleX = s.ScaleY = abajo ? 0.93 : 1.0;
    }

    private static string FormatSeconds(double ms) =>
        Math.Min(ms / 1000.0, 99.99).ToString("00.00", CultureInfo.InvariantCulture);

    private static string FormatKey(string k)
    {
        if (k.Length == 2 && k[0] == 'D' && char.IsDigit(k[1])) return k[1].ToString();
        if (k.StartsWith("NumPad", StringComparison.Ordinal) && k.Length > 6) return k.Substring(6);
        return k.ToUpperInvariant();
    }

    private static void PonerMarcaDeTecla(TextBlock tb, string tecla)
    {
        string t = FormatKey(tecla);
        tb.Text = t;
        tb.FontSize = t.Length == 1 ? 120 : t.Length <= 3 ? 70 : 34;
    }

    private void AplicarConfiguracion()
    {
        string titulo = _settings.TituloDesafio.ToUpperInvariant();
        TxtTituloJuego.Text = titulo;
        TxtTituloInicio.Text = titulo;
        PonerMarcaDeTecla(TxtKeyRojo, _settings.BotonRojo);
        PonerMarcaDeTecla(TxtKeyAzul, _settings.BotonAzul);
    }

    // ------------------------------------------------------------------
    //  Teclado
    // ------------------------------------------------------------------
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Con la configuración abierta no se juega (así escribir "A", "L" o "R" no da puntos ni reinicia)
        if (PantallaConfig.Visibility == Visibility.Visible)
        {
            if (key == Key.Escape) { CerrarConfig(); e.Handled = true; }
            return;
        }

        if (PantallaInicio.Visibility == Visibility.Visible)
        {
            if (key == Key.Enter) { PantallaInicio.Visibility = Visibility.Collapsed; e.Handled = true; }
            else if (key == Key.F11) { if (!e.IsRepeat) ToggleFullscreen(); e.Handled = true; }
            return;
        }

        if (key == Key.F11 || (key == Key.Escape && _fullscreen))
        {
            if (!e.IsRepeat) ToggleFullscreen();
            e.Handled = true;
            return;
        }

        if (key == Key.F1)
        {
            if (!e.IsRepeat) AbrirConfig();
            e.Handled = true;
            return;
        }

        string name = key.ToString();

        if (name == _settings.BotonRojo)
        {
            // IsRepeat: mantener la tecla pulsada no cuenta como nueva pulsación
            if (!e.IsRepeat) { VisualBoton(Team.Rojo, true); _engine.RegisterPress(Team.Rojo); }
            e.Handled = true;
        }
        else if (name == _settings.BotonAzul)
        {
            if (!e.IsRepeat) { VisualBoton(Team.Azul, true); _engine.RegisterPress(Team.Azul); }
            e.Handled = true;
        }
        else if (name == _settings.BotonReinicio)
        {
            if (!e.IsRepeat) Reiniciar();
            e.Handled = true;
        }
        else if (key == Key.Space)
        {
            if (!e.IsRepeat) IniciarRonda();
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        string name = (e.Key == Key.System ? e.SystemKey : e.Key).ToString();
        if (name == _settings.BotonRojo) VisualBoton(Team.Rojo, false);
        if (name == _settings.BotonAzul) VisualBoton(Team.Azul, false);
    }

    private void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
        }
        _fullscreen = !_fullscreen;
    }

    // ------------------------------------------------------------------
    //  Botones de la pantalla
    // ------------------------------------------------------------------
    private void BtnIniciar_Click(object sender, RoutedEventArgs e) => IniciarRonda();
    private void BtnReiniciar_Click(object sender, RoutedEventArgs e) => Reiniciar();
    private void BtnConfig_Click(object sender, RoutedEventArgs e) => AbrirConfig();
    private void BtnCerrarConfig_Click(object sender, RoutedEventArgs e) => CerrarConfig();

    private void PantallaInicio_MouseDown(object sender, MouseButtonEventArgs e) =>
        PantallaInicio.Visibility = Visibility.Collapsed;

    // ------------------------------------------------------------------
    //  Configuración
    // ------------------------------------------------------------------
    private void AbrirConfig()
    {
        TxtCfgTitulo.Text = _settings.TituloDesafio;
        TxtCfgLimite.Text = _settings.TiempoLimiteSegundos.ToString(CultureInfo.InvariantCulture);
        KeyRojo.Text = _settings.BotonRojo;
        KeyAzul.Text = _settings.BotonAzul;
        KeyReinicio.Text = _settings.BotonReinicio;
        TxtCfgAviso.Text = "";
        PantallaConfig.Visibility = Visibility.Visible;
    }

    private void CerrarConfig()
    {
        string titulo = TxtCfgTitulo.Text.Trim();
        if (titulo.Length > 0) _settings.TituloDesafio = titulo;

        if (int.TryParse(TxtCfgLimite.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int lim))
            _settings.TiempoLimiteSegundos = Math.Clamp(lim, 3, 60);

        _settings.BotonRojo = KeyRojo.Text;
        _settings.BotonAzul = KeyAzul.Text;
        _settings.BotonReinicio = KeyReinicio.Text;
        _settings.Normalize();
        _settings.Save();

        AplicarConfiguracion();
        PantallaConfig.Visibility = Visibility.Collapsed;
        Focus();
    }

    private void KeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var box = (TextBox)sender;
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (Array.IndexOf(TeclasReservadas, key) >= 0)
        {
            TxtCfgAviso.Text = "Esa tecla está reservada (ESPACIO, ENTER, ESC, F1, F11, TAB). Elige otra.";
            return;
        }

        string name = key.ToString();
        foreach (var otra in new[] { KeyRojo, KeyAzul, KeyReinicio })
        {
            if (!ReferenceEquals(otra, box) && otra.Text == name)
            {
                TxtCfgAviso.Text = $"La tecla {FormatKey(name)} ya está asignada a otro botón.";
                return;
            }
        }

        box.Text = name;
        TxtCfgAviso.Text = "";
    }
}
