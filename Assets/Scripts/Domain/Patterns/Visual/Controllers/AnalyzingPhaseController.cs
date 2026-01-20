using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// HARDCODE BRUTAÇO - FAZ TUDO EM UMA PANCADA!
/// 
/// O QUE FAZ:
/// 1. Passa por cada slot DO GRID (levita)
/// 2. CRESCE a planta (ProcessNightCycle)
/// 3. SE encontrar 2 crops LADO A LADO ? PARA
/// 4. FAZ os 2 slots BRILHAREM (verde)
/// 5. MOSTRA pop-up "ADJACENT PAIR! +5"
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
    
    // Pop-up hardcode
    [SerializeField] private TextMeshProUGUI _popupText;
    [SerializeField] private CanvasGroup _popupCanvasGroup;
    
    private void Start()
    {
        if (_popupCanvasGroup != null)
        {
            _popupCanvasGroup.alpha = 0f;
        }
    }
    
    /// <summary>
    /// HARDCODE MEGA: Levita, cresce, detecta, mostra popup TUDO JUNTO!
    /// </summary>
    public IEnumerator AnalyzeAndGrowGrid(IGridService gridService, GameEvents events, RunData runData)
    {
        Debug.Log("=== HARDCODE MEGA: Iniciando análise COMPLETA ===");
        
        if (_gridManager == null)
        {
            Debug.LogError("GridManager NULL!");
            yield break;
        }
        
        // Pegar TODOS os slots
        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        Debug.Log($"Total de slots: {allSlots.Length}");
        
        // Passar por cada slot
        for (int i = 0; i < allSlots.Length; i++)
        {
            var slot = allSlots[i];
            
            if (slot == null) continue;
            
            int slotIndex = slot.SlotIndex;
            
            // Verificar se está desbloqueado
            if (!gridService.IsSlotUnlocked(slotIndex))
            {
                continue;
            }
            
            Debug.Log($"?? Analisando slot {i} (Index: {slotIndex})");
            
            // EVENTO: Slot sendo analisado
            events.Grid.TriggerAnalyzeSlot(slotIndex);
            
            // LEVITAR slot atual (cosmético)
            StartCoroutine(LevitateSlot(slot));
            
            // CRESCER/PROCESSAR SLOT (lógica de jogo)
            gridService.ProcessNightCycleForSlot(slotIndex);
            
            // HARDCODE: Verificar se TEM CROP APÓS PROCESSAMENTO
            bool hasCrop = slot.HasPlant();
            
            if (hasCrop)
            {
                Debug.Log($"  ? Slot {i} TEM CROP!");
                
                // HARDCODE: Verificar se próximo slot TAMBÉM tem crop (par horizontal)
                if (i + 1 < allSlots.Length)
                {
                    var nextSlot = allSlots[i + 1];
                    int nextSlotIndex = nextSlot != null ? nextSlot.SlotIndex : -1;
                    
                    if (nextSlot != null && gridService.IsSlotUnlocked(nextSlotIndex) && nextSlot.HasPlant())
                    {
                        Debug.Log($"  ?? PAR ADJACENTE ENCONTRADO! Slots {i} e {i+1}");
                        
                        // FAZER BRILHAR OS 2 SLOTS (VERDE)
                        StartCoroutine(HighlightSlotHardcoded(slot, Color.green));
                        StartCoroutine(HighlightSlotHardcoded(nextSlot, Color.green));
                        
                        // MOSTRAR POP-UP
                        StartCoroutine(ShowPopupHardcoded("ADJACENT PAIR!\n+5 pontos"));
                        
                        // PARAR aqui por um tempo (para player ver)
                        yield return new WaitForSeconds(_highlightDuration);
                        
                        // Pular próximo slot (já foi processado visualmente)
                        // MAS ainda precisa crescer!
                        i++;
                        events.Grid.TriggerAnalyzeSlot(nextSlotIndex);
                        gridService.ProcessNightCycleForSlot(nextSlotIndex);
                    }
                }
            }
            
            // Delay antes do próximo slot
            yield return new WaitForSeconds(_delayPerSlot);
        }
        
        Debug.Log("=== HARDCODE MEGA: Análise COMPLETA concluída ===");
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
        if (slot == null) yield break;
        
        float duration = _highlightDuration;
        float elapsed = 0f;
        
        Debug.Log($"?? Brilhando slot {slot.SlotIndex} em {color}");
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Pulse (pingpong alpha)
            float t = Mathf.PingPong(elapsed * 2f, 1f);
            Color pulsedColor = color;
            pulsedColor.a = Mathf.Lerp(0.3f, 0.8f, t);
            
            // Aplicar
            slot.SetPatternHighlight(pulsedColor, true);
            
            yield return null;
        }
        
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
    }
    
    /// <summary>
    /// HARDCODE: Mostra pop-up de texto na tela.
    /// </summary>
    private IEnumerator ShowPopupHardcoded(string text)
    {
        if (_popupText == null || _popupCanvasGroup == null)
        {
            Debug.LogWarning("?? Pop-up não configurado no Inspector!");
            yield break;
        }
        
        Debug.Log($"?? POP-UP: {text}");
        
        // Configurar texto
        _popupText.text = text;
        _popupText.color = Color.green;
        
        // Fade IN
        float fadeInDuration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _popupCanvasGroup.alpha = elapsed / fadeInDuration;
            yield return null;
        }
        
        _popupCanvasGroup.alpha = 1f;
        
        // HOLD
        yield return new WaitForSeconds(1.5f);
        
        // Fade OUT
        float fadeOutDuration = 0.3f;
        elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _popupCanvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
            yield return null;
        }
        
        _popupCanvasGroup.alpha = 0f;
    }
}
