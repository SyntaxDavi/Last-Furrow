/// <summary>
/// Constantes centralizadas para IDs de padrões.
/// 
/// FUNÇÃO:
/// - Eliminar magic strings espalhadas pelo código
/// - Type-safe IDs
/// - Refactor-friendly (renomear uma vez, atualiza em todos os lugares)
/// - Documentação centralizada
/// 
/// USO:
/// - if (match.PatternID == PatternIDs.RAINBOW_LINE)
/// - Nunca usar strings literais como "RAINBOW_LINE" diretamente
/// </summary>
public static class PatternIDs
{
    // Tier 1 - Iniciante (5-15 pts)
    public const string ADJACENT_PAIR = "ADJACENT_PAIR";
    public const string TRIO_LINE = "TRIO_LINE";
    public const string CORNER = "CORNER";
    
    // Tier 2 - Casual (15-35 pts)
    public const string FULL_LINE = "FULL_LINE";
    public const string CHECKER = "CHECKER";
    public const string CROSS = "CROSS";
    
    // Tier 3 - Dedicado (35-60 pts)
    public const string DIAGONAL = "DIAGONAL";
    public const string FRAME = "FRAME";
    public const string RAINBOW_LINE = "RAINBOW_LINE";
    
    // Tier 4 - Master (80-150 pts)
    public const string PERFECT_GRID = "PERFECT_GRID";
    
    /// <summary>
    /// Verifica se um PatternID é válido.
    /// </summary>
    public static bool IsValid(string patternID)
    {
        return patternID == ADJACENT_PAIR ||
               patternID == TRIO_LINE ||
               patternID == CORNER ||
               patternID == FULL_LINE ||
               patternID == CHECKER ||
               patternID == CROSS ||
               patternID == DIAGONAL ||
               patternID == FRAME ||
               patternID == RAINBOW_LINE ||
               patternID == PERFECT_GRID;
    }
    
    /// <summary>
    /// Retorna se o padrão requer cálculo de diversidade especial.
    /// </summary>
    public static bool RequiresDiversityBonus(string patternID)
    {
        return patternID == RAINBOW_LINE || patternID == PERFECT_GRID;
    }
}
