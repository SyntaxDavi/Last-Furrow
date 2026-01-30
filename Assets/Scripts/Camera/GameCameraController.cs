using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Controlador de câmera para jogos de pixel art.
/// 
/// ARQUITETURA:
/// - Usa Strategy Pattern (ICameraFitStrategy) para calcular bounds
/// - Interpolação suave (Movimento fluido e diagonal)
/// - PPU oficial = 24 (conforme GameSettings)
/// - Sem zoom (simplificado conforme requisitos)
/// 
/// FILOSOFIA:
/// - "Mundo é protagonista" ? Câmera enquadra o mundo, não a UI
/// - "Grid é estrutura" ? Grid centralizado, protegido
/// - "Espaço artístico" ? Padding generoso para ambientação
/// </summary>
[RequireComponent(typeof(Camera))]
public class GameCameraController : MonoBehaviour
{
    [Header("Dependências")]
    [SerializeField] private PixelArtConfig _pixelConfig;
    [SerializeField] private CameraFramingConfig _framingConfig;

    [Header("Movimento")]
    [SerializeField] private float _moveSmoothTime = 0.25f;
    [SerializeField] private bool _snapPositionToPixels = GameSettings.USE_PIXEL_PERFECT;

    private Camera _cam;
    private ICameraFitStrategy _fitStrategy;

    // Estado interno
    private Vector3 _basePosition;
    private Vector3 _dynamicOffset;
    private Vector3 _shakeOffset;
    private float _dynamicRotation; // Z-axis (Inertia)
    private Vector2 _dynamicTilt;   // X and Y axis (Perspective)
    private float _dynamicSizeOffset;
    private Vector3 _currentVelocity;
    private Coroutine _moveRoutine;
    private DG.Tweening.Tween _shakeTween;
    private bool _isConfigured = false;

    // Cache configuração atual (para detectar mudanças)
    private GridConfiguration _lastGridConfig;
    private Vector2 _lastGridSpacing;

    // Cache para debug/gizmos
    private Bounds _lastGridBounds;
    private Bounds _lastCameraBounds;
    private float _baseOrthoSize;

    /// <summary>
    /// Posição base definida pelo enquadramento do Grid.
    /// </summary>
    public Vector3 CameraPosition
    {
        get => _basePosition;
        set
        {
            _basePosition = value;
            ApplyFinalPosition();
        }
    }

    /// <summary>
    /// Offset dinâmico (Scroll) que não altera a base.
    /// </summary>
    public Vector3 DynamicOffset
    {
        get => _dynamicOffset;
        set
        {
            _dynamicOffset = value;
            ApplyFinalPosition();
        }
    }

    /// <summary>
    /// Rotação dinâmica (Tilt Z) para "Juice".
    /// </summary>
    public float DynamicRotation
    {
        get => _dynamicRotation;
        set
        {
            _dynamicRotation = value;
            ApplyFinalPosition();
        }
    }

    /// <summary>
    /// Inclinação 3D (X e Y) para efeito de "espiar".
    /// </summary>
    public Vector2 DynamicTilt
    {
        get => _dynamicTilt;
        set
        {
            _dynamicTilt = value;
            ApplyFinalPosition();
        }
    }

    /// <summary>
    /// Offset de zoom dinâmico.
    /// </summary>
    public float DynamicSizeOffset
    {
        get => _dynamicSizeOffset;
        set
        {
            _dynamicSizeOffset = value;
            ApplyFinalPosition();
        }
    }

    /// <summary>
    /// Executa um shake na câmera usando um offset interno para não conflitar com o movimento.
    /// </summary>
    public void DoShake(float duration, float strength)
    {
        _shakeTween?.Kill();
        
        // Usamos uma força que decai ao longo do tempo
        float currentStrength = strength;
        _shakeTween = DOTween.To(() => currentStrength, x => {
            currentStrength = x;
            // Gera um offset aleatório baseado na força atual
            // Multiplicamos por um valor fixo para dar mais "juice" se necessário
            _shakeOffset = (Vector3)Random.insideUnitCircle * currentStrength;
            ApplyFinalPosition();
        }, 0f, duration)
        .SetEase(Ease.OutQuad)
        .OnComplete(() => {
            _shakeOffset = Vector3.zero;
            ApplyFinalPosition();
            _shakeTween = null;
        });
    }

    private void ApplyFinalPosition()
    {
        Vector3 finalPos = _basePosition + _dynamicOffset + _shakeOffset;

        // 1. Aplica Posição (Snap)
        if (_snapPositionToPixels && _pixelConfig != null)
        {
            transform.position = _pixelConfig.SnapPosition(finalPos);
        }
        else
        {
            transform.position = finalPos;
        }

        // 2. Aplica Rotação (Tilt 3D)
        // X tilt: Rotação no eixo X faz olhar pra Cima/Baixo
        // Y tilt: Rotação no eixo Y faz olhar pra Esquerda/Direita
        // Z rotation: Rotação no eixo Z é a inércia lateral
        transform.localRotation = Quaternion.Euler(_dynamicTilt.x, _dynamicTilt.y, _dynamicRotation);

        // 3. Aplica Zoom Dinâmico
        if (_cam != null)
        {
            _cam.orthographicSize = _baseOrthoSize + _dynamicSizeOffset;
        }
    }

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _fitStrategy = new PaddedGridFitStrategy();

        ValidateDependencies();
    }

    private void OnEnable()
    {
        // ⭐ RECÁLCULO AUTOMÁTICO: Registra eventos de mudança de grid
        if (AppCore.Instance != null && AppCore.Instance.Events != null)
        {
            // Futuro: AppCore.Instance.Events.Grid.OnGridResized += HandleGridResized;
            // Por enquanto, apenas validamos na configuração inicial
        }
    }

    private void OnDisable()
    {
        // Limpa eventos
        if (AppCore.Instance != null && AppCore.Instance.Events != null)
        {
            // AppCore.Instance.Events.Grid.OnGridResized -= HandleGridResized;
        }
    }

    private void ValidateDependencies()
    {
        if (_pixelConfig == null)
        {
            Debug.LogError("[GameCamera] ⚠️ PixelConfig AUSENTE! Atribua no Inspector ou câmera não funcionará corretamente.");
        }

        if (_framingConfig == null)
        {
            Debug.LogWarning("[GameCamera] ⚠️ CameraFramingConfig AUSENTE! Criando fallback temporário com padding padrão.");
            Debug.LogWarning("[GameCamera] → AÇÃO NECESSÁRIA: Crie um CameraFramingConfig e atribua no Inspector!");
            
            // Fallback explícito com valores seguros
            _framingConfig = ScriptableObject.CreateInstance<CameraFramingConfig>();
            _framingConfig.PaddingLeft = 3f;
            _framingConfig.PaddingRight = 3f;
            _framingConfig.PaddingTop = 3.5f;
            _framingConfig.PaddingBottom = 2f;
        }
    }

    /// <summary>
    /// Configura câmera usando GridConfiguration diretamente (SOLID).
    /// 
    /// ⭐ NOVA ASSINATURA: Não depende de GridManager.GetGridWorldSize()
    /// Recebe dados puros e usa Strategy para calcular.
    /// 
    /// ⭐ VALIDAÇÃO: Detecta se configuração mudou e avisa.
    /// </summary>
    public void ConfigureFromGrid(
        GridConfiguration gridConfig,
        Vector2 gridSpacing,
        ICameraFitStrategy customStrategy = null)
    {
        // ⭐ FORCE INIT: Garante que Awake() rodou
        if (_cam == null || _fitStrategy == null)
        {
            Debug.LogWarning("[GameCamera] ⚠️ Awake() não rodou ainda! Inicializando manualmente.");
            _cam = GetComponent<Camera>();
            _fitStrategy = new PaddedGridFitStrategy();
        }

        // ⭐ EARLY VALIDATION: Garante que dependências foram inicializadas
        if (_framingConfig == null)
        {
            Debug.LogWarning("[GameCamera] ⚠️ Dependências não inicializadas. Chamando ValidateDependencies().");
            ValidateDependencies();
        }

        if (gridConfig == null)
        {
            Debug.LogError("[GameCamera] ⚠️ GridConfiguration é null! Não é possível configurar câmera.");
            return;
        }

        // ⭐ DETECÇÃO DE MUDANÇA: Avisa se grid foi reconfigurado
        if (_isConfigured && (_lastGridConfig != gridConfig || _lastGridSpacing != gridSpacing))
        {
            Debug.LogWarning(
                "[GameCamera] ⚠️ Grid foi RECONFIGURADO durante runtime!\n" +
                $"Anterior: {_lastGridConfig?.Columns}×{_lastGridConfig?.Rows}, Spacing {_lastGridSpacing}\n" +
                $"Novo: {gridConfig.Columns}×{gridConfig.Rows}, Spacing {gridSpacing}\n" +
                "→ Recalculando câmera automaticamente."
            );
        }

        // Armazena configuração atual
        _lastGridConfig = gridConfig;
        _lastGridSpacing = gridSpacing;
        _isConfigured = true;

        // Usa estratégia customizada ou padrão
        var strategy = customStrategy ?? _fitStrategy;

        // 1. Calcula bounds necessários
        var (width, height) = strategy.CalculateRequiredBounds(
            gridConfig, 
            gridSpacing, 
            _framingConfig
        );

        // 2. Armazena bounds para debug/gizmos
        _lastGridBounds = new Bounds(
            Vector3.zero,
            new Vector3(
                gridConfig.Columns * gridSpacing.x,
                gridConfig.Rows * gridSpacing.y,
                0
            )
        );

        _lastCameraBounds = new Bounds(
            Vector3.zero,
            new Vector3(width, height, 0)
        );

        // 3. Posiciona câmera centralizada (⭐ USA PROPERTY COM SNAP)
        CameraPosition = new Vector3(0, 0, -10f);

        // 4. Aplica tamanho com pixel perfect
        FitBounds(width, height);

        Debug.Log($"[GameCamera] Configurada: Grid {gridConfig.Columns}×{gridConfig.Rows}, Bounds {width:F2}×{height:F2}");
    }

    /// <summary>
    /// LEGACY: Mantém compatibilidade com código antigo.
    /// ?? DEPRECATED: Use ConfigureFromGrid() em novo código.
    /// </summary>
    public void Configure(float gridWidth, float gridHeight)
    {
        transform.position = new Vector3(0, 0, -10f);
        if (_cam == null) _cam = GetComponent<Camera>();
        FitBounds(gridWidth, gridHeight);
    }

    /// <summary>
    /// Ajusta orthographic size para enquadrar bounds com pixel perfect.
    /// 
    /// PIXEL PERFECT:
    /// - Converte world units -> pixels
    /// - PPU oficial = 24 (conforme GameSettings)
    /// - Converte de volta -> orthographic size
    /// </summary>
    private void FitBounds(float width, float height)
    {
        if (_pixelConfig == null)
        {
            Debug.LogError("[GameCamera] PixelConfig não atribuído no Inspector!");
            return;
        }

        if (_cam == null)
        {
            Debug.LogError("[GameCamera] Componente Camera não encontrado!");
            return;
        }


        // 1. Determina qual dimensão limita (aspect ratio)
        float targetRatio = width / height;
        float cameraRatio = _cam.aspect;
        float requiredHeightInUnits;

        if (cameraRatio >= targetRatio)
        {
            // Câmera é mais larga → altura limita
            requiredHeightInUnits = height;
        }
        else
        {
            // Câmera é mais estreita → largura limita, compensa na altura
            requiredHeightInUnits = width / cameraRatio;
        }

        // 2. Converte para pixels
        float requiredHeightInPixels = requiredHeightInUnits * _pixelConfig.PPU;

        // 3. Determina altura final em pixels
        float snappedHeightPixels = Mathf.Ceil(requiredHeightInPixels);
        
        // Snap desativado por padrão para permitir interpolação suave
        if (_framingConfig.SnapToMultipleOf4 && GameSettings.USE_PIXEL_PERFECT)
        {
            // Arredonda para próximo múltiplo de 4
            float remainder = snappedHeightPixels % 4f;
            if (remainder != 0)
            {
                snappedHeightPixels += (4f - remainder);
            }
        }

        // 4. Converte de volta para Orthographic Size
        // Orthographic Size = Metade da altura visível
        float finalSize = (snappedHeightPixels / _pixelConfig.PPU) / 2f;
        _baseOrthoSize = finalSize;
        _cam.orthographicSize = _baseOrthoSize + _dynamicSizeOffset;

        Debug.Log(
            $"[GameCamera] FitBounds: {width:F2}×{height:F2} units → " +
            $"{snappedHeightPixels}px altura → Size {finalSize:F3}"
        );
    }

    // --- MOVIMENTO ---

    public void PanTo(Vector3 targetPosition, float duration = -1f)
    {
        targetPosition.z = transform.position.z;

        if (_moveRoutine != null) StopCoroutine(_moveRoutine);

        float time = duration > 0 ? duration : _moveSmoothTime;
        _moveRoutine = StartCoroutine(PanRoutine(targetPosition, time));
    }

    private IEnumerator PanRoutine(Vector3 targetPos, float smoothTime)
    {
        // Enquanto não estivermos "no pixel exato" do destino
        while (Vector3.Distance(transform.position, targetPos) > (1f / _pixelConfig.PPU))
        {
            // 1. Calcula posição lógica (Float)
            Vector3 nextPos = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref _currentVelocity,
                smoothTime
            );

            // 2. ⭐ APLICA SNAP VIA PROPERTY
            CameraPosition = nextPos;

            // 3. Notifica Anchors
            NotifyUpdate();

            yield return null;
        }

        // Chegada Final
        CameraPosition = targetPos; // ⭐ USA PROPERTY
        NotifyUpdate();
        _moveRoutine = null;
    }

    private void NotifyUpdate()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.Camera.TriggerCameraUpdated();
    }

    /// <summary>
    /// ⭐ BOUNDS REAIS: Calcula área visível REAL considerando aspect ratio.
    /// 
    /// USO:
    /// - Debug visual (gizmos)
    /// - Spawn de efeitos nas bordas
    /// - Não é usado para clamp (câmera é estática)
    /// </summary>
    public Bounds GetVisibleWorldBounds()
    {
        if (_cam == null) return new Bounds();

        float height = _cam.orthographicSize * 2f;
        float width = height * _cam.aspect;

        return new Bounds(
            transform.position + Vector3.forward * 10f, // Ajusta Z para match
            new Vector3(width, height, 0)
        );
    }

    // --- DEBUG / GIZMOS ---

    private void OnDrawGizmos()
    {
        if (_framingConfig == null || !_framingConfig.ShowDebugBounds) return;

        // Desenha bounds do grid (verde)
        if (_lastGridBounds.size.x > 0)
        {
            Gizmos.color = _framingConfig.GridBoundsColor;
            Gizmos.DrawWireCube(_lastGridBounds.center, _lastGridBounds.size);
        }

        // ⭐ Desenha bounds REAIS da câmera (ciano)
        var realBounds = GetVisibleWorldBounds();
        if (realBounds.size.x > 0)
        {
            Gizmos.color = _framingConfig.CameraBoundsColor;
            Gizmos.DrawWireCube(realBounds.center, realBounds.size);
        }
    }
}