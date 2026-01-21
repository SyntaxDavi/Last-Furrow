using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquestra a fase de análise usando Strategy Pattern para detecção de padrões.
/// Dispara eventos que outros controllers escutam (highlight, popup, breathing).
/// SOLID: Single Responsibility - apenas orquestra, detecção delegada aos detectores.
/// </summary>
public class AnalyzingPhaseController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PatternUIManager _uiManager;  // WRAPPER para popups
    
    private List<IPatternDetector> _detectors;
    private HashSet<int> _processedSlots;
    
    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_uiManager == null)
        {
            _uiManager = FindFirstObjectByType<PatternUIManager>();
        }
        
        // Obter todos os detectores da factory
        _detectors = PatternDetectorFactory.GetAllDetectors();
        _processedSlots = new HashSet<int>();
    }
    
    public IEnumerator AnalyzeAndGrowGrid(IGridService gridService, GameEvents events, RunData runData)
    {
        if (_gridManager == null || _config == null)
        {
            Debug.LogError("[AnalyzingPhase] Missing references!");
            yield break;
        }
        
        _config.DebugLog("Starting grid analysis with pattern detection");
        
        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        int[] allSlotIndices = new int[allSlots.Length];
        
        for (int i = 0; i < allSlots.Length; i++)
        {
            allSlotIndices[i] = allSlots[i].SlotIndex;
        }
        
        _processedSlots.Clear();
        var foundPatterns = new List<PatternMatch>();
        int totalPoints = 0;
        
        // Analisar cada slot
        for (int i = 0; i < allSlots.Length; i++)
        {
            var slot = allSlots[i];
            if (slot == null) continue;
            
            int slotIndex = slot.SlotIndex;
            
            // Pular se já processado em padrão anterior
            if (_processedSlots.Contains(slotIndex)) continue;
            
            if (!gridService.IsSlotUnlocked(slotIndex)) continue;
            
            events.Grid.TriggerAnalyzeSlot(slotIndex);
            
            StartCoroutine(LevitateSlot(slot));
            
            // Tentar detectar padrões neste slot (ordem de prioridade)
            PatternMatch foundPattern = null;
            
            foreach (var detector in _detectors)
            {
                if (detector.CanDetectAt(gridService, slotIndex))
                {
                    foundPattern = detector.DetectAt(gridService, slotIndex, allSlotIndices);
                    
                    if (foundPattern != null)
                    {
                        _config.DebugLog($"Pattern found: {foundPattern.DisplayName} at slot {slotIndex}");
                        
                        // Marcar slots deste padrão como processados
                        foreach (int processedSlot in foundPattern.SlotIndices)
                        {
                            _processedSlots.Add(processedSlot);
                        }
                        
                        // Adicionar à lista
                        foundPatterns.Add(foundPattern);
                        totalPoints += foundPattern.BaseScore;
                        
                        // Disparar evento para highlights/popup
                        events.Pattern.TriggerPatternSlotCompleted(foundPattern);
                        
                        // HARDCODE: Chamar popup via UIManager (resolve problema de GameObject inativo)
                        if (_uiManager != null)
                        {
                            Debug.Log($"[AnalyzingPhaseController] ?? Showing popup via UIManager: {foundPattern.DisplayName}");
                            _uiManager.ShowPatternPopupDirect(foundPattern);
                        }
                        else
                        {
                            Debug.LogWarning("[AnalyzingPhaseController] ?? UIManager is null!");
                        }
                        
                        // Disparar eventos de decay/recreation
                        if (foundPattern.DaysActive > 1)
                        {
                            float decayMultiplier = Mathf.Pow(0.9f, foundPattern.DaysActive - 1);
                            events.Pattern.TriggerPatternDecayApplied(
                                foundPattern, 
                                foundPattern.DaysActive, 
                                decayMultiplier
                            );
                            
                            _config.DebugLog($"[Decay] Pattern {foundPattern.DisplayName} has {foundPattern.DaysActive} days active, multiplier: {decayMultiplier:F2}");
                        }
                        
                        if (foundPattern.HasRecreationBonus)
                        {
                            events.Pattern.TriggerPatternRecreated(foundPattern);
                            _config.DebugLog($"[Recreation] Pattern {foundPattern.DisplayName} recreated with +10% bonus!");
                        }
                        
                        // Apenas 1 padrão por slot (maior prioridade)
                        break;
                    }
                }
            }
            
            // Delay configurável
            if (_config.analyzingSlotDelay > 0f)
            {
                yield return new WaitForSeconds(_config.analyzingSlotDelay);
            }
            else
            {
                yield return null;
            }
        }
        
        // Disparar evento geral para breathing
        if (foundPatterns.Count > 0)
        {
            events.Pattern.TriggerPatternsDetected(foundPatterns, totalPoints);
            _config.DebugLog($"Breathing event dispatched: {foundPatterns.Count} patterns, {totalPoints} points");
        }
        
        _config.DebugLog("Grid analysis complete");
    }
    
    
    private IEnumerator LevitateSlot(GridSlotView slot)
    {
        if (slot == null) yield break;
        
        Vector3 originalPos = slot.transform.localPosition;
        float duration = _config.levitationDuration;
        float height = _config.levitationHeight;
        
        float elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            slot.transform.localPosition = originalPos + Vector3.up * (height * t);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            slot.transform.localPosition = Vector3.Lerp(originalPos + Vector3.up * height, originalPos, t);
            yield return null;
        }
        
        slot.transform.localPosition = originalPos;
    }
}
