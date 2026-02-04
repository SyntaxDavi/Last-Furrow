using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador do bot�o "Sleep" V2 - ROBUSTO E DEFENSIVO.
/// 
/// VERS�O 2: Null checks completos + valida��es + logs verbosos
/// 
/// INSTALA��O:
/// 1. Delete SleepButtonController antigo do GameObject
/// 2. Add Component: SleepButtonControllerV2
/// 3. Arraste TextMeshPro no campo Button Text
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
            Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: Componente Button n�o encontrado!");
        }

        if (_buttonText == null)
        {
            _buttonText = GetComponentInChildren<TextMeshProUGUI>();
            if (_buttonText == null)
            {
                Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: TextMeshProUGUI n�o encontrado! Arraste no Inspector!");
            }
        }
    }

    public void Initialize(UIContext context)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[SleepButtonControllerV2] ? J� foi inicializado!");
            return;
        }

        // === VALIDA��ES CR�TICAS ===
        if (context == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: UIContext � NULL!");
            return;
        }

        if (context.RunData == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: context.RunData � NULL!");
            return;
        }

        if (context.TimeEvents == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: context.TimeEvents � NULL!");
            return;
        }

        if (context.GameStateEvents == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: context.GameStateEvents � NULL!");
            return;
        }

        if (context.TimePolicy == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: context.TimePolicy � NULL!");
            return;
        }

        if (_button == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? CRITICAL: Button null! Componente n�o encontrado!");
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

        // IMPORTANTE: Marca como inicializado ANTES de UpdateButtonState
        _isInitialized = true;

        // Atualiza estado inicial (agora _isInitialized j� � true)
        UpdateButtonState();

        Debug.Log("[SleepButtonControllerV2] ?? INICIALIZADO COM SUCESSO!");
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

        if (_button != null)
        {
            _button.onClick.RemoveListener(OnSleepButtonClicked);
        }
    }

    private void OnSleepButtonClicked()
    {
        if (_isProcessing)
        {
            Debug.LogWarning("[SleepButtonControllerV2] ? J� est� processando!");
            return;
        }

        if (!CanActivate())
        {
            Debug.LogWarning("[SleepButtonControllerV2] ? Bot�o n�o pode ser ativado no estado atual.");
            return;
        }

        Debug.Log("[SleepButtonControllerV2] ?? INICIANDO SLEEP SEQUENCE...");

        if (AppCore.Instance == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? AppCore.Instance � NULL!");
            return;
        }

        if (AppCore.Instance.DailyResolutionSystem == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? DailyResolutionSystem � NULL!");
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
        Debug.Log("[SleepButtonControllerV2] ?? Resolution STARTED - Bloqueando bot�o");
        _isProcessing = true;
        UpdateButtonState();
    }

    private void HandleResolutionEnded()
    {
        Debug.Log("[SleepButtonControllerV2] ?? Resolution ENDED - Desbloqueando bot�o");
        _isProcessing = false;
        UpdateButtonState();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        Debug.Log($"[SleepButtonControllerV2] ?? GameState mudou: {newState}");
        UpdateButtonState();
    }

    private void HandleDayChanged(int newDay)
    {
        Debug.Log($"[SleepButtonControllerV2] ?? Dia mudou: {newDay}");
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[SleepButtonControllerV2] ? UpdateButtonState chamado mas n�o inicializado!");
            return;
        }

        if (_button == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? Button null em UpdateButtonState!");
            return;
        }

        Debug.Log("[SleepButtonControllerV2] ?? UpdateButtonState CHAMADO");

        bool canActivate = CanActivate();
        bool finalState = canActivate && !_isProcessing;

        Debug.Log($"[SleepButtonControllerV2] ?? canActivate={canActivate}, isProcessing={_isProcessing}, FINAL={finalState}");

        _button.interactable = finalState;

        if (_buttonText != null)
        {
            _buttonText.text = _isProcessing ? _processingText : _normalText;
        }

        Debug.Log($"[SleepButtonControllerV2] ? Bot�o agora: {(_button.interactable ? "ATIVO ?" : "DESABILITADO ?")}");
    }

    private bool CanActivate()
    {
        if (!_isInitialized)
        {
            Debug.Log("[SleepButtonControllerV2] ? N�o inicializado");
            return false;
        }

        if (_context == null || _context.RunData == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? Context ou RunData null!");
            return false;
        }

        int currentDay = _context.RunData.CurrentDay;
        Debug.Log($"[SleepButtonControllerV2] ?? CurrentDay: {currentDay}");

        // Valida��o AppCore
        if (AppCore.Instance == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? AppCore.Instance null!");
            return false;
        }

        if (AppCore.Instance.GameStateManager == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? GameStateManager null!");
            return false;
        }

        var gameState = AppCore.Instance.GameStateManager.CurrentState;
        Debug.Log($"[SleepButtonControllerV2] ?? GameState: {gameState}");

        if (gameState != GameState.Playing)
        {
            Debug.Log($"[SleepButtonControllerV2] ? Bloqueado: GameState = {gameState} (precisa Playing)");
            return false;
        }

        if (AppCore.Instance.RunManager == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? RunManager null!");
            return false;
        }

        var runPhase = AppCore.Instance.RunManager.CurrentPhase;
        Debug.Log($"[SleepButtonControllerV2] ?? RunPhase: {runPhase}");

        if (_context.TimePolicy == null)
        {
            Debug.LogError("[SleepButtonControllerV2] ? TimePolicy null!");
            return false;
        }

        bool canSleep = _context.TimePolicy.CanSleep(currentDay, runPhase);
        Debug.Log($"[SleepButtonControllerV2] ?? TimePolicy.CanSleep: {canSleep}");

        return canSleep;
    }
}
