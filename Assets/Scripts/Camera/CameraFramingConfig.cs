using UnityEngine;

/// <summary>
/// Configuração de enquadramento da câmera.
/// 
/// FILOSOFIA DE DESIGN:
/// - "Mundo é protagonista" ? Padding generoso para arte de fundo
/// - "Grid é estrutura" ? Câmera protege o grid, não compensa UI
/// - "Espaço artístico" ? Padding é decisão estética, não resíduo técnico
/// 
/// PIXEL ART:
/// - Snap sempre múltiplo de 4 (alinhado com PPU=32)
/// - Garante sem bleeding/artefatos
/// </summary>
[CreateAssetMenu(fileName = "CameraFramingConfig", menuName = "Last Furrow/Camera/Framing Config")]
public class CameraFramingConfig : ScriptableObject
{
    [Header("Padding (World Units) - Separado por Lado")]
    [Tooltip("Espaço extra em volta do grid para arte de fundo e ambientação.\nPermite composição assimétrica (ex: mais céu que chão).")]
    public float PaddingLeft = 3f;
    public float PaddingRight = 3f;
    public float PaddingTop = 3.5f;
    public float PaddingBottom = 2f;

    [Header("Pixel Perfect Settings")]
    [Tooltip("Garante que o tamanho da câmera seja múltiplo de 4 pixels.\nEssencial para evitar artefatos com PPU=32.")]
    public bool SnapToMultipleOf4 = true;

    [Header("Debug")]
    [Tooltip("Desenha gizmos mostrando bounds do grid e área visível da câmera.")]
    public bool ShowDebugBounds = true;
    public Color GridBoundsColor = Color.green;
    public Color CameraBoundsColor = Color.cyan;
}
