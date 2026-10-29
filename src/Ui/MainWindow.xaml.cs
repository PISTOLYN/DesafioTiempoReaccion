using System.Windows;
using System.Windows.Input;
using DesafioTiempoReaccion.GameEngine;

namespace DesafioTiempoReaccion.Ui;

public partial class MainWindow : Window
{
    private readonly GameEngine.GameEngine _engine = new();
    private readonly Random _random = new();

    public string DisplayRojo { get; set; } = "00.00";
    public string DisplayAzul { get; set; } = "00.00";
    public string DisplayCentral { get; set; } = "00.00";
    public string MensajeEstado { get; set; } = "Pulsa ESPACIO para comenzar";
    public int PuntosRojo => _engine.PuntosRojo;
    public int PuntosAzul => _engine.PuntosAzul;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _engine.StateChanged += OnStateChanged;
        _engine.RoundFinished += OnRoundFinished;
    }

    private void OnStateChanged(GameState state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case GameState.CuentaRegresiva:
                    MensajeEstado = "PREPÁRATE...";
                    DisplayRojo = DisplayAzul = DisplayCentral = "00.00";
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
            RefreshBindings();
        });
    }

    private void OnRoundFinished(ReactionRoundResult result)
    {
        Dispatcher.Invoke(() =>
        {
            if (result.FalseStart)
                MensajeEstado = $"SALIDA EN FALSO. Gana {(result.Winner == Team.Rojo ? "ROJO" : "AZUL")}";
            else
            {
                MensajeEstado = $"Gana {(result.Winner == Team.Rojo ? "ROJO" : "AZUL")}: {result.ReactionTimeMs:F0} ms";
                if (result.Winner == Team.Rojo) DisplayRojo = (result.ReactionTimeMs / 1000).ToString("00.00");
                else DisplayAzul = (result.ReactionTimeMs / 1000).ToString("00.00");
                DisplayCentral = (result.ReactionTimeMs / 1000).ToString("00.00");
            }
            RefreshBindings();
        });
    }

    private async void BtnIniciar_Click(object sender, RoutedEventArgs e) => await StartRoundAsync();

    private async Task StartRoundAsync()
    {
        if (_engine.State != GameState.Inicio && _engine.State != GameState.Ganador) return;
        _engine.StartRound();
        int delay = _random.Next(2000, 5000);
        await Task.Delay(delay);
        if (_engine.State == GameState.EsperandoSenal) _engine.ShowSignal();
    }

    private void BtnRojo_Click(object sender, RoutedEventArgs e) => _engine.RegisterPress(Team.Rojo);
    private void BtnAzul_Click(object sender, RoutedEventArgs e) => _engine.RegisterPress(Team.Azul);

    private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
    {
        _engine.ResetGame();
        DisplayRojo = DisplayAzul = DisplayCentral = "00.00";
        MensajeEstado = "Pulsa ESPACIO para comenzar";
        RefreshBindings();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) BtnIniciar_Click(sender, e);
        else if (e.Key == Key.A) _engine.RegisterPress(Team.Rojo);
        else if (e.Key == Key.L) _engine.RegisterPress(Team.Azul);
    }

    private void RefreshBindings()
    {
        DataContext = null;
        DataContext = this;
    }
}
