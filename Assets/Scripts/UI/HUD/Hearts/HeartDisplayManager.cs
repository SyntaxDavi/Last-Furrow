using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerenciador do sistema de vidas visual (Controller).
/// 
/// RESPONSABILIDADE:
/// - Spawnar/gerenciar pool de HeartViews
/// - Sincronizar com RunData.CurrentLives via eventos
/// - Orquestrar animações em sequência
/// 
/// ARQUITETURA:
/// - Event-driven: Escuta ProgressionEvents.OnLivesChanged
/// - Pooling básico: Reutiliza GameObjects
/// - SOLID: Não acessa RunData diretamente, recebe via eventos
/// 
/// EXTENSIBILIDADE FUTURA:
/// - MaxLives dinâmico (cartas que aumentam vida máxima)
/// - Tipos diferentes de corações (shield, golden heart)
/// - Animações customizadas por tipo de dano
/// </summary>
public class HeartDisplayManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _heartPrefab;
    [SerializeField] private Transform _container;

    [Header("Layout Settings")]
    [Tooltip("Espaçamento horizontal entre corações")]
    [SerializeField] private float _spacing = 50f;

    [Tooltip("Delay entre spawn de cada coração no início")]
    [SerializeField] private float _spawnDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    // Pool de corações
    private List<HeartView> _heartPool = new List<HeartView>();
    
    // Estado atual
    private int _currentLives = 0;
    private int _maxLives = 0;

    private bool _isInitialized = false;

    private void Start()
    {
        // Aguarda AppCore estar pronto
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        // Espera AppCore estar disponível
        while (AppCore.Instance == null)
        {
            yield return null;
        }

        // Espera RunData estar disponível
        while (AppCore.Instance.SaveManager?.Data?.CurrentRun == null)
        {
            yield return null;
        }

        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        // Validações
        if (_heartPrefab == null)
        {
            Debug.LogError("[HeartDisplayManager] HeartPrefab não atribuído!");
            return;
        }

        if (_container == null)
        {
            Debug.LogWarning("[HeartDisplayManager] Container não atribuído. Usando this.transform.");
            _container = this.transform;
        }

        // Lê estado inicial
        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        _maxLives = runData.MaxLives;
        _currentLives = runData.CurrentLives;

        // Cria pool inicial
        CreateHeartPool(_maxLives);

        // Spawna com animação inicial
        StartCoroutine(SpawnInitialHearts());

        // Escuta eventos
        AppCore.Instance.Events.Progression.OnLivesChanged += HandleLivesChanged;

        _isInitialized = true;

        if (_showDebugLogs)
            Debug.Log($"[HeartDisplayManager] ? Inicializado. Lives: {_currentLives}/{_maxLives}");
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Progression.OnLivesChanged -= HandleLivesChanged;
        }
    }

    /// <summary>
    /// Cria pool de HeartViews baseado em MaxLives.
    /// </summary>
    private void CreateHeartPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject heartObj = Instantiate(_heartPrefab, _container);
            HeartView heartView = heartObj.GetComponent<HeartView>();

            if (heartView == null)
            {
                Debug.LogError("[HeartDisplayManager] HeartPrefab não tem componente HeartView!");
                Destroy(heartObj);
                continue;
            }

            // Posiciona horizontalmente
            RectTransform rt = heartObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(i * _spacing, 0);

            // Estado inicial: escondido
            heartView.Hide();

            _heartPool.Add(heartView);
        }
    }

    /// <summary>
    /// Spawn inicial com sequência animada.
    /// </summary>
    private IEnumerator SpawnInitialHearts()
    {
        for (int i = 0; i < _currentLives && i < _heartPool.Count; i++)
        {
            _heartPool[i].SetState(true, immediate: false);
            _heartPool[i].AnimateSpawn();

            yield return new WaitForSeconds(_spawnDelay);
        }

        // Corações vazios (se CurrentLives < MaxLives)
        for (int i = _currentLives; i < _maxLives && i < _heartPool.Count; i++)
        {
            _heartPool[i].SetState(false, immediate: true);
        }
    }

    /// <summary>
    /// Listener: Atualiza visual quando vidas mudam.
    /// </summary>
    private void HandleLivesChanged(int newLives)
    {
        if (!_isInitialized) return;

        int oldLives = _currentLives;
        _currentLives = Mathf.Clamp(newLives, 0, _maxLives);

        if (_showDebugLogs)
            Debug.Log($"[HeartDisplayManager] Lives: {oldLives} ? {_currentLives}");

        // Perdeu vida
        if (_currentLives < oldLives)
        {
            int livesLost = oldLives - _currentLives;
            StartCoroutine(AnimateLoseHearts(livesLost));
        }
        // Ganhou vida (heal)
        else if (_currentLives > oldLives)
        {
            int livesGained = _currentLives - oldLives;
            StartCoroutine(AnimateHealHearts(livesGained));
        }
    }

    /// <summary>
    /// Animação de perda: Da DIREITA para ESQUERDA.
    /// </summary>
    private IEnumerator AnimateLoseHearts(int count)
    {
        // Encontra corações cheios da direita para esquerda
        for (int i = _heartPool.Count - 1; i >= 0 && count > 0; i--)
        {
            // Verifica se é um coração cheio (índice dentro de currentLives antigo)
            // Como acabamos de atualizar _currentLives, precisamos calcular
            int indexToAnimate = _currentLives + (count - 1);
            
            if (indexToAnimate >= 0 && indexToAnimate < _heartPool.Count)
            {
                _heartPool[indexToAnimate].AnimateLose();
                count--;
                yield return new WaitForSeconds(0.1f); // Pequeno delay entre perdas
            }
        }
    }

    /// <summary>
    /// Animação de cura: Da ESQUERDA para DIREITA.
    /// </summary>
    private IEnumerator AnimateHealHearts(int count)
    {
        int startIndex = _currentLives - count;

        for (int i = 0; i < count && startIndex + i < _heartPool.Count; i++)
        {
            int index = startIndex + i;
            if (index >= 0)
            {
                _heartPool[index].AnimateHeal();
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    /// <summary>
    /// FUTURO: Expansão de MaxLives (cartas que aumentam vida máxima).
    /// </summary>
    public void ExpandMaxLives(int newMaxLives)
    {
        if (newMaxLives <= _maxLives) return;

        int difference = newMaxLives - _maxLives;
        _maxLives = newMaxLives;

        // Cria novos corações no pool
        for (int i = 0; i < difference; i++)
        {
            GameObject heartObj = Instantiate(_heartPrefab, _container);
            HeartView heartView = heartObj.GetComponent<HeartView>();

            if (heartView != null)
            {
                int index = _heartPool.Count;
                RectTransform rt = heartObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(index * _spacing, 0);

                heartView.SetState(false, immediate: true);
                _heartPool.Add(heartView);
            }
        }

        if (_showDebugLogs)
            Debug.Log($"[HeartDisplayManager] MaxLives expandido: {_maxLives}");
    }
}
