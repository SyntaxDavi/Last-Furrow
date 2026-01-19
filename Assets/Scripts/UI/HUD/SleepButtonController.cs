using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador do botão "Sleep" (avançar dia) - REFATORADO.
/// 
/// ?? RENOMEAR PARA SleepButtonController após deletar o antigo!
/// 
/// RESPONSABILIDADE:
/// - Validar se botão pode ser clicado (via ITimePolicy)
/// - Chamar DailyResolutionSystem ao clicar
/// - Feedback visual durante processamento
/// 
/// ARQUITETURA:
/// - Event-driven: Escuta TimeEvents e GameStateEvents via UIContext
/// - Dependency Injection: Recebe UIContext, não usa AppCore.Instance
/// - SOLID: UI pergunta para ITimePolicy, não decide
/// 
/// REFATORAÇÕES:
/// - ? Injeção via UIContext
/// - ? Validação delegada para ITimePolicy
/// - ? Removido AppCore.Instance (exceto DailyResolutionSystem - TODO futuro)
/// </summary>
[RequireComponent(typeof(Button))]
public class SleepButtonControllerRefactored : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _buttonText;

    [Header("Text Settings")]
    [SerializeField] private string _normalText = "Sleep";
    [SerializeField] private string _processingText = "Sleeping...";

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    private Button _button;
    private bool _isProcessing = false;

    // Contexto injetado
    private UIContext _context;
    private bool _isInitialized = false;

    private void Awake()
    {
        _button = GetComponent<Button>();
        
        if (_buttonText == null)
        {
            _buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Inicialização via UIBootstrapper (injeção de dependências).
    /// </summary>
    public void Initialize(UIContext context)
    {
        if (_isInitialized)
        {
            if (_showDebugLogs)
                Debug.LogWarning("[SleepButtonController] Já foi inicializado!");
            return;
        }

        _context = context ?? throw new System.ArgumentNullException(nameof(context));

        // Conecta listener do botão
        _button.onClick.AddListener(OnSleepButtonClicked);

        // Escuta eventos de estado via contexto
        _context.TimeEvents.OnResolutionStarted += HandleResolutionStarted;
        _context.TimeEvents.OnResolutionEnded += HandleResolutionEnded;
        _context.GameStateEvents.OnStateChanged += HandleGameStateChanged;
        _context.TimeEvents.OnDayChanged += HandleDayChanged;

        // Atualiza estado inicial
        UpdateButtonState();

        _isInitialized = true;

        if (_showDebugLogs)
            Debug.Log("[SleepButtonController] ? Inicializado");
    }

    private void OnDestroy()
    {
        if (_context != null)
        {
            _context.TimeEvents.OnResolutionStarted -= HandleResolutionStarted;
            _context.TimeEvents.OnResolutionEnded -= HandleResolutionEnded;
            _context.GameStateEvents.OnStateChanged -= HandleGameStateChanged;
            _context.TimeEvents.OnDayChanged -= HandleDayChanged;
        }

        _button.onClick.RemoveListener(OnSleepButtonClicked);
    }

    private void OnSleepButtonClicked()
    {
        if (_isProcessing)
        {
            if (_showDebugLogs)
                Debug.LogWarning("[SleepButtonController] Já está processando!");
            return;
        }

        if (!CanActivate())
        {
            if (_showDebugLogs)
                Debug.LogWarning("[SleepButtonController] Botão não pode ser ativado no estado atual.");
            return;
        }

        if (_showDebugLogs)
            Debug.Log("[SleepButtonController] Iniciando ciclo de fim de dia...");

        // Chama sistema de resolução (ainda precisa de AppCore para isso)
        // TODO FUTURO: Injetar DailyResolutionSystem via UIContext
        AppCore.Instance.DailyResolutionSystem.StartEndDaySequence();
    }

    private void HandleResolutionStarted()
    {
        _isProcessing = true;
        UpdateButtonState();
    }

    private void HandleResolutionEnded()
    {
        _isProcessing = false;
        UpdateButtonState();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        UpdateButtonState();
    }

    private void HandleDayChanged(int newDay)
    {
        UpdateButtonState();
    }

    /// <summary>
    /// Atualiza estado visual do botão.
    /// </summary>
    private void UpdateButtonState()
    {
        bool canActivate = CanActivate();
        _button.interactable = canActivate && !_isProcessing;

        // Atualiza texto
        if (_buttonText != null)
        {
            _buttonText.text = _isProcessing ? _processingText : _normalText;
        }
    }

    /// <summary>
    /// Valida se botão pode ser ativado.
    /// DELEGADO: Usa ITimePolicy para regras de tempo.
    /// </summary>
    private bool CanActivate()
    {
        if (!_isInitialized) return false;

        // 1. Lê dados via interface
        int currentDay = _context.RunData.CurrentDay;

        // 2. Pergunta para GameStateManager (ainda via AppCore, TODO: injetar)
        var gameState = AppCore.Instance.GameStateManager.CurrentState;
        if (gameState != GameState.Playing)
        {
            return false; // Bloqueado durante Shopping, Pause, etc
        }

        // 3. ? DELEGADO: Pergunta para ITimePolicy
        var runPhase = AppCore.Instance.RunManager.CurrentPhase;
        bool canSleep = _context.TimePolicy.CanSleep(currentDay, runPhase);

        return canSleep;
    }
}
