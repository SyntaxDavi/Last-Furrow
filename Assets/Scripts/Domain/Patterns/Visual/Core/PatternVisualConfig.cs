using UnityEngine;

/// <summary>
/// Single Source of Truth para configurações visuais de padrões.
/// Centraliza cores, timings e comportamentos visuais.
/// </summary>
[CreateAssetMenu(fileName = "PatternVisualConfig", menuName = "Patterns/Visual Config", order = 100)]
public class PatternVisualConfig : ScriptableObject
{
    [Header("=== TIER COLORS ===")]
    [Tooltip("Tier 1: 5-14 pts")]
    public Color tier1Color = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Tooltip("Tier 2: 15-34 pts")]
    public Color tier2Color = new Color(0.2f, 0.8f, 0.2f, 1f);
    
    [Tooltip("Tier 3: 35-79 pts")]
    public Color tier3Color = new Color(1f, 0.84f, 0f, 1f);
    
    [Tooltip("Tier 4: 80+ pts")]
    public Color tier4Color = new Color(0.8f, 0.2f, 1f, 1f);
    
    [Header("=== ANALYZING PHASE ===")]
    [Tooltip("Delay entre cada slot analisado")]
    [Range(0f, 1f)]
    public float analyzingSlotDelay = 0.1f;
    
    [Tooltip("Altura da levitação dos slots")]
    [Range(0.05f, 0.3f)]
    public float levitationHeight = 0.1f;
    
    [Tooltip("Duração da levitação")]
    [Range(0.1f, 0.5f)]
    public float levitationDuration = 0.2f;
    
    [Header("=== HIGHLIGHT ===")]
    [Tooltip("Duração do highlight")]
    [Range(0.5f, 3f)]
    public float highlightDuration = 2f;
    
    [Tooltip("Velocidade do pulse")]
    [Range(1f, 5f)]
    public float highlightPulseSpeed = 2f;
    
    [Tooltip("Duração do fade out")]
    [Range(0.1f, 1f)]
    public float highlightFadeOutDuration = 0.3f;
    
    [Header("=== POPUP ===")]
    [Tooltip("Duração total da animação do popup")]
    [Range(0.5f, 3f)]
    public float popupAnimationDuration = 1.5f;
    
    [Tooltip("Escala inicial do popup")]
    [Range(0f, 1f)]
    public float popupStartScale = 0.5f;
    
    [Tooltip("Escala final do popup")]
    [Range(1f, 2f)]
    public float popupEndScale = 1f;
    
    [Header("=== GRID BREATHING ===")]
    [Tooltip("Amplitude da respiração")]
    [Range(0.01f, 0.05f)]
    public float breathingAmount = 0.02f;
    
    [Tooltip("Velocidade da respiração")]
    [Range(0.1f, 1f)]
    public float breathingSpeed = 0.3f;
    
    [Tooltip("Força da reação a padrão detectado")]
    [Range(0.01f, 0.15f)]
    public float patternReactionStrength = 0.05f;
    
    [Tooltip("Duração das reações")]
    [Range(0.1f, 1f)]
    public float reactionDuration = 0.3f;
    
    [Header("=== DECAY VISUAL ===")]
    [Tooltip("Dias para considerar decay crítico")]
    [Range(3, 7)]
    public int criticalDecayDaysThreshold = 4;
    
    [Tooltip("Cor de warning para decay crítico")]
    public Color decayWarningColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    [Header("=== DEBUG ===")]
    [Tooltip("Ativar logs de debug")]
    public bool debugMode = false;
    
    [Tooltip("Congelar animações")]
    public bool freezeAnimations = false;
    
    // === HELPER METHODS ===
    
    public Color GetTierColor(int tier)
    {
        return tier switch
        {
            1 => tier1Color,
            2 => tier2Color,
            3 => tier3Color,
            4 => tier4Color,
            _ => Color.white
        };
    }
    
    public Color ApplyDecayToColor(Color baseColor, int daysActive)
    {
        if (daysActive <= 1) return baseColor;
        
        if (daysActive >= criticalDecayDaysThreshold)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            s = Mathf.Max(0, s - 0.4f);
            Color desaturated = Color.HSVToRGB(h, s, v);
            return Color.Lerp(desaturated, decayWarningColor, 0.3f);
        }
        
        return baseColor;
    }
    
    public void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[PatternVisual] {message}");
        }
    }
}
