/// <summary>
/// Interface para detecção de padrões específicos no grid.
/// Strategy Pattern: cada detector implementa sua lógica de detecção.
/// </summary>
public interface IPatternDetector
{
    /// <summary>
    /// ID único do padrão (ex: "ADJACENT_PAIR", "FULL_LINE", "CROSS")
    /// </summary>
    string PatternID { get; }
    
    /// <summary>
    /// Nome exibido na UI (ex: "Par Adjacente", "Linha Completa")
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Pontuação base do padrão
    /// </summary>
    int BaseScore { get; }
    
    /// <summary>
    /// Detecta o padrão em um slot específico e seus adjacentes.
    /// </summary>
    /// <param name="gridService">Serviço de acesso ao grid</param>
    /// <param name="slotIndex">Índice do slot sendo analisado</param>
    /// <param name="allSlotIndices">Todos os índices de slots no grid (para referência)</param>
    /// <returns>PatternMatch se encontrado, null caso contrário</returns>
    PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices);
    
    /// <summary>
    /// Verifica se este detector pode ser aplicado neste slot.
    /// Útil para otimização (ex: não testar linha se slot está bloqueado)
    /// </summary>
    bool CanDetectAt(IGridService gridService, int slotIndex);
}
