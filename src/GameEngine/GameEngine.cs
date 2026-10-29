using System.Diagnostics;
namespace DesafioTiempoReaccion.GameEngine;

public class GameEngine
{
    private readonly object _sync = new();
    private long _signalTimestamp;
    private int _winnerClaimed;
    private bool _signalActive;

    public GameState State { get; private set; } = GameState.Inicio;
    public int PuntosRojo { get; private set; }
    public int PuntosAzul { get; private set; }
    public double UltimoTiempoRojoMs { get; private set; }
    public double UltimoTiempoAzulMs { get; private set; }

    public event Action<GameState>? StateChanged;
    public event Action<ReactionRoundResult>? RoundFinished;

    public void StartRound()
    {
        lock (_sync)
        {
            _winnerClaimed = 0;
            _signalActive = false;
            SetState(GameState.CuentaRegresiva);
            SetState(GameState.EsperandoSenal);
        }
    }

    public void ShowSignal()
    {
        lock (_sync)
        {
            _signalTimestamp = Stopwatch.GetTimestamp();
            _signalActive = true;
            SetState(GameState.RondaActiva);
        }
    }

    public void RegisterPress(Team team)
    {
        lock (_sync)
        {
            long now = Stopwatch.GetTimestamp();

            if (State == GameState.EsperandoSenal || State == GameState.CuentaRegresiva)
            {
                var ganador = team == Team.Rojo ? Team.Azul : Team.Rojo;
                if (ganador == Team.Rojo) PuntosRojo++; else PuntosAzul++;
                SetState(GameState.Ganador);
                RoundFinished?.Invoke(new ReactionRoundResult
                {
                    Winner = ganador,
                    FalseStart = true,
                    ReactionTimeMs = 0
                });
                return;
            }

            if (State != GameState.RondaActiva || !_signalActive) return;

            int claim = team == Team.Rojo ? 1 : 2;
            if (Interlocked.CompareExchange(ref _winnerClaimed, claim, 0) != 0) return;

            double ms = (now - _signalTimestamp) * 1000.0 / Stopwatch.Frequency;
            if (team == Team.Rojo) { UltimoTiempoRojoMs = ms; PuntosRojo++; }
            else { UltimoTiempoAzulMs = ms; PuntosAzul++; }

            _signalActive = false;
            SetState(GameState.Ganador);
            RoundFinished?.Invoke(new ReactionRoundResult { Winner = team, ReactionTimeMs = ms });
        }
    }

    public void ResetGame()
    {
        lock (_sync)
        {
            PuntosRojo = PuntosAzul = 0;
            UltimoTiempoRojoMs = UltimoTiempoAzulMs = 0;
            SetState(GameState.Inicio);
        }
    }

    private void SetState(GameState s)
    {
        State = s;
        StateChanged?.Invoke(s);
    }
}
