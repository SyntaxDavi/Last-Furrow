using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scanner INCREMENTAL que verifica grid slot-por-slot.
/// 
/// RESPONSABILIDADE (SOLID):
/// - Iterar slots desbloqueados sequencialmente
/// - Consultar padrões no PatternDetectionCache
/// - Disparar OnPatternSlotCompleted uma vez por padrão
/// - Pausa mínima entre slots (timing/performance)
/// 
/// FILOSOFIA:
/// - Single Responsibility: Apenas scan incremental
/// - Não faz visual (dispara eventos para UI)
/// - Usa HashSet para evitar disparar mesmo padrão 2x
/// 
/// FLOW:
/// GridFullVerification (armazena) ? GridSlotScanner (itera) ? PatternEvents (dispara)
/// </summary>
public class GridSlotScanner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Pausa entre verificação de slots (segundos)")]
    [Range(0f, 0.1f)]
    [SerializeField] private float _slotScanDelay = 0.001f;
    
    [Header("Dependencies")]
    [Tooltip("GridManager para acessar slots")]
    [SerializeField] private GridManager _gridManager;
    
    [Tooltip("UIManager para aguardar popups terminarem")]
    [SerializeField] private PatternUIManager _uiManager;
    
    // HashSet para evitar disparar mesmo padrão múltiplas vezes
    private HashSet<PatternMatch> _patternsAlreadyTriggered = new HashSet<PatternMatch>();
    
    /// <summary>
    /// Escaneia grid slot-por-slot (apenas desbloqueados).
    /// Dispara OnPatternSlotCompleted quando encontra padrão completo.
    /// </summary>
    public IEnumerator ScanSequentially()
    {
        Debug.Log("[GridSlotScanner] ========== INICIANDO SCAN INCREMENTAL ==========");
        
        // Validações
        if (_gridManager == null)
        {
            Debug.LogError("[GridSlotScanner] GridManager não atribuído!");
            yield break;
        }
        
        // Cachear UIManager se não atribuído
        if (_uiManager == null)
        {
            _uiManager = Object.FindFirstObjectByType<PatternUIManager>();
            if (_uiManager == null)
            {
                Debug.LogWarning("[GridSlotScanner] PatternUIManager não encontrado, popups não aguardarão");
            }
        }
        
        if (PatternDetectionCache.Instance == null)
        {
            Debug.LogError("[GridSlotScanner] PatternDetectionCache não disponível!");
            yield break;
        }
        
        if (!PatternDetectionCache.Instance.HasPatterns())
        {
            Debug.Log("[GridSlotScanner] Nenhum padrão no cache, scan cancelado");
            yield break;
        }
        
        // Limpar padrões já disparados (nova verificação)
        _patternsAlreadyTriggered.Clear();
        
        // Obter slots desbloqueados
        var unlockedSlots = GetUnlockedSlots();
        
        if (unlockedSlots.Count == 0)
        {
            Debug.LogWarning("[GridSlotScanner] Nenhum slot desbloqueado encontrado!");
            yield break;
        }
        
        Debug.Log($"[GridSlotScanner] Verificando {unlockedSlots.Count} slots desbloqueados...");
        
        int patternsTriggered = 0;
        
        // Iterar slots sequencialmente
        foreach (var slotView in unlockedSlots)
        {
            if (slotView == null) continue;
            
            int slotIndex = slotView.SlotIndex;
            
            // Consultar padrões que incluem este slot
            var patternsForSlot = PatternDetectionCache.Instance.GetPatternsForSlot(slotIndex);
            
            // Disparar evento para cada padrão (se ainda não foi disparado)
            foreach (var pattern in patternsForSlot)
            {
                if (!_patternsAlreadyTriggered.Contains(pattern))
                {
                    // Disparar evento (para highlights e outros listeners)
                    AppCore.Instance?.Events.Pattern.TriggerPatternSlotCompleted(pattern);
                    
                    // ONDA 6.0: Aguardar popup terminar (pipeline sincronizado)
                    if (_uiManager != null)
                    {
                        yield return _uiManager.ShowPatternPopupRoutine(pattern);
                    }
                    
                    _patternsAlreadyTriggered.Add(pattern);
                    patternsTriggered++;
                    
                    Debug.Log($"[GridSlotScanner] Padrão encontrado no slot {slotIndex}: {pattern.DisplayName}");
                }
            }
            
            // Pausa mínima entre slots
            if (_slotScanDelay > 0f)
            {
                yield return new WaitForSeconds(_slotScanDelay);
            }
        }
        
        Debug.Log($"[GridSlotScanner] Scan concluído: {patternsTriggered} padrões disparados");
        Debug.Log("[GridSlotScanner] ========== FIM DO SCAN INCREMENTAL ==========");
        
        // Limpar HashSet após scan (preparar para próxima verificação)
        _patternsAlreadyTriggered.Clear();
    }
    
    /// <summary>
    /// Retorna lista de slots desbloqueados no grid.
    /// </summary>
    private List<GridSlotView> GetUnlockedSlots()
    {
        var unlockedSlots = new List<GridSlotView>();
        
        if (_gridManager == null)
        {
            return unlockedSlots;
        }
        
        // Obter RunData para verificar desbloqueio
        var saveData = AppCore.Instance?.SaveManager?.Data;
        if (saveData == null || saveData.CurrentRun == null)
        {
            Debug.LogWarning("[GridSlotScanner] RunData não disponível, usando todos os slots");
            
            // Fallback: retornar todos os slots
            var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
            unlockedSlots.AddRange(allSlots);
            return unlockedSlots;
        }
        
        var slotStates = saveData.CurrentRun.SlotStates;
        var allSlotViews = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        foreach (var slotView in allSlotViews)
        {
            if (slotView == null) continue;
            
            int slotIndex = slotView.SlotIndex;
            
            // Verificar se slot está desbloqueado
            if (slotIndex >= 0 && slotIndex < slotStates.Length)
            {
                if (slotStates[slotIndex].IsUnlocked)
                {
                    unlockedSlots.Add(slotView);
                }
            }
        }
        
        return unlockedSlots;
    }
    
    /// <summary>
    /// Para scan em andamento (se necessário).
    /// </summary>
    public void StopScan()
    {
        StopAllCoroutines();
        _patternsAlreadyTriggered.Clear();
        Debug.Log("[GridSlotScanner] Scan interrompido");
    }
}
