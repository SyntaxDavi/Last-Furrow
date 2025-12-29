using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class GridSlotView : MonoBehaviour, IInteractable
{
    [Header("Referencias Visuais")]
    [SerializeField] private SpriteRenderer _baseRenderer;
    [SerializeField] private SpriteRenderer _plantRenderer;

    [Header("Feedback")]
    [SerializeField] private Color _hoverColor = new Color(0.8f, 1f, 0.8f);
    private Color _defaultColor;

    public int SlotIndex { get; private set; }

    private void Awake()
    {
        // 1. Auto-correção de referências
        if (_baseRenderer == null) _baseRenderer = GetComponent<SpriteRenderer>();
        _defaultColor = _baseRenderer.color;

        // 2. Criação segura do objeto da planta
        if (_plantRenderer == null || _plantRenderer == _baseRenderer)
        {
            CreatePlantSpriteObject();
        }

        // 3. Configuração de Layer e Desativação Inicial (Otimização)
        _plantRenderer.sortingLayerID = _baseRenderer.sortingLayerID;
        _plantRenderer.sortingOrder = _baseRenderer.sortingOrder + 1;
        _plantRenderer.enabled = false; // Começa desligado!
    }

    private void CreatePlantSpriteObject()
    {
        GameObject plantObj = new GameObject("PlantSprite");
        plantObj.transform.SetParent(this.transform);
        plantObj.transform.localPosition = Vector3.zero;

        float parentScaleY = transform.localScale.y;
        float counterScale = 1f / parentScaleY;

        // Aplica escala normal no X, e compensada no Y
        plantObj.transform.localScale = new Vector3(1, counterScale, 1);

        _plantRenderer = plantObj.AddComponent<SpriteRenderer>();

        plantObj.transform.localPosition = new Vector3(0, 0.2f, 0);
    }

    public void Initialize(int index)
    {
        SlotIndex = index;
        ResetVisuals();
    }

    //  MUDANÇA ARQUITETURAL: Agora recebe o Sprite, não o State.
    // O GridManager que se vire para achar o sprite na Library.
    // Isso remove a dependência de GameLibrary daqui.
    public void SetPlantVisual(Sprite sprite)
    {
        if (sprite == null)
        {
            ResetPlantVisual();
            return;
        }

        _plantRenderer.sprite = sprite;
        _plantRenderer.color = Color.white;
        _plantRenderer.enabled = true; 
    }

    // Limpa TUDO (Cor da base e Sprite da planta)
    public void ResetVisuals()
    {
        ResetPlantVisual();

        if (_baseRenderer != null) _baseRenderer.color = _defaultColor;
    }

    private void ResetPlantVisual()
    {
        if (_plantRenderer != null)
        {
            _plantRenderer.sprite = null;
            _plantRenderer.enabled = false; 
        }
    }

    // --- IInteractable ---
    public void OnHoverEnter()
    {
        if (_baseRenderer != null) _baseRenderer.color = _hoverColor;
    }

    public void OnHoverExit()
    {
        if (_baseRenderer != null) _baseRenderer.color = _defaultColor;
    }

    public void OnClick()
    {
        Debug.Log($"Slot {SlotIndex} clicado.");
    }
}