using System.Collections;
using UnityEngine;

/// <summary>
/// HARDCODE BRUTAÇO - FAZ TUDO EM UMA PANCADA!
/// 
/// O QUE FAZ:
/// 1. Passa por cada slot DO GRID (levita)
/// 2. CRESCE a planta (ProcessNightCycle)
/// 3. SE encontrar 2 crops LADO A LADO ? PARA
/// 4. FAZ os 2 slots BRILHAREM (verde)
/// 5. MOSTRA pop-up usando PatternTextPopupController
/// 6. Continua...
/// 
/// LEVITAÇÃO + CRESCIMENTO + DETECÇÃO + POPUP = TUDO JUNTO!
/// </summary>
public class AnalyzingPhaseController : MonoBehaviour
{
    [Header("HARDCODE Settings")]
    [SerializeField] private float _delayPerSlot = 0.3f;
    [SerializeField] private float _highlightDuration = 2f;
    [SerializeField] private GridManager _gridManager;
    
    [Header("Pattern Popup")]
    [SerializeField] private PatternTextPopupController _patternPopup;
    
    private void Awake()
    {
        Debug.Log("[AnalyzingPhase] ========== AWAKE ==========");
        
        if (_patternPopup == null)
        {
            Debug.LogWarning("[AnalyzingPhase] PatternTextPopupController não atribuído! Procurando...");
            _patternPopup = FindObjectOfType<PatternTextPopupController>();
            Debug.Log($"[AnalyzingPhase] PatternPopup encontrado? {_patternPopup != null}");
        }
        else
        {
            Debug.Log($"[AnalyzingPhase] PatternPopup JÁ atribuído");
        }
        
        Debug.Log("[AnalyzingPhase] ========== AWAKE CONCLUÍDO ==========");
    }
    
    /// <summary>
    /// HARDCODE MEGA: Levita, cresce, detecta, mostra popup TUDO JUNTO!
    /// </summary>
    public IEnumerator AnalyzeAndGrowGrid(IGridService gridService, GameEvents events, RunData runData)
    {
        Debug.Log("====================================================================");
        Debug.Log("=== HARDCODE MEGA: Iniciando análise COMPLETA ===");
        Debug.Log("====================================================================");
        
        if (_gridManager == null)
        {
            Debug.LogError("? GridManager NULL!");
            yield break;
        }
        
        // DEBUG: Verificar se PatternTextPopupController está configurado
        Debug.Log($"?? UI CONFIG CHECK:");
        Debug.Log($"   _patternPopup = {(_patternPopup != null ? "? OK" : "? NULL")}");
        
        // Pegar TODOS os slots
        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        Debug.Log($"?? Total de slots no grid: {allSlots.Length}");
        
        // Passar por cada slot
        for (int i = 0; i < allSlots.Length; i++)
        {
            var slot = allSlots[i];
            
            if (slot == null)
            {
                Debug.LogWarning($"?? Slot {i} é NULL, pulando...");
                continue;
            }
            
            int slotIndex = slot.SlotIndex;
            
            // Verificar se está desbloqueado
            if (!gridService.IsSlotUnlocked(slotIndex))
            {
                Debug.Log($"?? Slot {i} (Index: {slotIndex}) está BLOQUEADO, pulando...");
                continue;
            }
            
            Debug.Log($"??????????????????????????????????????????");
            Debug.Log($"?? ANALISANDO SLOT {i} (Index: {slotIndex})");
            
            // EVENTO: Slot sendo analisado
            events.Grid.TriggerAnalyzeSlot(slotIndex);
            
            // LEVITAR slot atual (cosmético)
            StartCoroutine(LevitateSlot(slot));
            
            // ?? VERIFICAR DADOS REAIS (não visual!), pois plantas ainda não cresceram visualmente
            var slotData = gridService.GetSlotReadOnly(slotIndex);
            bool hasCrop = slotData != null && slotData.CropID.IsValid;
            
            Debug.Log($"   ?? Slot {i} tem crop nos DADOS? {(hasCrop ? "? SIM (" + slotData.CropID.ToString() + ")" : "? NÃO")}");
            
            if (hasCrop)
            {
                Debug.Log($"   ? SLOT {i} TEM CROP! Verificando adjacente...");
                
                // FAZER BRILHAR SLOT ATUAL (TIER 1 - PRATA)
                Color tier1Color = new Color(0.75f, 0.75f, 0.75f, 1f); // Prata
                Debug.Log($"   ?? Aplicando brilho TIER 1 (prata) no slot {i}");
                StartCoroutine(HighlightSlotHardcoded(slot, tier1Color));
                
                // HARDCODE: Verificar se próximo slot TAMBÉM tem crop (par horizontal)
                if (i + 1 < allSlots.Length)
                {
                    var nextSlot = allSlots[i + 1];
                    
                    if (nextSlot == null)
                    {
                        Debug.LogWarning($"   ?? Próximo slot ({i+1}) é NULL");
                    }
                    else
                    {
                        int nextSlotIndex = nextSlot.SlotIndex;
                        bool nextIsUnlocked = gridService.IsSlotUnlocked(nextSlotIndex);
                        var nextSlotData = gridService.GetSlotReadOnly(nextSlotIndex);
                        bool nextHasCrop = nextSlotData != null && nextSlotData.CropID.IsValid; // Verificar dados reais!
                        
                        Debug.Log($"   ?? PRÓXIMO SLOT {i+1}:");
                        Debug.Log($"      - Index: {nextSlotIndex}");
                        Debug.Log($"      - Desbloqueado? {(nextIsUnlocked ? "? SIM" : "? NÃO")}");
                        Debug.Log($"      - Tem crop nos DADOS? {(nextHasCrop ? "? SIM (" + nextSlotData.CropID.ToString() + ")" : "? NÃO")}");
                        
                        if (nextIsUnlocked && nextHasCrop)
                        {
                            Debug.Log($"");
                            Debug.Log($"?????? PAR ADJACENTE ENCONTRADO! Slots {i} e {i+1} ??????");
                            Debug.Log($"");
                            
                            // FAZER BRILHAR OS 2 SLOTS (VERDE - mais vibrante)
                            Color adjacentColor = new Color(0f, 1f, 0f, 1f); // Verde vibrante
                            Debug.Log($"   ?? Aplicando brilho VERDE VIBRANTE nos slots {i} e {i+1}");
                            StartCoroutine(HighlightSlotHardcoded(slot, adjacentColor));
                            StartCoroutine(HighlightSlotHardcoded(nextSlot, adjacentColor));
                            
                            // MOSTRAR POP-UP usando PatternTextPopupController
                            if (_patternPopup != null)
                            {
                                Debug.Log($"   ?? Mostrando popup através do PatternTextPopupController...");
                                // Criar PatternMatch temporário para o popup
                                var tempMatch = CreateTempPatternMatch("Par Adjacente", 5);
                                StartCoroutine(_patternPopup.ShowPatternName(tempMatch));
                            }
                            else
                            {
                                Debug.LogWarning($"   ?? PatternTextPopupController não disponível!");
                            }
                            
                            // NÃO PARAR - deixar as animações rodarem em paralelo
                            Debug.Log($"   ? Continuando imediatamente (animações em paralelo)");
                        
                        // Pular próximo slot (já foi processado visualmente)
                        Debug.Log($"   ?? Pulando para slot {i+2} (próximo par já foi analisado)");
                        i++;
                            
                            Debug.Log($"   ? Par adjacente processado completamente!");
                        }
                        else
                        {
                            Debug.Log($"   ?? Próximo slot NÃO forma par adjacente");
                        }
                    }
                }
                else
                {
                    Debug.Log($"   ?? Slot {i} é o último, não há próximo para verificar");
                }
            }
            
            // Delay reduzido antes do próximo slot (apenas para não sobrecarregar)
            yield return null; // Apenas um frame
        }
        
        Debug.Log("====================================================================");
        Debug.Log("=== HARDCODE MEGA: Análise COMPLETA concluída ===");
        Debug.Log("====================================================================");
    }
    
    /// <summary>
    /// HARDCODE: Levita slot (cosmético).
    /// </summary>
    private IEnumerator LevitateSlot(GridSlotView slot)
    {
        if (slot == null) yield break;
        
        Vector3 originalPos = slot.transform.localPosition;
        float duration = 0.2f;
        float height = 0.1f;
        
        // Subir
        float elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            
            slot.transform.localPosition = originalPos + Vector3.up * (height * t);
            yield return null;
        }
        
        // Descer
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
    
    /// <summary>
    /// HARDCODE: Faz slot BRILHAR por X segundos.
    /// </summary>
    private IEnumerator HighlightSlotHardcoded(GridSlotView slot, Color color)
    {
        if (slot == null)
        {
            Debug.LogError("? HighlightSlotHardcoded: slot é NULL!");
            yield break;
        }
        
        float duration = _highlightDuration;
        float elapsed = 0f;
        
        Debug.Log($"?? HighlightSlotHardcoded INICIADO:");
        Debug.Log($"   - Slot Index: {slot.SlotIndex}");
        Debug.Log($"   - Cor: {color}");
        Debug.Log($"   - Duração: {duration}s");
        
        int frameCount = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Pulse (pingpong alpha)
            float t = Mathf.PingPong(elapsed * 2f, 1f);
            Color pulsedColor = color;
            pulsedColor.a = Mathf.Lerp(0.3f, 0.8f, t);
            
            // Aplicar
            slot.SetPatternHighlight(pulsedColor, true);
            
            // Debug a cada 30 frames
            frameCount++;
            if (frameCount % 30 == 0)
            {
                Debug.Log($"   ?? Frame {frameCount}: alpha={pulsedColor.a:F2}, elapsed={elapsed:F2}s");
            }
            
            yield return null;
        }
        
        Debug.Log($"   ?? Pulse concluído, iniciando fade out...");
        
        // Fade out
        float fadeElapsed = 0f;
        float fadeDuration = 0.3f;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration;
            
            Color fadedColor = color;
            fadedColor.a = Mathf.Lerp(0.8f, 0f, t);
            
            slot.SetPatternHighlight(fadedColor, true);
            yield return null;
        }
        
        // Limpar
        slot.ClearPatternHighlight();
        Debug.Log($"   ? Highlight concluído e limpo para slot {slot.SlotIndex}");
    }
    
    
    
    /// <summary>
    /// Cria um PatternMatch temporário para testes/hardcoded.
    /// </summary>
    private PatternMatch CreateTempPatternMatch(string displayName, int baseScore)
    {
        // Usar o factory method do PatternMatch
        var match = PatternMatch.Create(
            patternID: "TEMP_" + displayName.ToUpper().Replace(" ", "_"),
            displayName: displayName,
            slotIndices: new System.Collections.Generic.List<int>(),
            baseScore: baseScore,
            cropIDs: new System.Collections.Generic.List<CropID>(),
            debugDescription: "Hardcoded test pattern"
        );
        
        // Configurar tracking data inicial
        match.SetTrackingData(daysActive: 1, hasRecreationBonus: false);
        
        return match;
    }
}
