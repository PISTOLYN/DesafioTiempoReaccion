using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesafioTiempoReaccion.GameEngine;

namespace DesafioTiempoReaccion.Ui;

public partial class MainWindow : Window
{
    private readonly GameEngine.GameEngine _engine = new();
    private readonly GameSettings _settings = new();
    private readonly DispatcherTimer _timerReloj;
    private readonly Random _random = new();
    private double _anguloReloj = 0;

    public string TituloDesafio => _settings.TituloDesafio;
    public int PuntosRojo => _engine.PuntosRojo;
    public int PuntosAzul => _engine.PuntosAzul;
    public int TiempoLimiteSegundos => _settings.TiempoLimiteSegundos;
    public string BotonRojo => _settings.BotonRojo;
    public string BotonAzul => _settings.BotonAzul;
    public string BotonReinicio => _settings.BotonReinicio;

    private string _mensajeEstado = "Pulsa ESPACIO para comenzar";
    public string MensajeEstado
    {
        get => _mensajeEstado;
        set { _mensajeEstado = value; RefreshBindings(); }
    }

    private string _displayCentralText = "00.00";
    public string DisplayCentralText
    {
        get => _displayCentralText;
        set { _displayCentralText = value; RefreshBindings(); }
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        _engine.StateChanged += OnStateChanged;
        _engine.RoundFinished += OnRoundFinished;

        _timerReloj = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timerReloj.Tick += (_, _) =>
        {
            _anguloReloj = (_anguloReloj + 3) % 360;
            RotacionManecilla.Angle = _anguloReloj;
        };
        _timerReloj.Start();
    }

    private void OnStateChanged(GameState state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case GameState.CuentaRegresiva:
                    MensajeEstado = "PREPÁRATE...";
                    DisplayCentralText = "00.00";
                    break;
                case GameState.EsperandoSenal:
                    MensajeEstado = "ESPERA LA SEÑAL...";
                    break;
                case GameState.RondaActiva:
                    MensajeEstado = "¡AHORA!";
                    break;
                case GameState.Ganador:
                    MensajeEstado = "¡GANADOR!";
                    break;
            }
        });
    }

    private void OnRoundFinished(Team winner, double ms)
    {
        Dispatcher.Invoke(() =>
        {
            if (ms == -1)
            {
                MensajeEstado = $"SALIDA EN FALSO. Gana {(winner == Team.Rojo ? "ROJO" : "AZUL")}";
            }
            else
            {
                MensajeEstado = $"Gana {(winner == Team.Rojo ? "ROJO" : "AZUL")}: {ms:F0} ms";
                string strMs = (ms / 1000).ToString("00.00");
                DisplayCentralText = strMs;
                
                var targetBorder = winner == Team.Rojo ? FindName("MarcadorRojo") as Border : FindName("MarcadorAzul") as Border;
                if (targetBorder != null)
                {
                    var animacion = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.5,
                        Duration = TimeSpan.FromMilliseconds(200),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(3)
                    };
                    targetBorder.BeginAnimation(OpacityProperty, animacion);
                }
            }
            RefreshBindings();
        });
    }

    private async void IniciarRonda()
    {
        if (_engine.State != GameState.Inicio && _engine.State != GameState.Ganador) return;
        
        _engine.StartRound();
        await Task.Delay(2000);
        _engine.BeginWaitingSignal(_settings.TiempoLimiteSegundos * 1000);
        
        await Task.Delay(_random.Next(2000, 5000));
        if (_engine.State == GameState.EsperandoSenal) _engine.ShowSignal();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (PantallaInicio.Visibility == Visibility.Visible && e.Key == Key.Enter)
        {
            PantallaInicio.Visibility = Visibility.Collapsed;
            return;
        }

        if (e.Key.ToString() == _settings.BotonRojo) { _engine.RegisterPress(Team.Rojo); AnimarBoton(BtnRojoVisual); }
        else if (e.Key.ToString() == _settings.BotonAzul) { _engine.RegisterPress(Team.Azul); AnimarBoton(BtnAzulVisual); }
        else if (e.Key.ToString() == _settings.BotonReinicio) { BtnReiniciar_Click(null, null); }
        else if (e.Key == Key.Space && PantallaInicio.Visibility == Visibility.Collapsed) { IniciarRonda(); }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        BtnRojoVisual.RenderTransform = Transform.Identity;
        BtnAzulVisual.RenderTransform = Transform.Identity;
    }

    private void AnimarBoton(System.Windows.Shapes.Ellipse boton)
    {
        var scale = new ScaleTransform(0.9, 0.9);
        boton.RenderTransform = scale;
        boton.RenderTransformOrigin = new Point(0.5, 0.5);
    }

    private void BtnConfig_Click(object sender, RoutedEventArgs e) => PantallaConfig.Visibility = Visibility.Visible;
    private void BtnCerrarConfig_Click(object sender, RoutedEventArgs e) { PantallaConfig.Visibility = Visibility.Collapsed; RefreshBindings(); }
    
    private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
    {
        _engine.ResetGame();
        DisplayCentralText = "00.00";
        MensajeEstado = "Pulsa ESPACIO para comenzar";
        RefreshBindings();
    }

    private void RefreshBindings()
    {
        DataContext = null;
        DataContext = this;
    }
}
