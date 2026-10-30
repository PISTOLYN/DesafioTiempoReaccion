namespace DesafioTiempoReaccion.GameEngine;

public class GameSettings
{
    public string TituloDesafio { get; set; } = "DESAFÍO TIEMPO DE REACCIÓN CHALLENGE";
    public int TiempoLimiteSegundos { get; set; } = 10;
    public string BotonRojo { get; set; } = "A"; // Tecla por defecto
    public string BotonAzul { get; set; } = "L"; // Tecla por defecto
    public string BotonReinicio { get; set; } = "R"; // Tecla por defecto
}
