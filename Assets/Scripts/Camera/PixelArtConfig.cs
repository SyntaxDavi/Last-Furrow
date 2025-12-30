using UnityEngine;

[CreateAssetMenu(fileName = "PixelConfig", menuName = "Config/Pixel Art Config")]
public class PixelArtConfig : ScriptableObject
{
    [Header("Core Settings")]
    [Tooltip("Quantos pixels cabem em 1 unidade da Unity.")]
    public int PPU = 32;

    [Tooltip("Resolução vertical de referência (ex: 180, 240, 360).")]
    public int ReferenceVerticalPixels = 360;

    /// <summary>
    /// Arredonda um valor flutuante para a grade de pixels mais próxima.
    /// </summary>
    public float SnapToPixel(float value)
    {
        if (PPU == 0) return value;
        return Mathf.Round(value * PPU) / PPU;
    }

    /// <summary>
    /// Arredonda um vetor para a grade de pixels mais próxima.
    /// Mantém Z inalterado.
    /// </summary>
    public Vector3 SnapPosition(Vector3 position)
    {
        float x = SnapToPixel(position.x);
        float y = SnapToPixel(position.y);
        return new Vector3(x, y, position.z);
    }
}