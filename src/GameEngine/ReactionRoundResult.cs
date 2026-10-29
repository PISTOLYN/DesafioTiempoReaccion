namespace DesafioTiempoReaccion.GameEngine;
public class ReactionRoundResult
{
    public Team? Winner { get; init; }
    public double ReactionTimeMs { get; init; }
    public bool FalseStart { get; init; }
}
