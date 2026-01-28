using UnityEngine;

/// <summary>
/// Configuração visual centralizada para o Grid.
/// ScriptableObject permite customização no Inspector sem recompilar.
/// </summary>
[CreateAssetMenu(fileName = "GridVisualConfig", menuName = "Grid/Visual Config")] 
public class GridVisualConfig : ScriptableObject
{
    [Header("Cursor Animado")]
    public RuntimeAnimatorController cursorAnimatorController;
    public Vector3 cursorLocalOffset = new Vector3(0, 0, -0.1f);
    public Vector3 cursorLocalScale = Vector3.one;

    [Header("Sprites de Solo")]
    public Sprite drySoilSprite;
    public Sprite wetSoilSprite;
    public Sprite lockedSoilSprite;

    [Header("Overlays de Estado")]
    public Color plantedOverlay = new Color(1, 1, 1, 0.5f);
    public Color matureOverlay = new Color(0, 1, 0, 0.5f);
    public Color witheredOverlay = new Color(1, 0, 0, 0.5f);
    public Color validHover = new Color(0, 1, 0, 0.3f);

    [Header("Pattern Elevation (Combo Juice)")]
    [Tooltip("Quanto o slot levita quando faz parte de um combo.")]
    public float patternElevationOffset = 0.3f;
    [Tooltip("Velocidade da animação de levitação.")]
    public float elevationSpeed = 10f;

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

    private void OnValidate()
    {
        if (patternElevationOffset <= 0) patternElevationOffset = 0.3f;
        if (elevationSpeed <= 0) elevationSpeed = 10f;
    }
}
