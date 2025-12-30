using UnityEngine;

public class GridSlotView : MonoBehaviour, IInteractable, IDropTarget
{
    [Header("Visuais")]
    [SerializeField] private SpriteRenderer _baseRenderer;
    [SerializeField] private SpriteRenderer _plantRenderer;

    public int SlotIndex { get; private set; }

    public void Initialize(int index)
    {
        SlotIndex = index;
        ResetVisuals();
    }

    private void Awake()
    {
        // 1. Auto-correção de referências
        if (_baseRenderer == null) _baseRenderer = GetComponent<SpriteRenderer>();

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
    public bool CanReceive(IDraggable draggable)
    {
        // Só aceita se for uma carta com dados válidos
        if (draggable is CardView card && card.Data != null)
        {
            // Acessa o dado real. Como é memória (não disco), é rápido checar todo frame.
            var slotState = AppCore.Instance.SaveManager.Data.CurrentRun.GridSlots[SlotIndex];
            bool isOccupied = !string.IsNullOrEmpty(slotState.CropID);

            if (card.Data.Type == CardType.Plant)
                return !isOccupied; // Planta exige vazio

            if (card.Data.Type == CardType.Modify || card.Data.Type == CardType.Harvest)
                return isOccupied; // Ação exige planta existente
        }
        return false;
    }
    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            // A View não sabe o que vai acontecer. Ela só avisa:
            // "Jogaram os DADOS dessa carta no meu índice."
            AppCore.Instance.Events.TriggerCardDroppedOnSlot(SlotIndex, cardView.Data);
        }
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
    // --- VISUALIZAÇÃO ---
    // Chamado pelo GridManager quando houver atualização
    public void SetPlantVisual(Sprite sprite)
    {
        if (sprite == null)
        {
            _plantRenderer.sprite = null;
            _plantRenderer.enabled = false;
            return;
        }
        _plantRenderer.sprite = sprite;
        _plantRenderer.enabled = true;
    }
    private void ResetVisuals()
    {
        SetPlantVisual(null);
    }

    // --- IInteractable ---
    public void OnHoverEnter() => _baseRenderer.color = new Color(0.8f, 1f, 0.8f);
    public void OnHoverExit() => _baseRenderer.color = _baseRenderer.color;

    public void OnClick()
    {
        Debug.Log($"Slot {SlotIndex} clicado.");
    }
}