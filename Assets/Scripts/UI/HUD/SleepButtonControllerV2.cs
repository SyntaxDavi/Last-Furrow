using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador do botão "Sleep" V2 - ROBUSTO E DEFENSIVO.
/// 
/// VERSÃO 2: Null checks completos + validações + logs verbosos
/// 
/// INSTALAÇÃO:
/// 1. Delete SleepButtonController antigo do GameObject
/// 2. Add Component: SleepButtonControllerV2
/// 3. Arraste TextMeshProUGUI no campo Button Text
/// 4. Play e veja logs detalhados
/// </summary>
[RequireComponent(typeof(Button))]
public class SleepButtonControllerV2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _buttonText;

    [Header("Text Settings")]
    [SerializeField] private string _normalText = "Sleep";
    [SerializeField] private string _processingText = "Sleeping...";


    private Button _button;
    private bool _isProcessing = false;

    private UIContext _context;
    private bool _isInitialized = false;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (_button == null)
        {
            Debug.LogError("[SleepButtonControllerV2] CRITICAL: Componente Button não encontrado!");
        }

        if (_buttonText == null)
        {
            _buttonText = GetComponentInChildren<TextMeshProUGUI>();
            if (_buttonText == null)
            {
                Debug.LogError("[SleepButtonControllerV2] CRITICAL: TextMeshProUGUI não encontrado! Arraste no Inspector!");
            }
        }
    }

    public void Initialize(UIContext context)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[SleepButtonControllerV2] Já foi inicializado!");
            return;
        }

        // === VALIDAÇÕES CRÍTICAS ===
        if (context == null)
        {
            Debug.LogError("[SleepButtonControllerV2] CRITICAL: UIContext é NULL!");
            return;
        }

        if (context.RunData == null)
        {
            Debug.LogError("[SleepButtonControllerV2] CRITICAL: context.RunData é NULL!");
            return;
        }

        if (context.TimeEvents == null)
        {
            Debug.LogError("[SleepButtonControllerV2] CRITICAL: context.TimeEvents é NULL!");
            return;
        }

        if (context.GameStateEvents == null)
        {
            Debug.LogError("[SleepButtonControllerV2] CRITICAL: context.GameStateEvents é NULL!");
            return;
        }

        if (context.TimePolicy == null)
        {
            Debug.LogError("[SleepButtonControllerV2] CRITICAL: context.TimePolicy é NULL!");
            return;
        }

        if (_button == null)
        {
            Debug.LogError("[SleepButtonControllerV2] CRITICAL: Button null! Componente não encontrado!");
            return;
        }

        _context = context;

        // Conecta listener
        _button.onClick.AddListener(OnSleepButtonClicked);

        // Escuta eventos
        _context.TimeEvents.OnResolutionStarted += HandleResolutionStarted;
        _context.TimeEvents.OnResolutionSequenceComplete += HandleResolutionEnded;
        _context.GameStateEvents.OnStateChanged += HandleGameStateChanged;
        _context.TimeEvents.OnDayChanged += HandleDayChanged;

        // FIX: Escuta quando a produção começa (após sair do shop)
        if (AppCore.Instance?.RunManager != null)
        {
            AppCore.Instance.RunManager.OnProductionStarted += HandleProductionStarted;
            Debug.Log("[SleepButtonControllerV2] Listener OnProductionStarted REGISTRADO");
        }
        else
        {
            Debug.LogError("[SleepButtonControllerV2] FALHA ao registrar OnProductionStarted - RunManager null!");
        }

        // IMPORTANTE: Marca como inicializado ANTES de UpdateButtonState
        _isInitialized = true;

        // Atualiza estado inicial (agora _isInitialized já é true)
        UpdateButtonState();

        Debug.Log("[SleepButtonControllerV2] INICIALIZADO COM SUCESSO!");
    }

    private void OnEnable()
    {
        if (_isInitialized)
        {
            UpdateButtonState();
        }
    }

    private void OnDestroy()
    {
        if (_context != null)
        {
            if (_context.TimeEvents != null)
            {
                _context.TimeEvents.OnResolutionStarted -= HandleResolutionStarted;
                _context.TimeEvents.OnResolutionSequenceComplete -= HandleResolutionEnded;
                _context.TimeEvents.OnDayChanged -= HandleDayChanged;
            }

            if (_context.GameStateEvents != null)
            {
                _context.GameStateEvents.OnStateChanged -= HandleGameStateChanged;
            }
        }

        // FIX: Remove listener do RunManager
        if (AppCore.Instance?.RunManager != null)
        {
            AppCore.Instance.RunManager.OnProductionStarted -= HandleProductionStarted;
            Debug.Log("[SleepButtonControllerV2] Listener OnProductionStarted REMOVIDO");
        }

        if (_button != null)
        {
            _button.onClick.RemoveListener(OnSleepButtonClicked);
        }
    }

    private void OnSleepButtonClicked()
    {
        if (_isProcessing)
        {
            Debug.LogWarning("[SleepButtonControllerV2] Já está processando!");
            return;
        }

        if (!CanActivate())
        {
            Debug.LogWarning("[SleepButtonControllerV2] Botão não pode ser ativado no estado atual.");
            return;
        }

        Debug.Log("[SleepButtonControllerV2] INICIANDO SLEEP SEQUENCE...");

        if (AppCore.Instance == null)
        {
            Debug.LogError("[SleepButtonControllerV2] AppCore.Instance é NULL!");
            return;
        }

        if (AppCore.Instance.DailyResolutionSystem == null)
        {
            Debug.LogError("[SleepButtonControllerV2] DailyResolutionSystem é NULL!");
            return;
        }

        // IMEDIATO: Desabilita botão para evitar spam de cliques
        _isProcessing = true;
        UpdateButtonState();
        
        bool started = AppCore.Instance.DailyResolutionSystem.StartEndDaySequence();
        
        // Se por algum motivo o sistema recusar (ex: já estava processando em outro lugar),
        // limpamos o estado do botão para não ficar travado.
        if (!started)
        {
            _isProcessing = false;
            UpdateButtonState();
        }
    }

    private void HandleResolutionStarted()
    {
        Debug.Log("[SleepButtonControllerV2] Resolution STARTED - Bloqueando botão");
        _isProcessing = true;
        UpdateButtonState();
    }

    private void HandleResolutionEnded()
    {
        Debug.Log("[SleepButtonControllerV2] Resolution ENDED - Desbloqueando botão");
        _isProcessing = false;
        UpdateButtonState();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        Debug.Log($"[SleepButtonControllerV2] GameState mudou: {newState}");
        UpdateButtonState();
    }

    private void HandleDayChanged(int newDay)
    {
        Debug.Log($"[SleepButtonControllerV2] Dia mudou: {newDay}");
        UpdateButtonState();
    }

    // FIX: Novo handler para quando a produção começa
    private void HandleProductionStarted(RunData runData)
    {
        Debug.Log($"[SleepButtonControllerV2] ===== PRODUCAO INICIADA =====");
        Debug.Log($"[SleepButtonControllerV2] Semana: {runData.CurrentWeek}, Dia: {runData.CurrentDay}");
        Debug.Log($"[SleepButtonControllerV2] RunPhase: {AppCore.Instance.RunManager.CurrentPhase}");
        Debug.Log($"[SleepButtonControllerV2] GameState: {AppCore.Instance.GameStateManager.CurrentState}");
        Debug.Log($"[SleepButtonControllerV2] Aguardando 0.5s para sincronização...");
        
        // IMPORTANTE: Aumentei o delay para garantir sincronização e evitar race condition de 0.5s do Fade
        Invoke(nameof(UpdateButtonStateDelayed), 2.0f);
    }

    // Helper para atualização com delay
    private void UpdateButtonStateDelayed()
    {
        Debug.Log($"[SleepButtonControllerV2] ===== DELAY COMPLETO - ATUALIZANDO BOTÃO =====");
        Debug.Log($"[SleepButtonControllerV2] RunPhase AGORA: {AppCore.Instance.RunManager.CurrentPhase}");
        Debug.Log($"[SleepButtonControllerV2] GameState AGORA: {AppCore.Instance.GameStateManager.CurrentState}");
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[SleepButtonControllerV2] UpdateButtonState chamado mas não inicializado!");
            return;
        }

        if (_button == null)
        {
            Debug.LogError("[SleepButtonControllerV2] Button null em UpdateButtonState!");
            return;
        }

        Debug.Log("[SleepButtonControllerV2] UpdateButtonState CHAMADO");

        bool canActivate = CanActivate();
        bool finalState = canActivate && !_isProcessing;

        Debug.Log($"[SleepButtonControllerV2] canActivate={canActivate}, isProcessing={_isProcessing}, FINAL={finalState}");

        _button.interactable = finalState;

        if (_buttonText != null)
        {
            _buttonText.text = _isProcessing ? _processingText : _normalText;
        }

        Debug.Log($"[SleepButtonControllerV2] Botão agora: {(_button.interactable ? "ATIVO" : "DESABILITADO")}");
    }

    private bool CanActivate()
    {
        if (!_isInitialized)
        {
            Debug.Log("[SleepButtonControllerV2] Não inicializado");
            return false;
        }

        if (_context == null || _context.RunData == null)
        {
            Debug.LogError("[SleepButtonControllerV2] Context ou RunData null!");
            return false;
        }

        int currentDay = _context.RunData.CurrentDay;
        Debug.Log($"[SleepButtonControllerV2] CurrentDay: {currentDay}");

        // Validação AppCore
        if (AppCore.Instance == null)
        {
            Debug.LogError("[SleepButtonControllerV2] AppCore.Instance null!");
            return false;
        }

        if (AppCore.Instance.GameStateManager == null)
        {
            Debug.LogError("[SleepButtonControllerV2] GameStateManager null!");
            return false;
        }

        var gameState = AppCore.Instance.GameStateManager.CurrentState;
        Debug.Log($"[SleepButtonControllerV2] GameState: {gameState}");

        if (gameState != GameState.Playing)
        {
            Debug.Log($"[SleepButtonControllerV2] Bloqueado: GameState = {gameState} (precisa Playing)");
            return false;
        }

        if (AppCore.Instance.RunManager == null)
        {
            Debug.LogError("[SleepButtonControllerV2] RunManager null!");
            return false;
        }

        var runPhase = AppCore.Instance.RunManager.CurrentPhase;
        Debug.Log($"[SleepButtonControllerV2] RunPhase: {runPhase}");

        if (_context.TimePolicy == null)
        {
            Debug.LogError("[SleepButtonControllerV2] TimePolicy null!");
            return false;
        }

        bool canSleep = _context.TimePolicy.CanSleep(currentDay, runPhase);
        Debug.Log($"[SleepButtonControllerV2] TimePolicy.CanSleep: {canSleep}");

        return canSleep;
    }
}
