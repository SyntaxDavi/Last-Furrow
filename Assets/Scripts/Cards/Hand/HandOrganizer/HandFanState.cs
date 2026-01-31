/// <summary>
/// Estados da transição Fan Out/In. Impede race conditions.
/// </summary>
public enum HandFanState
{
    Normal,      // Cartas em posição normal
    FanningOut,  // Transição: saindo da tela
    FannedOut,   // Cartas fora da tela
    FanningIn    // Transição: voltando para a tela
}
