using System.Collections.Generic;

/// <summary>
/// Interface: Abstração do cache de detecção de padrões.
/// SOLID: Dependency Inversion Principle.
/// Permite injetar implementação mockável (testes).
/// </summary>
public interface IPatternCache
{
    bool HasPatterns();
    List<PatternMatch> GetPatternsForSlot(int slotIndex);
    void Clear();
}
