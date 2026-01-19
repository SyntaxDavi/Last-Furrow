using UnityEngine;

/// <summary>
/// Configuração visual centralizada para o Grid.
/// ScriptableObject permite customização no Inspector sem recompilar.
/// 
/// RESPONSABILIDADE:
/// - Definir todas as cores e durações de animação
/// - Permitir diferentes configs (Easy/Normal/Hard, Day/Night themes)
/// - Centralizar valores visuais em um único lugar
/// 
/// USO:
/// Create ? Grid/Visual Config
/// Arraste no GridVisualBootstrapper
/// </summary>
[CreateAssetMenu(fileName = "GridVisualConfig", menuName = "Grid/Visual Config")]
public class GridVisualConfig : ScriptableObject
{
    [Header("Base Colors")]
    [Tooltip("Cor do solo seco (padrão branco)")]
    public Color dryColor = Color.white;
    
    [Tooltip("Cor do solo molhado (azulado)")]
    public Color wetColor = new Color(0.6f, 0.6f, 1f, 1f);
    
    [Tooltip("Cor de slot bloqueado (escuro)")]
    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("State Overlays")]
    [Tooltip("Overlay de planta madura (verde forte)")]
    public Color matureOverlay = new Color(0f, 1f, 0f, 0.3f);
    
    [Tooltip("Overlay de planta murcha (amarelo claro seco)")]
    public Color witheredOverlay = new Color(1f, 1f, 0.7f, 0.3f);
    
    [Tooltip("Overlay quando grid está desabilitado (Shopping/Weekend)")]
    public Color disabledOverlay = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Hover Feedback")]
    [Tooltip("Highlight de hover válido (branco translúcido)")]
    public Color validHover = new Color(1f, 1f, 1f, 0.4f);
    
    [Tooltip("Highlight de hover inválido (vermelho translúcido)")]
    public Color invalidHover = new Color(1f, 0f, 0f, 0.4f);
    
    [Tooltip("Highlight de slot desbloqueável (verde translúcido)")]
    public Color unlockableHover = new Color(0f, 1f, 0f, 0.3f);

    [Header("Flash Effects")]
    [Tooltip("Cor do flash de erro")]
    public Color errorFlash = Color.red;
    
    [Tooltip("Cor do pulse durante análise (rosa claro)")]
    public Color analyzingPulse = new Color(1f, 0.7f, 0.7f, 1f);
    
    [Tooltip("Duração do flash de erro (segundos)")]
    [Range(0.1f, 1f)]
    public float flashDuration = 0.2f;
    
    [Tooltip("Duração do pulse de análise (segundos)")]
    [Range(0.1f, 2f)]
    public float pulseDuration = 0.5f;

    [Header("Render Layers (Priority System)")]
    [Tooltip("Sorting order offset para overlay de estado (+1 sobre base)")]
    public int stateOverlayOffset = 1;
    
    [Tooltip("Sorting order offset para overlay de GameState (+2 sobre base)")]
    public int gameStateOverlayOffset = 2;
    
    [Tooltip("Sorting order offset para highlight de hover (+3 sobre base)")]
    public int hoverHighlightOffset = 3;
    
    [Tooltip("Sorting order offset para efeitos de flash (+4 sobre base)")]
    public int flashEffectOffset = 4;
}
