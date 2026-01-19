using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador do botão "Sleep" (avançar dia).
/// 
/// RESPONSABILIDADE:
/// - Validar se botão pode ser clicado (estado do jogo)
/// - Chamar DailyResolutionSystem ao clicar
/// - Feedback visual durante processamento
/// 
/// ARQUITETURA:
/// - Event-driven: Escuta TimeEvents para atualizar estado
/// - SOLID: Não acessa RunData diretamente
/// - Desacoplado: Não conhece implementação do DailyResolutionSystem
/// 
/// REGRAS:
/// - Desabilitado durante Weekend (Dia 6-7)
/// - Desabilitado durante menus (Shopping, Pause)
/// - Desabilitado durante animações (Resolution)
/// - Muda texto para "Sleeping..." durante processamento
/// </summary>
[RequireComponent(typeof(Button))]
public class SleepButtonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _buttonText;

    [Header("Text Settings")]
    [SerializeField] private string _normalText = "Sleep";
    [SerializeField] private string _processingText = "Sleeping...";

    [Header("State Rules")]
    [Tooltip("Desabilitar durante fim de semana (Dia 6-7)?")]
    [SerializeField] private bool _disableOnWeekend = true;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    private Button _button;
    private bool _isProcessing = false;
    private bool _isInitialized = false;

    private void Awake()
    {
        _button = GetComponent<Button>();
        
        if (_buttonText == null)
        {
            _buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeWhenReadyCoroutine());
    }

    private System.Collections.IEnumerator InitializeWhenReadyCoroutine()
    {
        // Espera AppCore
        while (AppCore.Instance == null)
        {
            yield return null;
        }

        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        // Conecta listener do botão
        _button.onClick.AddListener(OnSleepButtonClicked);

        // Escuta eventos de estado
        AppCore.Instance.Events.Time.OnResolutionStarted += HandleResolutionStarted;
        AppCore.Instance.Events.Time.OnResolutionEnded += HandleResolutionEnded;
        AppCore.Instance.Events.GameState.OnStateChanged += HandleGameStateChanged;
        AppCore.Instance.Events.Time.OnDayChanged += HandleDayChanged;

        // Atualiza estado inicial
        UpdateButtonState();

        _isInitialized = true;

        if (_showDebugLogs)
            Debug.Log("[SleepButtonController] ? Inicializado");
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnResolutionStarted -= HandleResolutionStarted;
            AppCore.Instance.Events.Time.OnResolutionEnded -= HandleResolutionEnded;
            AppCore.Instance.Events.GameState.OnStateChanged -= HandleGameStateChanged;
            AppCore.Instance.Events.Time.OnDayChanged -= HandleDayChanged;
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

        // Chama sistema de resolução
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

    private bool CanActivate()
    {
        if (AppCore.Instance == null) return false;
        if (AppCore.Instance.SaveManager?.Data?.CurrentRun == null) return false;

        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        var gameState = AppCore.Instance.GameStateManager.CurrentState;

        // 1. Verifica estado do jogo
        if (gameState != GameState.Playing)
        {
            return false; // Bloqueado durante Shopping, Pause, etc
        }

        // 2. Verifica se está no fim de semana (Dia 6-7)
        if (_disableOnWeekend)
        {
            if (runData.CurrentDay >= 6)
            {
                // Durante fim de semana, usa o botão "Trabalhar" do shop
                return false;
            }
        }

        return true;
    }
}
