using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerenciador do sistema de vidas visual V2 - ROBUSTO E DEFENSIVO.
/// 
/// VERSÃO 2: Null checks completos + validações + logs verbosos
/// 
/// INSTALAÇÃO:
/// 1. Delete HeartDisplayManager antigo do GameObject
/// 2. Add Component: HeartDisplayManagerV2
/// 3. Arraste HeartPrefab no Inspector
/// 4. Play e veja logs detalhados
/// </summary>
public class HeartDisplayManagerV2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _heartPrefab;
    [SerializeField] private Transform _container;

    [Header("Layout Settings")]
    [SerializeField] private float _spacing = 50f;
    [SerializeField] private float _spawnDelay = 0.5f;

    [Header("Multi-Loss Animation")]
    [SerializeField] private bool _simultaneousLoss = true;
    [SerializeField] private float _lossAnimationDelay = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    private List<HeartView> _heartPool = new List<HeartView>();
    private int _currentLives = 0;
    private int _maxLives = 0;
    private UIContext _context;
    private bool _isInitialized = false;

    public void Initialize(UIContext context)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[HeartDisplayManagerV2] Já foi inicializado!");
            return;
        }

        // === VALIDAÇÕES CRÍTICAS ===
        if (context == null)
        {
            Debug.LogError("[HeartDisplayManagerV2] CRITICAL: UIContext é NULL!");
            return;
        }

        if (_heartPrefab == null)
        {
            Debug.LogError("[HeartDisplayManagerV2] CRITICAL: HeartPrefab não atribuído no Inspector!");
            return;
        }

        if (_container == null)
        {
            _container = this.transform;
        }

        _context = context;

        // === VALIDAÇÕES DE DADOS ===
        if (_context.RunData == null)
        {
            Debug.LogError("[HeartDisplayManagerV2] CRITICAL: context.RunData é NULL!");
            return;
        }

        if (_context.ProgressionEvents == null)
        {
            Debug.LogError("[HeartDisplayManagerV2] CRITICAL: context.ProgressionEvents é NULL!");
            return;
        }

        _maxLives = _context.RunData.MaxLives;
        _currentLives = _context.RunData.CurrentLives;

        // Cria pool
        CreateHeartPool(_maxLives);

        // Spawna com animação
        StartCoroutine(SpawnInitialHearts());

        // Escuta eventos
        _context.ProgressionEvents.OnLivesChanged += HandleLivesChanged;

        _isInitialized = true;
        Debug.Log($"[HeartDisplayManagerV2] ?? INICIALIZADO COM SUCESSO!");
    }

    private void OnDestroy()
    {
        if (_context != null && _context.ProgressionEvents != null)
        {
            _context.ProgressionEvents.OnLivesChanged -= HandleLivesChanged;
        }
    }

    private void CreateHeartPool(int count)
    {
        Debug.Log($"[HeartDisplayManagerV2] ?? Criando pool de {count} corações...");

        if (_heartPrefab == null)
        {
            Debug.LogError("[HeartDisplayManagerV2] ? HeartPrefab null durante CreatePool!");
            return;
        }

        if (_container == null)
        {
            Debug.LogError("[HeartDisplayManagerV2] ? Container null durante CreatePool!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject heartObj = Instantiate(_heartPrefab, _container);

            if (heartObj == null)
            {
                Debug.LogError($"[HeartDisplayManagerV2] ? Instantiate retornou NULL no índice {i}!");
                continue;
            }

            HeartView heartView = heartObj.GetComponent<HeartView>();

            if (heartView == null)
            {
                Debug.LogError($"[HeartDisplayManagerV2] ? HeartPrefab não tem componente HeartView!");
                Destroy(heartObj);
                continue;
            }

            RectTransform rt = heartObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(i * _spacing, 0);
            }

            heartView.Hide();
            _heartPool.Add(heartView);
        }
    }

    private IEnumerator SpawnInitialHearts()
    {
        for (int i = 0; i < _currentLives && i < _heartPool.Count; i++)
        {
            if (_heartPool[i] == null)
            {
                Debug.LogError($"[HeartDisplayManagerV2] ? HeartPool[{i}] é NULL!");
                continue;
            }

            _heartPool[i].SetState(true, immediate: false);
            _heartPool[i].AnimateSpawn();

            yield return new WaitForSeconds(_spawnDelay);
        }

        // Corações vazios
        for (int i = _currentLives; i < _maxLives && i < _heartPool.Count; i++)
        {
            if (_heartPool[i] == null)
            {
                Debug.LogError($"[HeartDisplayManagerV2] ? HeartPool[{i}] é NULL!");
                continue;
            }
            _heartPool[i].SetState(false, immediate: true);
        }
    }

    private void HandleLivesChanged(int newLives)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[HeartDisplayManagerV2] ? HandleLivesChanged chamado mas não inicializado!");
            return;
        }

        int oldLives = _currentLives;
        _currentLives = Mathf.Clamp(newLives, 0, _maxLives);

        if (_currentLives < oldLives)
        {
            int livesLost = oldLives - _currentLives;
            StartCoroutine(AnimateLoseHearts(livesLost));
        }
        else if (_currentLives > oldLives)
        {
            int livesGained = _currentLives - oldLives;
            StartCoroutine(AnimateHealHearts(livesGained));
        }
    }

    private IEnumerator AnimateLoseHearts(int count)
    {
        List<int> heartsToLose = new List<int>();

        for (int i = 0; i < count; i++)
        {
            int heartIndex = (_currentLives + count - 1) - i;
            if (heartIndex >= 0 && heartIndex < _heartPool.Count)
            {
                heartsToLose.Add(heartIndex);
            }
        }

        if (_simultaneousLoss)
        {
            foreach (int index in heartsToLose)
            {
                if (_heartPool[index] != null)
                {
                    _heartPool[index].AnimateLose();
                }
            }
        }
        else
        {
            foreach (int index in heartsToLose)
            {
                if (_heartPool[index] != null)
                {
                    _heartPool[index].AnimateLose();
                    yield return new WaitForSeconds(_lossAnimationDelay);
                }
            }
        }
    }

    private IEnumerator AnimateHealHearts(int count)
    {
        int startIndex = _currentLives - count;

        for (int i = 0; i < count && startIndex + i < _heartPool.Count; i++)
        {
            int index = startIndex + i;
            if (index >= 0 && _heartPool[index] != null)
            {
                _heartPool[index].AnimateHeal();
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    public void ExpandMaxLives(int newMaxLives)
    {
        if (newMaxLives <= _maxLives) return;

        int difference = newMaxLives - _maxLives;
        _maxLives = newMaxLives;

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
    }
}
