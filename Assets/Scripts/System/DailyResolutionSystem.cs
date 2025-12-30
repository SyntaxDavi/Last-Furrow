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

        // Uso do cache (sem AppCore.Instance...)
        _events.Time.TriggerResolutionStarted();

        var runData = _saveManager.Data.CurrentRun;
        int slotCount = runData.GridSlots.Length;

        Debug.Log("Iniciando Resolução Visual...");

        for (int i = 0; i < slotCount; i++)
        {
            if (i >= runData.GridSlots.Length) break;

            // Dispara evento visual usando cache
            _events.Grid.TriggerAnalyzeSlot(i);

            // Correção do Ponto 3: Lógica não lê Input direto
            // Pergunta ao InputManager se deve acelerar
            bool speedUp = _inputManager != null && _inputManager.IsPrimaryButtonHeld;

            float delay = speedUp ? _fastDelayPerSlot : _baseDelayPerSlot;
            yield return new WaitForSeconds(delay);
        }

        Debug.Log("Transição Visual Concluída.");

        // Pequena pausa final
        yield return new WaitForSeconds(_isInitialized && _inputManager.IsPrimaryButtonHeld ? _fastDelayPerSlot : _baseDelayPerSlot);

        _events.Time.TriggerResolutionEnded();

        // Avança o dia
        _runManager.AdvanceDay();

        _isProcessing = false;
    }

    private void OnDisable()
    {
        _isProcessing = false;
    }
}