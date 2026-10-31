using System.Diagnostics;
namespace DesafioTiempoReaccion.GameEngine;

public class TargetTimeResult
{
    public double RojoMs { get; init; }
    public double AzulMs { get; init; }
    public double ObjetivoMs { get; init; }
    /// <summary>Equipo ganador; null si nadie acertó o hubo empate.</summary>
    public Team? Winner { get; init; }
    public bool RojoAcerto { get; init; }
    public bool AzulAcerto { get; init; }
}

/// <summary>
/// Modo "Tiempo Objetivo": ambos equipos detienen su cronómetro lo más cerca posible del objetivo.
/// Debe usarse desde el hilo de la interfaz (igual que GameEngine).
/// </summary>
public class TargetTimeEngine
{
    public const int MinMinutes = 5, MaxMinutes = 95, StepMinutes = 5;

    private CancellationTokenSource? _cts;
    private long _startTs;
    private double _rojoMs, _azulMs;

    public GameState State { get; private set; } = GameState.Inicio;
    public int TargetMinutes { get; private set; } = MinMinutes;
    /// <summary>Margen (± ms) para considerar que un equipo "le atinó" al objetivo.</summary>
    public double MargenMs { get; set; } = 1000;
    public double TargetMs => TargetMinutes * 60000.0;
    public int PuntosRojo { get; private set; }
    public int PuntosAzul { get; private set; }
    public bool RojoListo { get; private set; }
    public bool AzulListo { get; private set; }

    public double ElapsedMs =>
        State == GameState.RondaActiva ? TicksToMs(Stopwatch.GetTimestamp() - _startTs) : 0;

    public event Action<GameState>? StateChanged;
    /// <summary>3, 2, 1 y 0 (= "¡Ahora!", en ese instante empiezan a contar los dos equipos).</summary>
    public event Action<int>? CountdownTick;
    public event Action<Team, double>? TeamStopped;
    public event Action<TargetTimeResult>? Finished;
    public event Action? TargetChanged;

    /// <summary>Cambia el objetivo en pasos de 5 min (solo antes de iniciar la ronda).</summary>
    public bool AdjustTarget(int steps)
    {
        if (State != GameState.Inicio && State != GameState.Ganador) return false;
        int nuevo = Math.Clamp(TargetMinutes + steps * StepMinutes, MinMinutes, MaxMinutes);
        if (nuevo == TargetMinutes) return false;
        TargetMinutes = nuevo;
        TargetChanged?.Invoke();
        return true;
    }

    public async Task StartAsync()
    {
        if (State != GameState.Inicio && State != GameState.Ganador) return;

        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        var ct = cts.Token;
        RojoListo = AzulListo = false;
        _rojoMs = _azulMs = 0;

        try
        {
            SetState(GameState.CuentaRegresiva);
            for (int n = 3; n >= 1; n--)
            {
                CountdownTick?.Invoke(n);
                await Task.Delay(1000, ct);
                ct.ThrowIfCancellationRequested();
            }

            _startTs = Stopwatch.GetTimestamp();
            SetState(GameState.RondaActiva);
            CountdownTick?.Invoke(0);
        }
        catch (OperationCanceledException) { }
    }

    public void Stop(Team team)
    {
        if (State != GameState.RondaActiva) return;
        double ms = TicksToMs(Stopwatch.GetTimestamp() - _startTs);

        if (team == Team.Rojo) { if (RojoListo) return; RojoListo = true; _rojoMs = ms; }
        else { if (AzulListo) return; AzulListo = true; _azulMs = ms; }

        TeamStopped?.Invoke(team, ms);
        if (RojoListo && AzulListo) Finish();
    }

    public void ResetGame()
    {
        _cts?.Cancel();
        PuntosRojo = PuntosAzul = 0;
        RojoListo = AzulListo = false;
        SetState(GameState.Inicio);
    }

    private void Finish()
    {
        double dr = Math.Abs(_rojoMs - TargetMs), da = Math.Abs(_azulMs - TargetMs);
        bool aciertoR = dr <= MargenMs, aciertoA = da <= MargenMs;

        // Solo puede ganar quien le atinó al objetivo. Si acertaron los dos, gana el más cercano.
        Team? winner = null;
        if (aciertoR && !aciertoA) winner = Team.Rojo;
        else if (aciertoA && !aciertoR) winner = Team.Azul;
        else if (aciertoR && aciertoA) winner = dr < da ? Team.Rojo : (da < dr ? Team.Azul : (Team?)null);

        if (winner == Team.Rojo) PuntosRojo++;
        else if (winner == Team.Azul) PuntosAzul++;

        SetState(GameState.Ganador);
        Finished?.Invoke(new TargetTimeResult { RojoMs = _rojoMs, AzulMs = _azulMs, ObjetivoMs = TargetMs, Winner = winner, RojoAcerto = aciertoR, AzulAcerto = aciertoA });
    }

    private static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private void SetState(GameState s)
    {
        State = s;
        StateChanged?.Invoke(s);
    }
}
