namespace DesafioTiempoReaccion.GameEngine;

public class ReactionRoundResult
{
    public Team Winner { get; init; }
    public double ReactionTimeMs { get; init; }
    public bool FalseStart { get; init; }

    /// <summary>Equipo que cometió la salida en falso (solo tiene sentido si FalseStart es true).</summary>
    public Team Infractor => Winner == Team.Rojo ? Team.Azul : Team.Rojo;
}
