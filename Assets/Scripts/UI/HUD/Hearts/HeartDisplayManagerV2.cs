using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerenciador do sistema de vidas visual V2 - ROBUSTO E DEFENSIVO.
/// 
/// VERS�O 2: Null checks completos + valida��es + logs verbosos
/// 
/// INSTALA��O:
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
    private const int LIVES_PER_HEART = 3;
    private const int MAX_HEART_COUNT = 2; // 1 Coração Base (3 vidas) + 1 Coração Extra (1 vida) = 4 total

    private List<HeartView> _heartPool = new List<HeartView>();
    private int _currentLives = 0;
    private int _maxLives = 0;
    private UIContext _context;
    private bool _isInitialized = false;

    public void Initialize(UIContext context)
    {
        if (_isInitialized) return;

        if (context == null || _heartPrefab == null)
        {
            Debug.LogError("[HeartDisplayManagerV2] Erro na inicialização!");
            return;
        }

        _context = context;
        _maxLives = _context.RunData.MaxLives;
        _currentLives = _context.RunData.CurrentLives;
        
        Debug.Log($"[HeartDisplay] Vidas: {_currentLives}/{_maxLives}");

        // Calcula quantos corações precisamos (Ex: 4 vidas = 2 corações)
        int extraLives = Mathf.Max(0, _maxLives - 3);
        int neededHearts = 1 + extraLives;
        neededHearts = Mathf.Min(neededHearts, MAX_HEART_COUNT);
        
        Debug.Log($"[HeartDisplay] Spawnando {neededHearts} corações.");

        CreateHeartPool(neededHearts);
        RefreshAllHearts(true);

        _context.ProgressionEvents.OnLivesChanged += HandleLivesChanged;
        _isInitialized = true;
    }

    private void OnDestroy()
    {
        if (_context?.ProgressionEvents != null)
            _context.ProgressionEvents.OnLivesChanged -= HandleLivesChanged;
    }

    private void CreateHeartPool(int count)
    {
        foreach (var heart in _heartPool) if(heart != null) Destroy(heart.gameObject);
        _heartPool.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject heartObj = Instantiate(_heartPrefab, _container);
            HeartView heartView = heartObj.GetComponent<HeartView>();
            
            if (heartView != null)
            {
                RectTransform rt = heartObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i * _spacing, 0);
                heartView.Hide();
                _heartPool.Add(heartView);
            }
        }
    }

    private void HandleLivesChanged(int newLives)
    {
        if (!_isInitialized) return;
        _currentLives = newLives;
        RefreshAllHearts(false);
    }

    private void RefreshAllHearts(bool immediate)
    {
        if (_context == null) return;
        
        for (int i = 0; i < _heartPool.Count; i++)
        {
            int fill = 0;
            bool isExtraTier = (i > 0);

            if (i == 0)
            {
                // Coração Base: consome as primeiras 3 vidas (0, 1, 2, 3)
                fill = Mathf.Clamp(_currentLives, 0, 3);
            }
            else
            {
                // Corações Extras: cada um é uma vida inteira (após as 3 primeiras)
                // Vida 4 -> Coração index 1 (fill 3)
                // Vida 5 -> Coração index 2 (fill 3)
                int extraLifeIndex = i + 3; 
                fill = (_currentLives >= extraLifeIndex) ? 3 : 0;
            }

            Debug.Log($"[HeartDisplay] Heart {i}: Fill={fill}, Extra={isExtraTier}");
            _heartPool[i].SetState(fill, isExtraTier, immediate);
        }
    }

    public void ExpandMaxLives(int newMaxLives)
    {
        if (!_isInitialized) return;
        
        _maxLives = newMaxLives;
        
        // Cálculo: 1 (base) + (Vidas Extras que passarem de 3)
        int extraLives = Mathf.Max(0, _maxLives - 3);
        int neededHearts = 1 + extraLives;
        
        neededHearts = Mathf.Min(neededHearts, MAX_HEART_COUNT);

        if (neededHearts != _heartPool.Count)
        {
            CreateHeartPool(neededHearts);
            RefreshAllHearts(true);
        }
    }
}
