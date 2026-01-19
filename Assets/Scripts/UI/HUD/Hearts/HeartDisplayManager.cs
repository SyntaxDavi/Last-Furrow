using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerenciador do sistema de vidas visual (Controller) - REFATORADO.
/// 
/// ?? RENOMEAR PARA HeartDisplayManager após deletar o antigo!
/// 
/// RESPONSABILIDADE:
/// - Spawnar/gerenciar pool de HeartViews
/// - Sincronizar com RunData.CurrentLives via eventos
/// - Orquestrar animações simultâneas com pop-up
/// 
/// ARQUITETURA:
/// - Event-driven: Escuta ProgressionEvents.OnLivesChanged via UIContext
/// - Dependency Injection: Recebe UIContext, não usa AppCore.Instance
/// - Pooling básico: Reutiliza GameObjects
/// 
/// REFATORAÇÕES:
/// - ? Injeção via UIContext (não mais AppCore.Instance)
/// - ? Animação simultânea ao perder múltiplas vidas
/// - ? Lógica de perda clarificada
/// - ? ExpandMaxLives preparado para futuro
/// </summary>
public class HeartDisplayManagerRefactored : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _heartPrefab;
    [SerializeField] private Transform _container;

    [Header("Layout Settings")]
    [Tooltip("Espaçamento horizontal entre corações")]
    [SerializeField] private float _spacing = 50f;

    [Tooltip("Delay entre spawn de cada coração no início")]
    [SerializeField] private float _spawnDelay = 0.5f;

    [Header("Multi-Loss Animation")]
    [Tooltip("Quando perde múltiplas vidas, animar simultaneamente?")]
    [SerializeField] private bool _simultaneousLoss = true;

    [Tooltip("Delay entre animações se não for simultâneo")]
    [SerializeField] private float _lossAnimationDelay = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    // Pool de corações
    private List<HeartView> _heartPool = new List<HeartView>();
    
    // Estado atual
    private int _currentLives = 0;
    private int _maxLives = 0;

    // Contexto injetado
    private UIContext _context;
    private bool _isInitialized = false;

    /// <summary>
    /// Inicialização via UIBootstrapper (injeção de dependências).
    /// </summary>
    public void Initialize(UIContext context)
    {
        if (_isInitialized)
        {
            if (_showDebugLogs)
                Debug.LogWarning("[HeartDisplayManager] Já foi inicializado!");
            return;
        }

        _context = context ?? throw new System.ArgumentNullException(nameof(context));

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

        // Lê estado inicial via interface
        _maxLives = _context.RunData.MaxLives;
        _currentLives = _context.RunData.CurrentLives;

        // Cria pool inicial
        CreateHeartPool(_maxLives);

        // Spawna com animação inicial
        StartCoroutine(SpawnInitialHearts());

        // Escuta eventos via contexto
        _context.ProgressionEvents.OnLivesChanged += HandleLivesChanged;

        _isInitialized = true;

        if (_showDebugLogs)
            Debug.Log($"[HeartDisplayManager] ? Inicializado. Lives: {_currentLives}/{_maxLives}");
    }

    private void OnDestroy()
    {
        if (_context != null)
        {
            _context.ProgressionEvents.OnLivesChanged -= HandleLivesChanged;
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
    /// CLARIFICADO: Lógica simplificada e documentada.
    /// </summary>
    private IEnumerator AnimateLoseHearts(int count)
    {
        // Encontra índices dos corações a serem perdidos
        // Ex: Se tinha 3 vidas e perdeu 2, anima índices 2 e 1 (direita ? esquerda)
        List<int> heartsToLose = new List<int>();
        
        for (int i = 0; i < count; i++)
        {
            int heartIndex = (_currentLives + count - 1) - i; // Começa do mais à direita
            if (heartIndex >= 0 && heartIndex < _heartPool.Count)
            {
                heartsToLose.Add(heartIndex);
            }
        }

        if (_simultaneousLoss)
        {
            // Animação simultânea (todos de uma vez)
            foreach (int index in heartsToLose)
            {
                _heartPool[index].AnimateLose();
            }
        }
        else
        {
            // Animação sequencial (um por vez)
            foreach (int index in heartsToLose)
            {
                _heartPool[index].AnimateLose();
                yield return new WaitForSeconds(_lossAnimationDelay);
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
    /// Por enquanto adiciona silenciosamente sem animação.
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
