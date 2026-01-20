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
    
    [Tooltip("Altura da levitação ao analisar")]
    [Range(0.05f, 0.3f)]
    [SerializeField] private float _levitationHeight = 0.1f;
    
    [Tooltip("Analisar apenas slots com plantas? (otimizado)")]
    [SerializeField] private bool _onlyPlantsSlots = true;
    
    [Tooltip("Mostrar pulse rosa durante análise? (usa analyzing pulse do GridVisualConfig)")]
    [SerializeField] private bool _showAnalyzingPulse = true;
    
    // Estado
    private bool _isAnalyzing;
    private Coroutine _analyzingCoroutine;
    
    private void Start()
    {
        // Subscribe to sleep button (ou evento customizado)
        // TODO: Quando tiver evento de "antes de detect patterns", usar ele
        // Por enquanto, será chamado manualmente via PatternHighlightController
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
        
        if (_gridManager == null)
        {
            Debug.LogError("[AnalyzingPhaseController] GridManager não atribuído!");
            _isAnalyzing = false;
            onComplete?.Invoke();
            yield break;
        }
        
        // Obter slots para analisar
        var slotsToAnalyze = GetSlotsToAnalyze();
        
        _config?.DebugLog($"[AnalyzingPhase] Analisando {slotsToAnalyze.Count} slots...");
        
        // Analisar cada slot sequencialmente
        foreach (var slotView in slotsToAnalyze)
        {
            yield return AnalyzeSingleSlot(slotView);
        }
        
        _isAnalyzing = false;
        _config?.DebugLog("[AnalyzingPhase] ANÁLISE CONCLUÍDA!");
        
        // Callback para próxima fase
        onComplete?.Invoke();
        
        _analyzingCoroutine = null;
    }
    
    /// <summary>
    /// Retorna lista de slots para analisar.
    /// </summary>
    private List<GridSlotView> GetSlotsToAnalyze()
    {
        var slots = new List<GridSlotView>();
        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        foreach (var slot in allSlots)
        {
            // Se configurado para apenas plantas, verificar
            if (_onlyPlantsSlots)
            {
                // TODO: GridSlotView precisa expor HasPlant() ou similar
                // Por enquanto, vamos analisar todos os slots desbloqueados
                // Quando GridSlotView tiver API, filtrar aqui
                slots.Add(slot);
            }
            else
            {
                slots.Add(slot);
            }
        }
        
        return slots;
    }
    
    /// <summary>
    /// Analisa um único slot (levitação + pulse).
    /// </summary>
    private IEnumerator AnalyzeSingleSlot(GridSlotView slotView)
    {
        if (slotView == null) yield break;
        
        Transform slotTransform = slotView.transform;
        Vector3 originalPos = slotTransform.position;
        
        float halfDuration = _durationPerSlot * 0.5f;
        
        // FASE 1: Subir (levitação)
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            // EaseOut para subida suave
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            
            float offsetY = Mathf.Lerp(0f, _levitationHeight, easedT);
            slotTransform.position = originalPos + Vector3.up * offsetY;
            
            yield return null;
        }
        
        // FASE 2: Descer (retorno)
        elapsed = 0f;
        Vector3 peakPos = slotTransform.position;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            // EaseIn para descida suave
            float easedT = Mathf.Pow(t, 2f);
            
            slotTransform.position = Vector3.Lerp(peakPos, originalPos, easedT);
            
            yield return null;
        }
        
        // Garantir posição original
        slotTransform.position = originalPos;
        
        // TODO: Se _showAnalyzingPulse = true, triggerar pulse rosa no slot
        // Isso requer que GridSlotView exponha método de pulse
        // Por enquanto, a levitação é suficiente
        
        _config?.DebugLog($"[AnalyzingPhase] Slot {slotView.SlotIndex} analisado");
    }
    
    /// <summary>
    /// Para analyzing phase (se necessário cancelar).
    /// </summary>
    public void StopAnalyzing()
    {
        if (_analyzingCoroutine != null)
        {
            StopCoroutine(_analyzingCoroutine);
            _analyzingCoroutine = null;
        }
        
        _isAnalyzing = false;
        
        // Resetar posições de todos os slots
        ResetAllSlotPositions();
    }
    
    /// <summary>
    /// Reseta posições de todos os slots (cleanup).
    /// </summary>
    private void ResetAllSlotPositions()
    {
        if (_gridManager == null) return;
        
        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        foreach (var slot in allSlots)
        {
            // Resetar posição local (assume que slots estão em posição fixa no grid)
            slot.transform.localPosition = new Vector3(
                slot.transform.localPosition.x,
                0f,  // Y sempre 0 (sem offset)
                slot.transform.localPosition.z
            );
        }
    }
    
    private void OnDestroy()
    {
        StopAnalyzing();
    }
}
