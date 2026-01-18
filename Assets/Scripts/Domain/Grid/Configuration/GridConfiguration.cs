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
}
