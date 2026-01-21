using System.Collections;
using UnityEngine;

/// <summary>
/// Orquestra a fase de análise: levitação + detecção + eventos.
/// Dispara eventos que outros controllers escutam (highlight, popup, breathing).
/// </summary>
public class AnalyzingPhaseController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PatternTextPopupController _patternPopup;
    
    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_patternPopup == null)
        {
            _patternPopup = FindObjectOfType<PatternTextPopupController>();
        }
    }
    
    public IEnumerator AnalyzeAndGrowGrid(IGridService gridService, GameEvents events, RunData runData)
    {
        if (_gridManager == null || _config == null)
        {
            Debug.LogError("[AnalyzingPhase] Missing references!");
            yield break;
        }
        
        _config.DebugLog("Starting grid analysis");
        
        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        for (int i = 0; i < allSlots.Length; i++)
        {
            var slot = allSlots[i];
            if (slot == null) continue;
            
            int slotIndex = slot.SlotIndex;
            
            if (!gridService.IsSlotUnlocked(slotIndex)) continue;
            
            events.Grid.TriggerAnalyzeSlot(slotIndex);
            
            StartCoroutine(LevitateSlot(slot));
            
            var slotData = gridService.GetSlotReadOnly(slotIndex);
            bool hasCrop = slotData != null && slotData.CropID.IsValid;
            
            if (hasCrop && i + 1 < allSlots.Length)
            {
                var nextSlot = allSlots[i + 1];
                if (nextSlot != null)
                {
                    int nextSlotIndex = nextSlot.SlotIndex;
                    var nextSlotData = gridService.GetSlotReadOnly(nextSlotIndex);
                    bool nextHasCrop = nextSlotData != null && nextSlotData.CropID.IsValid;
                    
                    if (gridService.IsSlotUnlocked(nextSlotIndex) && nextHasCrop)
                    {
                        // Pattern found! Dispatch event
                        var tempMatch = CreatePatternMatch("Par Adjacente", 5, new System.Collections.Generic.List<int> { slotIndex, nextSlotIndex });
                        
                        events.Pattern.TriggerPatternSlotCompleted(tempMatch);
                        
                        if (_patternPopup != null)
                        {
                            StartCoroutine(_patternPopup.ShowPatternName(tempMatch));
                        }
                        
                        i++; // Skip next slot
                    }
                }
            }
            
            if (_config.analyzingSlotDelay > 0f)
            {
                yield return new WaitForSeconds(_config.analyzingSlotDelay);
            }
            else
            {
                yield return null;
            }
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
    
    private PatternMatch CreatePatternMatch(string displayName, int baseScore, System.Collections.Generic.List<int> slotIndices)
    {
        var match = PatternMatch.Create(
            patternID: "TEMP_" + displayName.ToUpper().Replace(" ", "_"),
            displayName: displayName,
            slotIndices: slotIndices,
            baseScore: baseScore,
            cropIDs: new System.Collections.Generic.List<CropID>()
        );
        
        match.SetTrackingData(daysActive: 1, hasRecreationBonus: false);
        return match;
    }
}
