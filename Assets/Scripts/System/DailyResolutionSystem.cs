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
        _events.Time.TriggerResolutionStarted();

        IGridService gridService = AppCore.Instance.GetGridLogic();
        var runData = _saveManager.Data.CurrentRun;


        Debug.Log("Iniciando Resolução Visual...");

        for (int i = 0; i < runData.GridSlots.Length; i++)
        {
            // 1. Visual: Câmera/UI foca no slot
            _events.Grid.TriggerAnalyzeSlot(i);

            // 2. Lógica: Processa maturação
            gridService.ProcessNightCycleForSlot(i);

            // 3. Ritmo: Delay para o jogador entender o que aconteceu
            // Se a planta morreu, o visual atualiza via evento do GridService, 
            // e este delay permite que o jogador veja o sprite mudando para "Withered".

            bool speedUp = _inputManager.IsPrimaryButtonHeld;
            float delay = speedUp ? _fastDelayPerSlot : _baseDelayPerSlot;
            yield return new WaitForSeconds(delay);
        }

        Debug.Log("Transição Visual Concluída.");

        // Pequena pausa final
        yield return new WaitForSeconds(_isInitialized && _inputManager.IsPrimaryButtonHeld ? _fastDelayPerSlot : _baseDelayPerSlot);

        _events.Time.TriggerResolutionEnded();
        _runManager.AdvanceDay();
        _isProcessing = false;
    }

    private void OnDisable()
    {
        _isProcessing = false;
    }
}