using DG.Tweening;
using UnityEngine;

public enum TrashState
{
    Hidden,
    Appearing,
    Visible,
}
/// <summary>
/// Refatoração da lixeira...
/// Existencia: Só existe quando alguma carta esta sendo arrastada (o "botão" de trash também), se não estiver fazendo drag
/// a lixeira e a indicação de "TRASH" ficam escondidas
/// Visual: Se está em hover com carta -> Sprite Aberto, quando a carta vai fora -> sprite fechado
/// Refino: Quando o drag termina, não some instantâneo.
/// Inicia um timer curto. Se nenhum novo drag começar → esconde.
/// </summary>
public class DiscartCardsLogic : MonoBehaviour, IDropTarget, IDraggable
{
    [Header("Configuração")]
    [SerializeField] private ParticleSystem _DiscartEffect;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer _trashSpriteRenderer;
    [SerializeField] private Sprite _trashOpenSprite;
    [SerializeField] private Sprite _trashClosedSprite;

    [Header("Animação")]
    [SerializeField] private RectTransform _trashContainer;
    [SerializeField] private float _animationDuration = 0.3f;

    [Header("Posição")]
    [SerializeField] private Vector2 _hiddenPosition = new Vector2(-200f, 0f);
    [SerializeField] private Vector2 _visiblePosition = new Vector2(0f, 0f);

    [Header("Botão")]
    [SerializeField] TrashToggleButton _trashButton;

    // Eventos
    public event System.Action<CardView> OnCardDiscarded;
    public event System.Action OnTrashOpened;
    public event System.Action OnTrashClosed;

    private TrashState _currentState = TrashState.Hidden;
    private bool _hasCardOver = false;

    public TrashState CurrentState => _currentState;

    private void Start()
    {
        if (_trashContainer != null)
        {
            _trashContainer.anchoredPosition = _hiddenPosition;
        }

        if (_trashButton != null)
        {
            _trashButton.GetComponent<TrashToggleButton>();
        }

        UpdateSprite();
    }

    private void Update()
    {
        if (_currentState == TrashState.Visible)
        {
            CheckMouseDistance();
        }
    }

    // --- ESTADOS ---

    public void OnDragStart()
    {
        ShowTrashButton();
    }
    public void OnDragEnd()
    {
        InitializeDragTimer();
    }
    private void EnterVisible()
    {
        Debug.LogWarning("[TrashLogic] Estado: VISIBLE - Monitorando distância do mouse");
    }

    private void EnterAppearing()
    {
        Debug.LogWarning($"[TrashLogic] Estado: APPEARING - Movendo de {_trashContainer.anchoredPosition} para {_visiblePosition}");
        
        _trashContainer.DOKill();
        _trashContainer.DOAnchorPos(_visiblePosition, _animationDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => 
            {
                Debug.LogWarning("[TrashLogic] Animação APPEARING completa");
                ChangeState(TrashState.Visible);
            });

        OnTrashOpened?.Invoke();
    }

    private void EnterHidden()
    {
        Debug.LogWarning($"[TrashLogic] Estado: HIDDEN - Movendo para {_hiddenPosition}");
        
        _trashContainer.DOKill();
        _trashContainer.DOAnchorPos(_hiddenPosition, _animationDuration)
            .SetEase(Ease.InBack);

        _hasCardOver = false;
        UpdateSprite();

        OnTrashClosed?.Invoke();
    }

    // --- INTERFACE IDROPTARGET ---

    public bool CanReceive(IDraggable draggable)
    {
        // Só aceita se estiver visível 
        if (_currentState != TrashState.Visible)
        {
            Debug.LogWarning($"[TrashLogic] CanReceive = false - Estado atual: {_currentState}");
            return false;
        }

        // Proteção de Estado - NÃO aceita durante Shopping
        var currentState = AppCore.Instance.GameStateManager.CurrentState;
        bool isAllowedState = (currentState == GameState.Playing);

        if (!isAllowedState)
        {
            Debug.LogWarning($"[TrashLogic] CanReceive = false - GameState inválido: {currentState}");
            return false;
        }

        // Validação de Tipo
        bool isCardView = draggable is CardView;
        Debug.LogWarning($"[TrashLogic] CanReceive = {isCardView}");
        return isCardView;
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            Debug.LogWarning($"[TrashLogic] Descartando carta: {cardView.name}");
            
            // Remoção lógica e visual
            AppCore.Instance.Events.Player.TriggerCardRemoved(cardView.Instance);

            // Feedback visual
            if (_DiscartEffect != null)
            {
                _DiscartEffect.Play();
            }

            // Dispara evento
            OnCardDiscarded?.Invoke(cardView);

            _hasCardOver = false;
            UpdateSprite();
        }
    }

    // --- EVENTOS DE HOVER ---

    public void OnDragEnter(IDraggable draggable)
    {
        if (draggable is CardView)
        {
            Debug.LogWarning("[TrashLogic] Drag ENTER - Abrindo lixeira");
            _hasCardOver = true;
            UpdateSprite();
        }
    }

    public void OnDragExit(IDraggable draggable)
    {
        if (draggable is CardView)
        {
            Debug.LogWarning("[TrashLogic] Drag EXIT - Fechando lixeira");
            _hasCardOver = false;
            UpdateSprite();
        }
    }

    // --- CONTROLE PÚBLICO ---

    public void ShowTrash()
    {
        Debug.LogWarning($"[TrashLogic] ShowTrash() chamado - Estado atual: {_currentState}");
        
        if (_currentState == TrashState.Hidden)
        {
            ChangeState(TrashState.Appearing);
        }
        else
        {
            Debug.LogWarning($"[TrashLogic] ShowTrash() ignorado - já está em {_currentState}");
        }
    }

    public void HideTrash()
    {
        Debug.LogWarning($"[TrashLogic] HideTrash() chamado - Estado atual: {_currentState}");
        
        if (_currentState != TrashState.Hidden)
        {
            ChangeState(TrashState.Hidden);
        }
    }

    public void ToggleTrash()
    {   
        // Evita toggle durante animação de aparecer
        if (_currentState == TrashState.Appearing) 
        {
            Debug.LogWarning("[TrashLogic] Toggle ignorado - animação em progresso");
            return;
        }

        if (_currentState == TrashState.Hidden)
        {
            ShowTrash();
        }
        else
        {
            HideTrash();
        }
    }

    // --- ATUALIZAÇÃO VISUAL ---

    private void UpdateSprite()
    {
        if (_trashSpriteRenderer == null) return;

        _trashSpriteRenderer.sprite = _hasCardOver ? _trashOpenSprite : _trashClosedSprite;
    }

    // --- STATE MACHINE ---

    private bool CanTransition(TrashState from, TrashState to)
    {
        if (from == to) return false;

        return (from, to) switch
        {
            (TrashState.Hidden, TrashState.Appearing) => true,
            (TrashState.Appearing, TrashState.Visible) => true,
            (TrashState.Visible, TrashState.Hidden) => true,
            (TrashState.Appearing, TrashState.Hidden) => true, 
            _ => false
        };
    }

    private void ChangeState(TrashState newState)
    {
        if (!CanTransition(_currentState, newState))
        {
            Debug.LogWarning($"[TrashLogic] Transição inválida: {_currentState} -> {newState}");
            return;
        }

        Debug.LogWarning($"[TrashLogic] Mudando estado: {_currentState} -> {newState}");
        _currentState = newState;

        switch (_currentState)
        {
            case TrashState.Hidden:
                EnterHidden();
                break;
            case TrashState.Appearing:
                EnterAppearing();
                break;
            case TrashState.Visible:
                EnterVisible();
                break;
        }
    }
}
