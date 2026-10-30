using System.Windows;
using System.Windows.Controls;
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
    private readonly DispatcherTimer _timerCronometro;
    private readonly Random _random = new();
    private double _anguloReloj = 0;
    private int _tiempoObjetivoSegundos = 10; 
    private long _startTime = 0;

    // Variables para saber qué botón estamos configurando
    private string _modoCaptura = ""; 

    public string TituloDesafio => _settings.TituloDesafio;
    public int PuntosRojo => _engine.PuntosRojo;
    public int PuntosAzul => _engine.PuntosAzul;
    
    private string _mensajeEstado = "Pulsa ESPACIO para comenzar";
    public string MensajeEstado
    {
        get => _mensajeEstado;
        set { _mensajeEstado = value; RefreshBindings(); }
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

        _timerCronometro = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        _timerCronometro.Tick += (_, _) =>
        {
            if (_engine.State == GameState.RondaActiva)
            {
                var elapsed = (DateTime.Now.Ticks - _startTime) / 10000.0;
                DisplayCentralText.Text = (elapsed / 1000).ToString("00.00");
            }
        };
        _timerCronometro.Start();

        ActualizarDisplayTiempo();
        ActualizarBotonesConfig();
    }

    private void ActualizarBotonesConfig()
    {
        BtnConfigRojo.Content = $"Rojo: {_settings.BotonRojo}";
        BtnConfigAzul.Content = $"Azul: {_settings.BotonAzul}";
        BtnConfigReinicio.Content = $"Reinicio: {_settings.BotonReinicio}";
    }

    private void ActualizarDisplayTiempo() => DisplayCentralText.Text = $"{_tiempoObjetivoSegundos:00}.00";

    private void BtnMasTiempo_Click(object sender, RoutedEventArgs e) { _tiempoObjetivoSegundos += 5; ActualizarDisplayTiempo(); }
    private void BtnMenosTiempo_Click(object sender, RoutedEventArgs e) { if (_tiempoObjetivoSegundos > 5) _tiempoObjetivoSegundos -= 5; ActualizarDisplayTiempo(); }

    private void OnStateChanged(GameState state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case GameState.CuentaRegresiva: MensajeEstado = "PREPÁRATE..."; DisplayCentralText.Text = "00.00"; break;
                case GameState.EsperandoSenal: MensajeEstado = "ESPERA LA SEÑAL..."; break;
                case GameState.RondaActiva: MensajeEstado = "¡DETENLO EN EL TIEMPO!"; _startTime = DateTime.Now.Ticks; break;
                case GameState.Ganador: MensajeEstado = "¡RESULTADO!"; break;
            }
        });
    }

    private void OnRoundFinished(Team winner, double msPresionado, double desviacion)
    {
        Dispatcher.Invoke(() =>
        {
            if (msPresionado == -1) MensajeEstado = $"SALIDA EN FALSO. Gana {(winner == Team.Rojo ? "ROJO" : "AZUL")}";
            else
            {
                DisplayCentralText.Text = (msPresionado / 1000.0).ToString("00.00");
                if (Math.Abs(desviacion) <= 30) MensajeEstado = $"¡EXACTO! Gana {(winner == Team.Rojo ? "ROJO" : "AZUL")}";
                else if (desviacion > 0) MensajeEstado = $"¡MUY TARDE! Gana {(winner == Team.Rojo ? "AZUL" : "ROJO")}";
                else MensajeEstado = $"¡MUY PRONTO! Gana {(winner == Team.Rojo ? "AZUL" : "ROJO")}";

                var targetBorder = (desviacion <= 30 && desviacion >= -30) 
                    ? (winner == Team.Rojo ? FindName("MarcadorRojo") as Border : FindName("MarcadorAzul") as Border)
                    : (winner == Team.Rojo ? FindName("MarcadorAzul") as Border : FindName("MarcadorRojo") as Border);
                
                if (targetBorder != null)
                {
                    var animacion = new DoubleAnimation { From = 1.0, To = 1.5, Duration = TimeSpan.FromMilliseconds(200), AutoReverse = true, RepeatBehavior = new RepeatBehavior(3) };
                    targetBorder.BeginAnimation(OpacityProperty, animacion);
                }
            }
            RefreshBindings();
        });
    }

    private async void IniciarRonda()
    {
        if (_engine.State != GameState.Inicio && _engine.State != GameState.Ganador) return;
        _engine.StartRound(_tiempoObjetivoSegundos);
        await Task.Delay(2000);
        _engine.BeginWaitingSignal();
        await Task.Delay(_random.Next(2000, 5000));
        if (_engine.State == GameState.EsperandoSenal) { _startTime = DateTime.Now.Ticks; _engine.ShowSignal(); }
    }

    // --- LÓGICA DE CAPTURA DE BOTONES USB ---
    private void BtnConfigRojo_Click(object sender, RoutedEventArgs e) { _modoCaptura = "Rojo"; BtnConfigRojo.Content = "Presiona..."; }
    private void BtnConfigAzul_Click(object sender, RoutedEventArgs e) { _modoCaptura = "Azul"; BtnConfigAzul.Content = "Presiona..."; }
    private void BtnConfigReinicio_Click(object sender, RoutedEventArgs e) { _modoCaptura = "Reinicio"; BtnConfigReinicio.Content = "Presiona..."; }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Si estamos en la pantalla de configuración capturando un botón...
        if (PantallaConfig.Visibility == Visibility.Visible && _modoCaptura != "")
        {
            string tecla = e.Key.ToString();
            
            if (_modoCaptura == "Rojo") _settings.BotonRojo = tecla;
            else if (_modoCaptura == "Azul") _settings.BotonAzul = tecla;
            else if (_modoCaptura == "Reinicio") _settings.BotonReinicio = tecla;

            _modoCaptura = "";
            ActualizarBotonesConfig();
            return; // Evitamos que la tecla haga otra cosa
        }

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
    private void BtnCerrarConfig_Click(object sender, RoutedEventArgs e) { PantallaConfig.Visibility = Visibility.Collapsed; ActualizarBotonesConfig(); RefreshBindings(); }
    
    private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
    {
        _engine.ResetGame();
        ActualizarDisplayTiempo();
        MensajeEstado = "Pulsa ESPACIO para comenzar";
        RefreshBindings();
    }

    private void RefreshBindings()
    {
        DataContext = null;
        DataContext = this;
    }
}
