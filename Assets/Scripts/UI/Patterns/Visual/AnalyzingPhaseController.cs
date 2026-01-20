using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller responsável pela ANALYZING PHASE - animação de análise dos slots.
/// 
/// RESPONSABILIDADE:
/// - Animar slots individualmente (levitação) ao serem analisados
/// - Passar apenas por slots com plantas (otimizado)
/// - Timing configurável via Inspector
/// - Triggerar antes da detection de padrões
/// 
/// FLOW:
/// Sleep ? Analyzing Phase (este controller) ? Pattern Detection ? Highlights ? Sinergia
/// 
/// FILOSOFIA: "O grid está sendo escaneado slot por slot"
/// 
/// ONDA 5.5 REFACTORED:
/// - Cacheia slots (evita GetComponentsInChildren repetido)
/// - Usa localPosition (funciona com hierarquia de Canvas)
/// - Salva originalPosition de cada slot (reset correto)
/// - Filtragem real de slots com plantas via GridSlotView.HasPlant()
/// - Pulse rosa implementado via GridSlotView.TriggerAnalyzingPulse()
/// </summary>
public class AnalyzingPhaseController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Grid Reference")]
    [Tooltip("Referência ao GridManager para acessar slots")]
    [SerializeField] private GridManager _gridManager;
    
    [Header("Analyzing Settings")]
    [Tooltip("Tempo de análise por slot (segundos)")]
    [Range(0.05f, 1f)]
    [SerializeField] private float _durationPerSlot = 0.2f;
    
    [Tooltip("Altura da levitação ao analisar (local space)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float _levitationHeight = 0.1f;
    
    [Tooltip("Analisar apenas slots com plantas? (otimizado)")]
    [SerializeField] private bool _onlyPlantsSlots = true;
    
    [Tooltip("Mostrar pulse rosa durante análise?")]
    [SerializeField] private bool _showAnalyzingPulse = true;
    
    // Cache de slots (performance)
    private List<GridSlotView> _cachedSlots;
    
    // Posições originais (para reset correto)
    private Dictionary<GridSlotView, Vector3> _originalPositions;
    
    // Estado
    private bool _isAnalyzing;
    private Coroutine _analyzingCoroutine;
    
    private void Awake()
    {
        _originalPositions = new Dictionary<GridSlotView, Vector3>();
    }
    
    private void Start()
    {
        // Cachear slots no Start (evita GC em runtime)
        CacheSlots();
    }
    
    /// <summary>
    /// Cacheia slots para evitar GetComponentsInChildren repetido.
    /// </summary>
    private void CacheSlots()
    {
        if (_gridManager == null)
        {
            Debug.LogError("[AnalyzingPhaseController] GridManager não atribuído!");
            return;
        }
        
        _cachedSlots = new List<GridSlotView>();
        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                _cachedSlots.Add(slot);
                
                // Salvar posição original de cada slot
                _originalPositions[slot] = slot.transform.localPosition;
            }
        }
        
        _config?.DebugLog($"[AnalyzingPhase] {_cachedSlots.Count} slots cacheados");
    }
    
    /// <summary>
    /// Inicia analyzing phase (chamado externamente).
    /// </summary>
    public void StartAnalyzing(System.Action onComplete = null)
    {
        if (_isAnalyzing)
        {
            _config?.DebugLog("[AnalyzingPhase] Já está analisando, ignorando...");
            return;
        }
        
        // Recachear slots se necessário
        if (_cachedSlots == null || _cachedSlots.Count == 0)
        {
            CacheSlots();
        }
        
        if (_analyzingCoroutine != null)
        {
            StopCoroutine(_analyzingCoroutine);
        }
        
        _analyzingCoroutine = StartCoroutine(AnalyzingRoutine(onComplete));
    }
    
    /// <summary>
    /// Coroutine principal de analyzing.
    /// </summary>
    private IEnumerator AnalyzingRoutine(System.Action onComplete)
    {
        _isAnalyzing = true;
        _config?.DebugLog("[AnalyzingPhase] INICIANDO análise dos slots...");
        
        // Obter slots para analisar (com filtro real)
        var slotsToAnalyze = GetSlotsToAnalyze();
        
        if (slotsToAnalyze.Count == 0)
        {
            _config?.DebugLog("[AnalyzingPhase] Nenhum slot para analisar (filtro ativo)");
            _isAnalyzing = false;
            onComplete?.Invoke();
            yield break;
        }
        
        _config?.DebugLog($"[AnalyzingPhase] Analisando {slotsToAnalyze.Count} slots...");
        
        // Analisar cada slot sequencialmente
        foreach (var slotView in slotsToAnalyze)
        {
            if (slotView != null)  // Safety check
            {
                yield return AnalyzeSingleSlot(slotView);
            }
        }
        
        _isAnalyzing = false;
        _config?.DebugLog("[AnalyzingPhase] ANÁLISE CONCLUÍDA!");
        
        // Callback para próxima fase
        onComplete?.Invoke();
        
        _analyzingCoroutine = null;
    }
    
    /// <summary>
    /// Retorna lista de slots para analisar (com filtragem real).
    /// </summary>
    private List<GridSlotView> GetSlotsToAnalyze()
    {
        var slots = new List<GridSlotView>();
        
        if (_cachedSlots == null) return slots;
        
        foreach (var slot in _cachedSlots)
        {
            if (slot == null) continue;  // Safety check
            
            // Filtro real: apenas slots com plantas
            if (_onlyPlantsSlots)
            {
                if (slot.HasPlant())
                {
                    slots.Add(slot);
                }
            }
            else
            {
                // Todos os slots
                slots.Add(slot);
            }
        }
        
        return slots;
    }
    
    /// <summary>
    /// Analisa um único slot (levitação + pulse).
    /// CORRIGIDO: Usa localPosition (funciona com Canvas/Grid hierarchy).
    /// </summary>
    private IEnumerator AnalyzeSingleSlot(GridSlotView slotView)
    {
        if (slotView == null) yield break;
        
        Transform slotTransform = slotView.transform;
        
        // Usar posição original salva (mais robusto)
        Vector3 originalLocalPos = _originalPositions.ContainsKey(slotView)
            ? _originalPositions[slotView]
            : slotTransform.localPosition;
        
        float halfDuration = _durationPerSlot * 0.5f;
        
        // Trigger pulse rosa (se habilitado)
        if (_showAnalyzingPulse && _config != null)
        {
            // Usar cor de analyzing pulse do GridVisualConfig
            // TODO: GridVisualConfig precisa ter analyzingPulseColor
            Color pulseColor = new Color(1f, 0.4f, 0.7f, 0.5f);  // Rosa placeholder
            slotView.TriggerAnalyzingPulse(pulseColor, _durationPerSlot);
        }
        
        // FASE 1: Subir (levitação) - USA LOCAL POSITION
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            // EaseOut para subida suave
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            
            float offsetY = Mathf.Lerp(0f, _levitationHeight, easedT);
            slotTransform.localPosition = originalLocalPos + Vector3.up * offsetY;
            
            yield return null;
        }
        
        // FASE 2: Descer (retorno) - USA LOCAL POSITION
        elapsed = 0f;
        Vector3 peakLocalPos = slotTransform.localPosition;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            // EaseIn para descida suave
            float easedT = Mathf.Pow(t, 2f);
            
            slotTransform.localPosition = Vector3.Lerp(peakLocalPos, originalLocalPos, easedT);
            
            yield return null;
        }
        
        // Garantir posição original
        slotTransform.localPosition = originalLocalPos;
        
        _config?.DebugLog($"[AnalyzingPhase] Slot {slotView.SlotIndex} analisado");
    }
    
    /// <summary>
    /// Para analyzing phase (com reset suave).
    /// </summary>
    public void StopAnalyzing()
    {
        if (_analyzingCoroutine != null)
        {
            StopCoroutine(_analyzingCoroutine);
            _analyzingCoroutine = null;
        }
        
        _isAnalyzing = false;
        
        // Resetar posições de todos os slots (suavemente)
        StartCoroutine(ResetAllSlotPositionsSmoothly());
    }
    
    /// <summary>
    /// Reseta posições de todos os slots (com Lerp suave).
    /// </summary>
    private IEnumerator ResetAllSlotPositionsSmoothly()
    {
        if (_cachedSlots == null) yield break;
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        // Salvar posições atuais
        var currentPositions = new Dictionary<GridSlotView, Vector3>();
        foreach (var slot in _cachedSlots)
        {
            if (slot != null)
            {
                currentPositions[slot] = slot.transform.localPosition;
            }
        }
        
        // Lerp para original
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            foreach (var slot in _cachedSlots)
            {
                if (slot != null && _originalPositions.ContainsKey(slot))
                {
                    slot.transform.localPosition = Vector3.Lerp(
                        currentPositions[slot],
                        _originalPositions[slot],
                        t
                    );
                }
            }
            
            yield return null;
        }
        
        // Garantir posições finais
        foreach (var slot in _cachedSlots)
        {
            if (slot != null && _originalPositions.ContainsKey(slot))
            {
                slot.transform.localPosition = _originalPositions[slot];
            }
        }
    }
    
    private void OnDestroy()
    {
        StopAnalyzing();
    }
}

