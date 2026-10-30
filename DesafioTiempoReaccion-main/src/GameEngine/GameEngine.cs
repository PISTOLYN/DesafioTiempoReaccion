using System.Diagnostics;
namespace DesafioTiempoReaccion.GameEngine;

/// <summary>
/// Motor del juego. Debe usarse SIEMPRE desde el hilo de la interfaz (WPF):
/// los retrasos usan await, así que todo continúa en ese mismo hilo y no hacen
/// falta locks ni Dispatcher.Invoke (que antes podían provocar bloqueos).
/// </summary>
public class GameEngine
{
    private readonly Random _random = new();
    private CancellationTokenSource? _roundCts;
    private long _signalTimestamp;

    public GameEngine(GameSettings settings) => Settings = settings;

    public GameSettings Settings { get; }
    public GameState State { get; private set; } = GameState.Inicio;
    public int PuntosRojo { get; private set; }
    public int PuntosAzul { get; private set; }

    /// <summary>Milisegundos transcurridos desde que apareció la señal (0 si no hay ronda activa).</summary>
    public double ElapsedMs =>
        State == GameState.RondaActiva ? TicksToMs(Stopwatch.GetTimestamp() - _signalTimestamp) : 0;

    public event Action<GameState>? StateChanged;
    public event Action<ReactionRoundResult>? RoundFinished;
    public event Action? TimedOut;

    /// <summary>Inicia una ronda: cuenta regresiva → espera aleatoria → señal.</summary>
    public async Task StartRoundAsync()
    {
        if (State != GameState.Inicio && State != GameState.Ganador) return;

        _roundCts?.Cancel();
        var cts = _roundCts = new CancellationTokenSource();
        var ct = cts.Token;

        try
        {
            SetState(GameState.CuentaRegresiva);
            await WaitAsync(Settings.CuentaRegresivaMs, ct);

            SetState(GameState.EsperandoSenal);
            await WaitAsync(_random.Next(Settings.EsperaMinMs, Settings.EsperaMaxMs + 1), ct);

            _signalTimestamp = Stopwatch.GetTimestamp();
            SetState(GameState.RondaActiva);

            await WaitAsync(Settings.TiempoLimiteSegundos * 1000, ct);

            // Nadie presionó a tiempo
            SetState(GameState.Inicio);
            TimedOut?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // Ronda cancelada por un ganador o por reinicio: no hacer nada.
        }
    }

    public void RegisterPress(Team team)
    {
        switch (State)
        {
            case GameState.CuentaRegresiva:
            case GameState.EsperandoSenal:
                // Salida en falso: el punto es para el equipo contrario.
                FinishRound(team == Team.Rojo ? Team.Azul : Team.Rojo, 0, falseStart: true);
                break;

            case GameState.RondaActiva:
                double ms = TicksToMs(Stopwatch.GetTimestamp() - _signalTimestamp);
                FinishRound(team, ms, falseStart: false);
                break;
        }
        // En Inicio / Ganador las pulsaciones se ignoran.
    }

    public void ResetGame()
    {
        _roundCts?.Cancel();
        PuntosRojo = PuntosAzul = 0;
        SetState(GameState.Inicio);
    }

    private void FinishRound(Team winner, double ms, bool falseStart)
    {
        _roundCts?.Cancel();
        if (winner == Team.Rojo) PuntosRojo++; else PuntosAzul++;

        SetState(GameState.Ganador);
        RoundFinished?.Invoke(new ReactionRoundResult
        {
            Winner = winner,
            ReactionTimeMs = ms,
            FalseStart = falseStart
        });
    }

    // Task.Delay + comprobación extra: si el delay terminó justo antes de cancelar,
    // la continuación ya estaba en cola y sin esto "resucitaría" una ronda ya terminada.
    private static async Task WaitAsync(int ms, CancellationToken ct)
    {
        await Task.Delay(ms, ct);
        ct.ThrowIfCancellationRequested();
    }

    private static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private void SetState(GameState s)
    {
        State = s;
        StateChanged?.Invoke(s);
    }
}
