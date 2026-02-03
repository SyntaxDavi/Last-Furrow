using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controlador de ambiente top-down: chão + objetos decorativos.
/// 
/// RESPONSABILIDADE:
/// - Spawnar chão base (grama/terra)
/// - Spawnar props individuais (pedras, árvores, arbustos)
/// - Interpolação suave (PPU=24 conforme GameSettings)
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
    
    [Tooltip("AUTO: Preenche tela + margem de scroll | MANUAL: usa Ground Size")]
    [SerializeField] private bool _autoFitCamera = true;

    [Tooltip("Margem extra para que o chão não acabe quando a câmera se mover (Edge Scroll)")]
    [SerializeField] private float _scrollSafetyMargin = 5.0f;
    
    [Tooltip("(Apenas se Auto Fit desligado) Tamanho manual")]
    [SerializeField] private Vector2 _groundSize = new Vector2(12f, 8f);

    [Header("Parallax (Profundidade)")]
    [Tooltip("Se > 0, o background se move levemente com a câmera. 0.1f é um bom valor.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float _parallaxEffect = 0.05f;

    [Header("Props Decorativos (Pedras, Árvores)")]
    [Tooltip("Lista de objetos ao redor do grid")]
    [SerializeField] private List<PropSetup> _props = new List<PropSetup>();

    private SpriteRenderer _groundRenderer;
    private List<SpriteRenderer> _spawnedProps = new List<SpriteRenderer>();
    private Camera _mainCamera;
    private Vector3 _lastCameraPos;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera != null) _lastCameraPos = _mainCamera.transform.position;
        SpawnEnvironment();
    }

    private void LateUpdate()
    {
        if (_parallaxEffect > 0 && _mainCamera != null)
        {
            Vector3 delta = _mainCamera.transform.position - _lastCameraPos;
            // Move o background na mesma direção da câmera, mas mais devagar,
            // criando a ilusão de que está "mais longe".
            transform.position += delta * _parallaxEffect;
            _lastCameraPos = _mainCamera.transform.position;
        }
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

        if (_autoFitCamera && _mainCamera != null)
        {
            Vector2 cameraSize = CalculateCameraSize();
            // Adicionamos a margem de segurança para o scroll
            Vector2 finalSize = cameraSize + new Vector2(_scrollSafetyMargin * 2, _scrollSafetyMargin * 2);
            
            _groundRenderer.drawMode = SpriteDrawMode.Sliced;
            _groundRenderer.size = finalSize;
            
            Debug.Log($"[BackgroundController] AUTO-FIT: Camera size {cameraSize} + Margin {_scrollSafetyMargin} = {finalSize}");
        }
        else
        {
            _groundRenderer.drawMode = SpriteDrawMode.Tiled;
            _groundRenderer.size = _groundSize;
        }

        if (_groundSprite.texture.filterMode != FilterMode.Point)
            _groundSprite.texture.filterMode = FilterMode.Point;
    }
    
    private Vector2 CalculateCameraSize()
    {
        if (_mainCamera == null) return new Vector2(12f, 8f);

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

            propObj.transform.position = prop.Position;

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
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Vector2 drawSize = _autoFitCamera ? CalculateCameraSize() + new Vector2(_scrollSafetyMargin * 2, _scrollSafetyMargin * 2) : _groundSize;
        Gizmos.DrawCube(transform.position, new Vector3(drawSize.x, drawSize.y, 0.1f));

        foreach (var prop in _props)
        {
            Gizmos.color = prop.InFrontOfGrid ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(prop.Position, 0.2f);
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
