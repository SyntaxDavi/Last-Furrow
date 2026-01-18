using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridConfiguration", menuName = "Last Furrow/Configs/Grid Configuration")]
public class GridConfiguration : ScriptableObject
{
    [Header("Dimensions")]
    [Tooltip("Number of columns (Width)")]
    [Min(1)] public int Columns = 5;

    [Tooltip("Number of rows (Height)")]
    [Min(1)] public int Rows = 5;

    [Header("Initial State")]
    [Tooltip("Coordinates (X,Y) of slots that start unlocked. (0,0) is bottom-left.")]
    public List<Vector2Int> DefaultUnlockedCoordinates = new List<Vector2Int>
    {
        // 3x3 center for a 5x5 grid (indices 1,2,3 for both x and y)
        new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
        new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2),
        new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(3, 3)
    };

    public int TotalSlots => Columns * Rows;

    /// <summary>
    /// Gera um hash estrutural da configuração do grid.
    /// 
    /// IMPORTANTE: O hash só considera aspectos ESTRUTURAIS do mundo:
    /// - Dimensões (Rows, Columns)
    /// - Layout inicial de desbloqueio (DefaultUnlockedCoordinates)
    /// 
    /// Mudanças visuais (sprites, cores, efeitos) NÃO afetam o hash.
    /// 
    /// POLÍTICA: Se o hash mudar, saves antigos serão REJEITADOS.
    /// Isso garante que o mundo do jogador nunca seja corrompido por mudanças estruturais.
    /// </summary>
    public int GetVersionHash()
    {
        unchecked
        {
            int hash = 17;
            
            // Dimensões do grid (crítico)
            hash = hash * 31 + Columns;
            hash = hash * 31 + Rows;
            
            // Estrutura de desbloqueio inicial (ordem não importa)
            // Usamos count + soma de coordenadas para ser order-independent
            if (DefaultUnlockedCoordinates != null)
            {
                hash = hash * 31 + DefaultUnlockedCoordinates.Count;
                
                // Soma de coordenadas (comutativa)
                int coordSum = 0;
                foreach (var coord in DefaultUnlockedCoordinates)
                {
                    coordSum += coord.x * 1000 + coord.y; // Evita colisões (1,2) vs (2,1)
                }
                hash = hash * 31 + coordSum;
            }
            
            return hash;
        }
    }
}
