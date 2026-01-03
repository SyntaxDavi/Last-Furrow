using UnityEngine;
using System.Collections;

public class DailyResolutionSystem : MonoBehaviour
{
    [Header("Configuração Visual")]
    [SerializeField] private float _baseDelayPerSlot = 0.3f;
    [SerializeField] private float _fastDelayPerSlot = 0.05f;

    // --- ESTADO INTERNO ---
    private bool _isProcessing = false;

    // --- DEPENDÊNCIAS CACHEADAS (Correção do Ponto 2) ---
    private RunManager _runManager;
    private SaveManager _saveManager;
    private InputManager _inputManager; // Correção do Ponto 3
    private GameEvents _events;         // Correção do Ponto 2

    private bool _isInitialized = false; // Flag de segurança

    // Chamado pelo AppCore no boot
    public void Initialize()
    {
        if (AppCore.Instance != null)
        {
            _runManager = AppCore.Instance.RunManager;
            _saveManager = AppCore.Instance.SaveManager;
            _inputManager = AppCore.Instance.InputManager;
            _events = AppCore.Instance.Events;

            _isInitialized = true;
        }
        else
        {
            Debug.LogError("[DailyResolution] AppCore não encontrado na inicialização!");
        }
    }

    public void StartEndDaySequence()
    {
        // 1. Segurança contra inicialização esquecida (Correção do Ponto 1)
        if (!_isInitialized)
        {
            Debug.LogWarning("[DailyResolution] Não inicializado! Tentando inicializar agora...");
            Initialize();

            if (!_isInitialized) // Se falhou mesmo assim
            {
                Debug.LogError("[DailyResolution] Falha fatal: Dependências não resolvidas.");
                return;
            }
        }

        // 2. Proteção contra execução dupla
        if (_isProcessing) return;

        // 3. Validação de dados da Run
        if (_runManager == null || _saveManager.Data.CurrentRun == null)
        {
            Debug.LogError("[DailyResolution] RunData inexistente.");
            return;
        }

        StartCoroutine(ResolveDayRoutine());
    }
    private IEnumerator ResolveDayRoutine()
    {
        _isProcessing = true;

        // Avisa a UI de tempo que a noite começou
        _events.Time.TriggerResolutionStarted();

        IGridService gridService = AppCore.Instance.GetGridLogic();
        var runData = _saveManager.Data.CurrentRun;

        // 1. Lógica Visual do Grid (Colheita/Morte)
        for (int i = 0; i < runData.GridSlots.Length; i++)
        {
            _events.Grid.TriggerAnalyzeSlot(i);
            gridService.ProcessNightCycleForSlot(i);

            // Pequeno delay visual
            float delay = _inputManager.IsPrimaryButtonHeld ? 0.05f : 0.3f;
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(0.5f);

        // 2. MUDANÇA DE DIA LÓGICA (Avança dia 1 -> 2)
        _runManager.AdvanceDay();

        // Atualiza a referência pois o runData pode ter mudado
        var currentRun = _saveManager.Data.CurrentRun;

        // 3. DRAW DIÁRIO + OVERFLOW
        // Aqui chamamos o novo sistema que criamos no AppCore
        AppCore.Instance.DailyHandSystem.ProcessDailyDraw(currentRun);

        // Pequeno delay para permitir que as animações de entrada de carta iniciem visualmente
        yield return new WaitForSeconds(0.8f);

        // 4. Salvar o estado final (Grid processado + Mão Nova + Dinheiro atualizado)
        _saveManager.SaveGame();

        _events.Time.TriggerResolutionEnded();
        _isProcessing = false;
    }

    private void OnDisable()
    {
        _isProcessing = false;
    }


}