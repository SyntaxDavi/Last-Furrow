using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controlador de ambiente top-down: chão + objetos decorativos.
/// 
/// RESPONSABILIDADE:
/// - Spawnar chão base (grama/terra)
/// - Spawnar props individuais (pedras, árvores, arbustos)
/// - Manter pixel perfect (PPU=32)
/// - MODO AUTO: Calcula tamanho da câmera automaticamente
/// 
/// USO:
/// 1. Atribua sprite do chão
/// 2. Marque "Auto Fit Camera" para preencher tela inteira
/// 3. Play = ambiente spawna automaticamente
/// </summary>
public class BackgroundController : MonoBehaviour
{
    [Header("Chão Base")]
    [Tooltip("Sprite do chão (grama, terra)")]
    [SerializeField] private Sprite _groundSprite;
    
    [Tooltip("AUTO: Preenche tela inteira | MANUAL: usa Ground Size")]
    [SerializeField] private bool _autoFitCamera = true;
    
    [Tooltip("(Apenas se Auto Fit desligado) Tamanho manual")]
    [SerializeField] private Vector2 _groundSize = new Vector2(12f, 8f);

    [Header("Props Decorativos (Pedras, Árvores)")]
    [Tooltip("Lista de objetos ao redor do grid")]
    [SerializeField] private List<PropSetup> _props = new List<PropSetup>();

    [Header("Config")]
    [SerializeField] private PixelArtConfig _pixelConfig;
    [SerializeField] private bool _snapToPixels = true;

    private SpriteRenderer _groundRenderer;
    private List<SpriteRenderer> _spawnedProps = new List<SpriteRenderer>();
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        SpawnEnvironment();
    }

    public void SpawnEnvironment()
    {
        SpawnGround();
        SpawnProps();
    }

    private void SpawnGround()
    {
        if (_groundSprite == null) return;

        GameObject groundObj = new GameObject("Ground");
        groundObj.transform.SetParent(transform);
        groundObj.transform.localPosition = Vector3.zero;

        _groundRenderer = groundObj.AddComponent<SpriteRenderer>();
        _groundRenderer.sprite = _groundSprite;
        _groundRenderer.sortingLayerName = "Background";
        _groundRenderer.sortingOrder = -100;

        // ? MODO AUTO: Calcula tamanho exato da câmera
        if (_autoFitCamera && _mainCamera != null)
        {
            Vector2 cameraSize = CalculateCameraSize();
            _groundRenderer.drawMode = SpriteDrawMode.Sliced;
            _groundRenderer.size = cameraSize;
            
            Debug.Log($"[BackgroundController] AUTO-FIT: Camera size = {cameraSize}");
        }
        else
        {
            // MODO MANUAL: Usa valor do Inspector
            _groundRenderer.drawMode = SpriteDrawMode.Tiled;
            _groundRenderer.size = _groundSize;
        }

        if (_groundSprite.texture.filterMode != FilterMode.Point)
            _groundSprite.texture.filterMode = FilterMode.Point;
    }
    
    /// <summary>
    /// Calcula tamanho da câmera em unidades Unity.
    /// </summary>
    private Vector2 CalculateCameraSize()
    {
        if (_mainCamera == null)
        {
            Debug.LogWarning("[BackgroundController] Camera não encontrada! Usando tamanho padrão.");
            return new Vector2(12f, 8f);
        }

        float height = _mainCamera.orthographicSize * 2f;
        float width = height * _mainCamera.aspect;
        
        return new Vector2(width, height);
    }

    private void SpawnProps()
    {
        foreach (var prop in _props)
        {
            if (prop.Sprite == null) continue;

            GameObject propObj = new GameObject($"Prop_{prop.Name}");
            propObj.transform.SetParent(transform);

            Vector3 position = prop.Position;
            if (_snapToPixels && _pixelConfig != null)
            {
                position = _pixelConfig.SnapPosition(position);
            }
            propObj.transform.position = position;

            var sr = propObj.AddComponent<SpriteRenderer>();
            sr.sprite = prop.Sprite;
            sr.sortingLayerName = prop.InFrontOfGrid ? "Default" : "Background";
            sr.sortingOrder = prop.InFrontOfGrid ? 10 : -50;

            if (prop.Sprite.texture.filterMode != FilterMode.Point)
                prop.Sprite.texture.filterMode = FilterMode.Point;

            _spawnedProps.Add(sr);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Área do chão
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(transform.position, new Vector3(_groundSize.x, _groundSize.y, 0.1f));

        // Posições dos props
        foreach (var prop in _props)
        {
            Gizmos.color = prop.InFrontOfGrid ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(prop.Position, 0.2f);
            UnityEditor.Handles.Label(prop.Position + Vector3.up * 0.3f, prop.Name);
        }
    }
#endif
}

[System.Serializable]
public class PropSetup
{
    public string Name = "Prop";
    public Sprite Sprite;
    public Vector3 Position;
    [Tooltip("Se true, renderiza na frente do grid (foreground)")]
    public bool InFrontOfGrid = false;
}
