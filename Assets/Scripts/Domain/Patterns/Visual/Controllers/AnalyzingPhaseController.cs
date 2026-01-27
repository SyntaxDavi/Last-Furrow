using System; 
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks; 

/// <summary>
/// Orquestra a fase de an�lise usando Strategy Pattern para detec��o de padr�es.
/// Dispara eventos que outros controllers escutam (highlight, popup, breathing).
/// SOLID: Single Responsibility - apenas orquestra, detec��o delegada aos detectores.
/// Vers�o: UniTask (Async/Await)
/// </summary>
public class AnalyzingPhaseController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;

    [Header("References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PatternUIManager _uiManager;

    private List<IPatternDetector> _detectors;
    private HashSet<int> _processedSlots;

    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }

        // ONDA 6.1: Remover FindFirstObjectByType - Validar apenas (atribuir no Inspector)
        if (_uiManager == null)
        {
            Debug.LogError("[AnalyzingPhaseController] PatternUIManager n�o atribu�do no Inspector!");
        }

        // Obter todos os detectores da factory
        _detectors = PatternDetectorFactory.GetAllDetectors();
        _processedSlots = new HashSet<int>();
    }

    public async UniTask AnalyzeAndGrowGrid(IGridService gridService, GameEvents events, RunData runData)
    {
        if (_gridManager == null || _config == null)
        {
            Debug.LogError("[AnalyzingPhase] Missing references!");
            return; // Substitui yield break
        }

        _config.DebugLog("Starting grid analysis with pattern detection");

        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();
        int[] allSlotIndices = new int[allSlots.Length];

        for (int i = 0; i < allSlots.Length; i++)
        {
            allSlotIndices[i] = allSlots[i].SlotIndex;
        }

        // ? CORRE��O CR�TICA: Processar TODOS os slots desbloqueados PRIMEIRO
        // Isso garante que �gua seja removida de TODOS os slots, n�o apenas dos que est�o em padr�es
        for (int i = 0; i < allSlotIndices.Length; i++)
        {
            int slotIndex = allSlotIndices[i];
            if (gridService.IsSlotUnlocked(slotIndex))
            {
                // Processa ciclo noturno (remove �gua, cresce plantas, etc.)
                gridService.ProcessNightCycleForSlot(slotIndex);
            }
        }

        _processedSlots.Clear();
        var foundPatterns = new List<PatternMatch>();
        int totalPoints = 0;

        // Analisar cada slot para padr�es e pontos passivos
        for (int i = 0; i < allSlots.Length; i++)
        {
            var slot = allSlots[i];
            if (slot == null) continue;

            int slotIndex = slot.SlotIndex;

            // Pular se j� processado em padr�o anterior
            if (_processedSlots.Contains(slotIndex)) continue;

            if (!gridService.IsSlotUnlocked(slotIndex)) continue;

            events.Grid.TriggerAnalyzeSlot(slotIndex);

            // ONDA 6.5: Verificar se slot tem crop e dar pontos passivos PRIMEIRO
            await ProcessCropPassiveScore(slotIndex, gridService, runData, events, slot);

            // AGUARDAR levita��o terminar (pipeline sincronizado)
            await LevitateSlot(slot);

            // Tentar detectar padr�es neste slot (ordem de prioridade)
            PatternMatch foundPattern = null;

            foreach (var detector in _detectors)
            {
                if (detector.CanDetectAt(gridService, slotIndex))
                {
                    foundPattern = detector.DetectAt(gridService, slotIndex, allSlotIndices);

                    if (foundPattern != null)
                    {
                        _config.DebugLog($"Pattern found: {foundPattern.DisplayName} at slot {slotIndex}");

                        // Marcar slots deste padr�o como processados
                        foreach (int processedSlot in foundPattern.SlotIndices)
                        {
                            _processedSlots.Add(processedSlot);
                        }

                        // Adicionar � lista
                        foundPatterns.Add(foundPattern);
                        totalPoints += foundPattern.BaseScore;
                        
                        // UI atualiza contador ENQUANTO popup aparece
                        int newTotalScore = runData.CurrentWeeklyScore + totalPoints;
                        events.Pattern.TriggerScoreIncremented(
                            foundPattern.BaseScore,  // Pontos deste padr�o
                            newTotalScore,           // Novo total (previs�o)
                            runData.WeeklyGoalTarget // Meta
                        );

                        // Disparar evento para highlights/popup
                        events.Pattern.TriggerPatternSlotCompleted(foundPattern);

                        // AGUARDAR popup terminar (pipeline sincronizado)
                        if (_uiManager != null)
                        {
                            Debug.Log($"[AnalyzingPhaseController] ?? Showing popup via UIManager: {foundPattern.DisplayName}");

                            // Como atualizamos o PatternUIManager para UniTask, basta awaitar direto
                            await _uiManager.ShowPatternPopupRoutine(foundPattern);
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

                        // Apenas 1 padr�o por slot (maior prioridade)
                        break;
                    }
                }
            }

            // Delay configur�vel entre slots
            if (_config.analyzingSlotDelay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_config.analyzingSlotDelay));
            }
            else
            {
                // Sempre bom ceder o controle por 1 frame se n�o houver delay, para evitar travamento
                await UniTask.Yield();
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

    private async UniTask LevitateSlot(GridSlotView slot)
    {
        if (slot == null) return;

        Vector3 originalPos = slot.transform.localPosition;
        float duration = _config.levitationDuration;
        float height = _config.levitationHeight;

        // Fase de subida
        float elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            slot.transform.localPosition = originalPos + Vector3.up * (height * t);

            // Espera o pr�ximo frame
            await UniTask.Yield();
        }

        // Fase de descida
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            slot.transform.localPosition = Vector3.Lerp(originalPos + Vector3.up * height, originalPos, t);

            await UniTask.Yield();
        }

        slot.transform.localPosition = originalPos;
    }
    
    /// <summary>
    /// ONDA 6.5: Processa pontos passivos de crop individual.
    /// Mostra popup pequeno "+X" em cima do slot e dispara evento.
    /// </summary>
    private UniTask ProcessCropPassiveScore(
        int slotIndex, 
        IGridService gridService, 
        RunData runData, 
        GameEvents events,
        GridSlotView slotView)
    {
        // Pegar estado do slot
        if (slotIndex < 0 || slotIndex >= runData.GridSlots.Length) return UniTask.CompletedTask;
        
        var slotState = runData.GridSlots[slotIndex];
        
        // Verificar se tem crop plantado (CropState usa CropID diretamente)
        if (!slotState.CropID.IsValid || slotState.CropID == CropID.Empty) return UniTask.CompletedTask;
        
        // Pegar dados do crop via GameLibrary
        if (!AppCore.Instance.GameLibrary.TryGetCrop(slotState.CropID, out CropData cropData)) return UniTask.CompletedTask;
        if (cropData == null) return UniTask.CompletedTask;
        
        // Calcular pontos passivos (BasePassiveScore)
        int passivePoints = cropData.BasePassiveScore;
        
        // Aplicar multiplicador se maduro
        if (slotState.CurrentGrowth >= cropData.DaysToMature && !slotState.IsWithered)
        {
            passivePoints = Mathf.RoundToInt(passivePoints * cropData.MatureScoreMultiplier);
        }
        
        if (passivePoints <= 0) return UniTask.CompletedTask; // Sem pontos
        
        // Calcular novo total (previso)
        int newTotal = runData.CurrentWeeklyScore + passivePoints;
        
        // Disparar evento para HUD atualizar
        events.Grid.TriggerCropPassiveScore(slotIndex, passivePoints, newTotal, runData.WeeklyGoalTarget);
        
        // ONDA 6.5: Mostrar pontos no prprio slot (TextMeshPro local)
        if (slotView != null)
        {
            slotView.ShowPassiveScore(passivePoints);
        }
        
        Debug.Log($"[AnalyzingPhase] Crop em slot {slotIndex} deu +{passivePoints} pontos");
        return UniTask.CompletedTask;
    }
}
