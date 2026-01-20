using UnityEngine;

/// <summary>
/// Configuração centralizada para o Pattern Visual Juice System.
/// 
/// RESPONSABILIDADE:
/// - Armazenar TODAS as configurações visuais em um único lugar
/// - Permitir ajuste via Inspector sem tocar em código
/// - Debug mode para testes
/// 
/// USO:
/// - Criar via Assets/Create/Patterns/Visual Config
/// - Referenciar em todos os visual controllers
/// - Uma instância única compartilhada por todos os sistemas
/// 
/// FILOSOFIA: Single Source of Truth para configuração visual.
/// </summary>
[CreateAssetMenu(fileName = "PatternVisualConfig", menuName = "Patterns/Visual Config", order = 100)]
public class PatternVisualConfig : ScriptableObject
{
    [Header("=== TIER COLORS ===")]
    [Tooltip("Tier 1: Padrões 5-15 pts (Prata/Cinza)")]
    public Color tier1Color = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Tooltip("Tier 2: Padrões 15-35 pts (Verde)")]
    public Color tier2Color = new Color(0.2f, 0.8f, 0.2f, 1f);
    
    [Tooltip("Tier 3: Padrões 35-60 pts (Dourado)")]
    public Color tier3Color = new Color(1f, 0.84f, 0f, 1f);
    
    [Tooltip("Tier 4: Padrões 80-150 pts (Roxo Místico)")]
    public Color tier4Color = new Color(0.8f, 0.2f, 1f, 1f);
    
    [Header("=== HIGHLIGHT SETTINGS ===")]
    [Tooltip("Velocidade do pulse animation")]
    [Range(0.5f, 5f)]
    public float pulseSpeed = 2f;
    
    [Tooltip("Duração do highlight em segundos")]
    [Range(0.5f, 3f)]
    public float highlightDuration = 1.5f;
    
    [Tooltip("Delay entre highlights sequenciais")]
    [Range(0.05f, 0.5f)]
    public float sequentialDelay = 0.15f;
    
    [Header("=== ANALYZING PHASE (ONDA 5.5) ===")]
    [Tooltip("Duração da análise por slot (segundos)")]
    [Range(0.05f, 1f)]
    public float analyzingDurationPerSlot = 0.2f;
    
    [Tooltip("Altura da levitação durante análise")]
    [Range(0.05f, 0.3f)]
    public float analyzingLevitationHeight = 0.1f;
    
    [Tooltip("Analisar apenas slots com plantas? (otimizado)")]
    public bool analyzingOnlyPlants = true;
    
    [Tooltip("Mostrar pulse rosa durante análise?")]
    public bool analyzingShowPulse = true;
    
    [Header("=== DECAY VISUAL ===")]
    [Tooltip("Apenas destacar decay em padrões importantes?")]
    public bool onlyHighlightImportantDecay = true;
    
    [Tooltip("Threshold de pontos para mostrar decay (se onlyHighlightImportantDecay = true)")]
    [Range(10, 100)]
    public int importantDecayThreshold = 30;
    
    [Tooltip("Dias para considerar decay crítico (sempre mostra)")]
    [Range(3, 7)]
    public int criticalDecayDaysThreshold = 4;
    
    [Tooltip("Cor de warning para decay crítico")]
    public Color decayWarningColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    [Header("=== POP-UP TEXT ===")]
    [Tooltip("Offset do texto 'Padrões' (posição base)")]
    public Vector2 popupOffset = new Vector2(0, 50);
    
    [Tooltip("Fade in duration")]
    [Range(0.1f, 1f)]
    public float fadeInDuration = 0.2f;
    
    [Tooltip("Hold duration (texto visível)")]
    [Range(0.3f, 2f)]
    public float holdDuration = 0.8f;
    
    [Tooltip("Fade out duration")]
    [Range(0.1f, 1f)]
    public float fadeOutDuration = 0.2f;
    
    [Tooltip("Tamanho base do texto")]
    [Range(20f, 60f)]
    public float baseTextSize = 36f;
    
    [Tooltip("Multiplicador de tamanho por Tier")]
    [Range(1f, 2f)]
    public float scaleBonusPerTier = 1.2f;
    
    [Header("=== GRID BREATHING ===")]
    [Tooltip("Amplitude da respiração (± scale)")]
    [Range(0.01f, 0.05f)]
    public float breathingAmount = 0.02f;
    
    [Tooltip("Velocidade da respiração (ciclos/segundo)")]
    [Range(0.1f, 1f)]
    public float breathingSpeed = 0.3f;
    
    [Tooltip("Força da reação ao plantar")]
    [Range(0.01f, 0.1f)]
    public float plantReactionStrength = 0.02f;
    
    [Tooltip("Força da reação ao colher")]
    [Range(0.01f, 0.1f)]
    public float harvestReactionStrength = 0.03f;
    
    [Tooltip("Força da reação a padrão detectado")]
    [Range(0.01f, 0.15f)]
    public float patternReactionStrength = 0.05f;
    
    [Tooltip("Duração das reações")]
    [Range(0.1f, 1f)]
    public float reactionDuration = 0.3f;
    
    [Header("=== PARTICLE SYSTEM ===")]
    [Tooltip("Máximo de partículas ativas simultaneamente")]
    [Range(20, 200)]
    public int maxActiveParticles = 50;
    
    [Tooltip("Máximo de partículas por burst")]
    [Range(5, 50)]
    public int maxParticlesPerBurst = 20;
    
    [Tooltip("LOD scaling (1.0 = full, 0.5 = half)")]
    [Range(0.1f, 1f)]
    public float particleLOD = 1f;
    
    [Tooltip("Habilitar sistema de partículas?")]
    public bool enableParticles = true;
    
    [Header("=== SORTING LAYERS ===")]
    [Tooltip("Sorting order - Highlights")]
    public int highlightSortingOrder = 5;
    
    [Tooltip("Sorting order - Pop-ups")]
    public int popupSortingOrder = 10;
    
    [Tooltip("Sorting order - Combo Counter")]
    public int comboSortingOrder = 8;
    
    [Tooltip("Sorting order - Sinergia")]
    public int synergiaSortingOrder = 3;
    
    [Tooltip("Sorting order - Partículas")]
    public int particleSortingOrder = 1;
    
    [Header("=== OBJECT POOLING ===")]
    [Tooltip("Tamanho inicial do pool de pop-ups")]
    [Range(3, 10)]
    public int popupPoolSize = 5;
    
    [Tooltip("Tamanho inicial do pool de partículas")]
    [Range(10, 50)]
    public int particlePoolSize = 20;
    
    [Tooltip("Pre-warm pools no Awake?")]
    public bool preWarmPools = true;
    
    [Header("=== DEBUG MODE ===")]
    [Tooltip("Ativar modo debug (logs verbosos, overrides)")]
    public bool debugMode = false;
    
    [Tooltip("Mostrar cores de tier sem animação")]
    public bool showTierColors = true;
    
    [Tooltip("Log de eventos visuais no Console")]
    public bool logVisualEvents = false;
    
    [Tooltip("Desabilitar pooling (força instanciar - testar GC)")]
    public bool disablePooling = false;
    
    [Tooltip("Congelar animações (testar estados)")]
    public bool freezeAnimations = false;
    
    [Tooltip("Mostrar métricas de performance")]
    public bool showPerformanceMetrics = false;
    
    [Header("=== DEBUG OVERRIDES ===")]
    [Tooltip("Forçar decay visual em todos os padrões (testar Dia 4+)")]
    public bool forceDecayVisual = false;
    
    [Tooltip("Forçar tier específico (0 = normal, 1-4 = override)")]
    [Range(0, 4)]
    public int forceTierOverride = 0;
    
    // === MÉTODOS HELPER ===
    
    /// <summary>
    /// Retorna a cor do tier baseado no score base do padrão.
    /// </summary>
    public Color GetTierColor(int tier)
    {
        // Debug override
        if (debugMode && forceTierOverride > 0)
        {
            tier = forceTierOverride;
        }
        
        return tier switch
        {
            1 => tier1Color,
            2 => tier2Color,
            3 => tier3Color,
            4 => tier4Color,
            _ => Color.white
        };
    }
    
    /// <summary>
    /// Aplica decay à cor (reduz saturação e brilho).
    /// </summary>
    public Color ApplyDecayToColor(Color baseColor, int daysActive)
    {
        if (daysActive <= 1 && !forceDecayVisual) return baseColor;
        
        float saturationLoss = 0f;
        Color tint = Color.white;
        
        if (daysActive >= criticalDecayDaysThreshold || forceDecayVisual)
        {
            // Crítico: -40% saturação + tint vermelho
            saturationLoss = 0.4f;
            tint = Color.Lerp(Color.white, decayWarningColor, 0.5f);
        }
        else if (daysActive >= 3)
        {
            // Warning: -20% saturação
            saturationLoss = 0.2f;
        }
        
        // Desaturar
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        s = Mathf.Max(0, s - saturationLoss);
        Color desaturated = Color.HSVToRGB(h, s, v);
        
        // Aplicar tint
        return Color.Lerp(desaturated, tint, 0.3f);
    }
    
    /// <summary>
    /// Verifica se deve mostrar warning de decay para este padrão.
    /// </summary>
    public bool ShouldShowDecayWarning(int baseScore, int daysActive)
    {
        if (daysActive <= 1) return false;
        if (!onlyHighlightImportantDecay) return true;
        
        // Mostra apenas se padrão valioso OU crítico
        return baseScore >= importantDecayThreshold || 
               daysActive >= criticalDecayDaysThreshold;
    }
    
    /// <summary>
    /// Log de debug (apenas se debugMode ativo).
    /// </summary>
    public void DebugLog(string message)
    {
        if (debugMode && logVisualEvents)
        {
            Debug.Log($"[PatternVisual] {message}");
        }
    }
}
