using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estado de desbloqueamento do grid (separado do RunData para clareza).
/// 
/// RESPONSABILIDADE:
/// - Armazena metadados sobre o padrão inicial gerado
/// - Permite validação e regeneração se necessário
/// - Cache descartável (Seed é autoridade)
/// 
/// DESIGN DECISION:
/// - Pattern é cache, não verdade absoluta
/// - Se grid mudar ou algoritmo mudar, regenera a partir do Seed
/// - Versionado para migração futura
/// </summary>
[Serializable]
public class GridUnlockState
{
    [Tooltip("Versão do algoritmo de geração (para detectar mudanças)")]
    public int AlgorithmVersion = 1;

    [Tooltip("Dimensões do grid quando padrão foi gerado")]
    public int GridWidth;
    public int GridHeight;

    [Tooltip("Quantidade de slots que deveriam ser desbloqueados")]
    public int TargetSlotCount;

    [Tooltip("Cache do padrão gerado (coordenadas x,y)")]
    public List<Vector2Int> Pattern = new List<Vector2Int>();

    /// <summary>
    /// Valida se este estado ainda é compatível com o grid atual.
    /// </summary>
    public bool IsCompatibleWith(int currentWidth, int currentHeight, int currentVersion)
    {
        return AlgorithmVersion == currentVersion
            && GridWidth == currentWidth
            && GridHeight == currentHeight;
    }

    /// <summary>
    /// Cria novo estado para grid específico.
    /// </summary>
    public static GridUnlockState Create(int width, int height, int slotCount, List<Vector2Int> pattern)
    {
        return new GridUnlockState
        {
            AlgorithmVersion = 1,
            GridWidth = width,
            GridHeight = height,
            TargetSlotCount = slotCount,
            Pattern = new List<Vector2Int>(pattern) // Cópia defensiva
        };
    }
}
