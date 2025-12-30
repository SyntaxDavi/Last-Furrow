using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class GameCameraController : MonoBehaviour
{
    [Header("Dependências")]
    [SerializeField] private PixelArtConfig _pixelConfig;

    [Header("Movimento")]
    [SerializeField] private float _moveSmoothTime = 0.25f;

    private Camera _cam;
    private float _baseOrthographicSize;
    private int _currentZoomLevel = 1;

    // Estado interno
    private Vector3 _currentVelocity;
    private Coroutine _moveRoutine;

    // Cache da última posição para notificar anchors apenas se moveu
    private Vector3 _lastFramePosition;

    private void Awake()
    {
        _cam = GetComponent<Camera>();

        if (_pixelConfig == null)
        {
            Debug.LogError("[GameCamera] PixelConfig ausente! Atribua no Inspector.");
        }
    }

    /// <summary>
    /// Configura a câmera para enquadrar o Grid respeitando o PPU.
    /// </summary>
    public void Configure(float gridWidth, float gridHeight)
    {
        transform.position = new Vector3(0, 0, -10f);

        if (_cam == null) _cam = GetComponent<Camera>();
        FitGrid(gridWidth, gridHeight);
    }

    public void FitGrid(float gridWidth, float gridHeight)
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


        // 1. Calcula o tamanho lógico necessário em Units
        float targetRatio = gridWidth / gridHeight;
        float cameraRatio = _cam.aspect;
        float requiredHeightInUnits;

        if (cameraRatio >= targetRatio)
            requiredHeightInUnits = gridHeight; // Trava na altura
        else
            requiredHeightInUnits = gridHeight * (targetRatio / cameraRatio); // Compensa largura

        // 2. Converte para Pixels e Arredonda (SNAP DE TAMANHO)
        // Isso garante que o tamanho da câmera case perfeitamente com o PPU
        float requiredHeightInPixels = requiredHeightInUnits * _pixelConfig.PPU;

        // Arredonda para o par mais próximo para manter centro alinhado (opcional, mas recomendado)
        // ou Mathf.Ceil se não quiser cortar nada.
        float snappedHeightPixels = Mathf.Ceil(requiredHeightInPixels);

        // Se for ímpar, soma 1 para garantir centro perfeito em PPU pares (comum)
        if (snappedHeightPixels % 2 != 0) snappedHeightPixels += 1;

        // 3. Converte de volta para Orthographic Size
        // Size = Altura / 2
        _baseOrthographicSize = (snappedHeightPixels / _pixelConfig.PPU) / 2f;

        ApplyZoom();
    }

    // --- ZOOM DISCRETO ---

    /// <summary>
    /// Define o zoom por níveis inteiros (1x, 2x, 3x).
    /// Zoom contínuo é proibido em Pixel Art estrito.
    /// </summary>
    public void SetZoomLevel(int level)
    {
        if (level < 1) level = 1;
        _currentZoomLevel = level;
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        // Em pixel art, Zoom out = aumentar size. Zoom in = diminuir size.
        // Aqui assumimos que Level 1 = Tamanho Base (Enquadra tudo)
        // Level 2 = Metade do tamanho (Zoom In 2x)

        // CUIDADO: Se você quer Zoom IN, você divide o Size.
        // Se quer Zoom OUT, você multiplica.
        // Vamos assumir Zoom In para focar em detalhes.

        float zoomedSize = _baseOrthographicSize / _currentZoomLevel;

        // RE-SNAP: O novo tamanho dividido também precisa cair no grid de pixels?
        // Sim. Verificamos se gerou fração.
        float zoomedPixels = (zoomedSize * 2f) * _pixelConfig.PPU;

        // Se der número quebrado, ajusta para o inteiro mais próximo
        zoomedPixels = Mathf.Round(zoomedPixels);

        _cam.orthographicSize = (zoomedPixels / _pixelConfig.PPU) / 2f;

        // Notifica sistema
        NotifyUpdate();
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

            // 2. Aplica SNAP de Posição
            transform.position = _pixelConfig.SnapPosition(nextPos);

            // 3. Notifica Anchors
            NotifyUpdate();

            yield return null;
        }

        // Chegada Final
        transform.position = _pixelConfig.SnapPosition(targetPos);
        NotifyUpdate();
        _moveRoutine = null;
    }

    private void NotifyUpdate()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.Camera.TriggerCameraUpdated();
    }
}