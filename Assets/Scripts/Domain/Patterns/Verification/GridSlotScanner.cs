using Cysharp.Threading.Tasks; 
using System; 
using System.Collections.Generic;
using System.Threading; 
using UnityEngine;

public class GridSlotScanner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Pausa entre verificação de slots (segundos)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float _slotScanDelay = 0.001f;

    [Header("Dependencies")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PatternUIManager _uiManager;

    // Controle de cancelamento (substitui StopAllCoroutines)
    private CancellationTokenSource _cts;

    private HashSet<PatternMatch> _patternsAlreadyTriggered = new HashSet<PatternMatch>();

    /// <summary>
    /// Escaneia grid slot-por-slot usando UniTask (Async/Await).
    /// ONDA 6.4: Recebe RunData para disparar pontos incrementais.
    /// </summary>
    public async UniTask ScanSequentially(RunData runData, GameEvents events)
    {
        Debug.Log("[GridSlotScanner] ========== INICIANDO SCAN INCREMENTAL ==========");

        // 1. Configurar Cancelamento (Segurança)
        // Se já houver um scan rodando, cancela ele antes de começar o novo
        if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
        _cts = new CancellationTokenSource();
        // O token é o que passamos para os "awaits" saberem que devem parar se mandarmos
        var token = _cts.Token;

        // --- VALIDAÇÕES (yield break vira return) ---
        if (_gridManager == null)
        {
            Debug.LogError("[GridSlotScanner] GridManager não atribuído!");
            return;
        }

        // ONDA 6.1: Remover FindFirstObjectByType - Apenas validar
        if (_uiManager == null)
        {
            Debug.LogError("[GridSlotScanner] PatternUIManager não atribuído no Inspector!");
            return;
        }

        if (PatternDetectionCache.Instance == null || !PatternDetectionCache.Instance.HasPatterns())
        {
            Debug.Log("[GridSlotScanner] Cache vazio ou nulo, scan cancelado");
            return;
        }

        _patternsAlreadyTriggered.Clear();
        var unlockedSlots = GetUnlockedSlots();

        if (unlockedSlots.Count == 0)
        {
            Debug.LogWarning("[GridSlotScanner] Nenhum slot desbloqueado!");
            return;
        }

        Debug.Log($"[GridSlotScanner] Verificando {unlockedSlots.Count} slots...");
        int patternsTriggered = 0;

        // --- LOOP PRINCIPAL ---
        foreach (var slotView in unlockedSlots)
        {
            // Verifica se pediram para parar o scan no meio do loop
            if (token.IsCancellationRequested) return;

            if (slotView == null) continue;
            int slotIndex = slotView.SlotIndex;

            var patternsForSlot = PatternDetectionCache.Instance.GetPatternsForSlot(slotIndex);

            foreach (var pattern in patternsForSlot)
            {
                if (!_patternsAlreadyTriggered.Contains(pattern))
                {

                    int accumulatedPoints = 0;
                    foreach (var p in _patternsAlreadyTriggered)
                    {
                        accumulatedPoints += p.BaseScore;
                    }
                    accumulatedPoints += pattern.BaseScore; 
                    
                    int newTotalScore = runData.CurrentWeeklyScore + accumulatedPoints;
                    events.Pattern.TriggerScoreIncremented(
                        pattern.BaseScore,
                        newTotalScore,
                        runData.WeeklyGoalTarget
                    );
                    
                    // Disparar evento de padrão completo (para highlights)
                    AppCore.Instance?.Events.Pattern.TriggerPatternSlotCompleted(pattern);

                    if (_uiManager != null)
                    {
                        await _uiManager.ShowPatternPopupRoutine(pattern);
                    }

                    _patternsAlreadyTriggered.Add(pattern);
                    patternsTriggered++;
                }
            }

            // Pausa entre slots
            if (_slotScanDelay > 0f)
            {
                // Substitui WaitForSeconds. TimeSpan.FromSeconds converte float para tempo real.
                await UniTask.Delay(TimeSpan.FromSeconds(_slotScanDelay), cancellationToken: token);
            }
        }

        Debug.Log($"[GridSlotScanner] Scan concluído: {patternsTriggered} padrões.");
        _patternsAlreadyTriggered.Clear();

        // Limpa o token ao final com sucesso
        _cts?.Dispose();
        _cts = null;
    }

    // ... GetUnlockedSlots (Mantém igual pois não tem corrotina) ...
    private List<GridSlotView> GetUnlockedSlots()
    {
        var unlockedSlots = new List<GridSlotView>();
        if (_gridManager == null) return unlockedSlots;

        var saveData = AppCore.Instance?.SaveManager?.Data;

        // Fallback simplificado
        if (saveData?.CurrentRun == null)
        {
            unlockedSlots.AddRange(_gridManager.GetComponentsInChildren<GridSlotView>());
            return unlockedSlots;
        }

        var slotStates = saveData.CurrentRun.SlotStates;
        var allSlotViews = _gridManager.GetComponentsInChildren<GridSlotView>();

        foreach (var slotView in allSlotViews)
        {
            if (slotView != null && slotView.SlotIndex < slotStates.Length)
            {
                if (slotStates[slotView.SlotIndex].IsUnlocked)
                    unlockedSlots.Add(slotView);
            }
        }
        return unlockedSlots;
    }

    /// <summary>
    /// Interrompe o scan imediatamente.
    /// </summary>
    public void StopScan()
    {
        // Em vez de StopAllCoroutines, cancelamos o Token
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        _patternsAlreadyTriggered.Clear();
        Debug.Log("[GridSlotScanner] Scan interrompido (UniTask Cancelled)");
    }

    private void OnDestroy()
    {
        // Segurança: Garante que se o objeto for destruído, a task para
        StopScan();
    }
}