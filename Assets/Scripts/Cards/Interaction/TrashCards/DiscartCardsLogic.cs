using DG.Tweening;
using UnityEngine;

public enum TrashState
{
    Hidden,
    Appearing,
    Visible,
    Disappearing
}

/// <summary>
/// Lixeira física que aparece quando o jogador faz hover sobre o TrashIndicator.
/// Visual: Sprite aberto quando carta está sobre ela, fechado quando não.
/// </summary>
public class DiscartCardsLogic : MonoBehaviour, IDropTarget
{
    [Header("Configuração")]
    [SerializeField] private ParticleSystem _discartEffect;
    [SerializeField] private AudioClip OnDescartSound;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private bool _debugLogs = false;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer _trashSpriteRenderer;
    [SerializeField] private Sprite _trashOpenSprite;
    [SerializeField] private Sprite _trashClosedSprite;

    [Header("Animação")]
    [SerializeField] private Transform _trashContainer; // Transform da lixeira (world space)
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private float _scaleMultiplier = 1.2f; // Escala quando aparece

    [Header("Posição")]
    [SerializeField] private Vector3 _targetPosition = Vector3.zero; // Posição quando visível

    // Eventos
    public event System.Action<CardView> OnCardDiscarded;

    private TrashState _currentState = TrashState.Hidden;
    private bool _hasCardOver = false;
    private Vector3 _originalScale;

    public TrashState CurrentState => _currentState;

    private void Awake()
    {
        if (_trashContainer == null)
        {
            _trashContainer = transform;
        }

        _originalScale = _trashContainer.localScale;
        
        // Inicia invisível e pequena
        _trashContainer.localScale = Vector3.zero;
        UpdateSprite();
    }

    // --- CONTROLE PÚBLICO ---

    public void ShowTrash()
    {
        if (_currentState == TrashState.Hidden || _currentState == TrashState.Disappearing)
        {
            ChangeState(TrashState.Appearing);
        }
    }

    public void HideTrash()
    {
        if (_currentState == TrashState.Visible || _currentState == TrashState.Appearing)
        {
            ChangeState(TrashState.Disappearing);
        }
    }

    // --- ESTADOS ---

    private void EnterAppearing()
    {
        _trashContainer.DOKill();
        
        // Anima escala e posição
        _trashContainer.DOScale(_originalScale * _scaleMultiplier, _animationDuration)
            .SetEase(Ease.OutBack);
        
        _trashContainer.DOMove(_targetPosition, _animationDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => ChangeState(TrashState.Visible));
    }

    private void EnterVisible()
    {
        // Ajusta para escala normal
        _trashContainer.DOScale(_originalScale, _animationDuration * 0.5f)
            .SetEase(Ease.InOutQuad);
    }

    private void EnterDisappearing()
    {
        _trashContainer.DOKill();
        
        _trashContainer.DOScale(Vector3.zero, _animationDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => ChangeState(TrashState.Hidden));

        _hasCardOver = false;
        UpdateSprite();
    }

    private void EnterHidden()
    {
        _trashContainer.localScale = Vector3.zero;
    }

    // --- INTERFACE IDROPTARGET ---

    public bool CanReceive(IDraggable draggable)
    {
        // Só aceita se estiver visível
        if (_currentState != TrashState.Visible)
        {
            return false;
        }

        // Proteção de Estado - NÃO aceita durante Shopping
        var currentState = AppCore.Instance.GameStateManager.CurrentState;
        bool isAllowedState = (currentState == GameState.Playing);

        if (!isAllowedState)
        {
            return false;
        }

        // Validação de Tipo
        return draggable is CardView;
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            Debug.Log($"[TrashLogic] Descartando carta: {cardView.name}");
            
            // Remoção lógica e visual
            AppCore.Instance.Events.Player.TriggerCardRemoved(cardView.Instance);

            // Feedback visual
            if (_discartEffect != null)
            {
                _discartEffect.Play();
                _audioSource.PlayOneShot(OnDescartSound);
            }

            // Anima "mastigando" a carta
            _trashContainer.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);

            // Dispara evento
            OnCardDiscarded?.Invoke(cardView);

            _hasCardOver = false;
            UpdateSprite();
            
            // Esconde após o descarte
            HideTrash();
        }
    }

    // --- EVENTOS DE HOVER ---

    public void OnDragEnter(IDraggable draggable)
    {
        if (draggable is CardView && _currentState == TrashState.Visible)
        {
            _hasCardOver = true;
            UpdateSprite();
            
            // Feedback visual de "olhando pra carta"
            _trashContainer.DOKill();
            _trashContainer.DOScale(_originalScale * 1.1f, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    public void OnDragExit(IDraggable draggable)
    {
        if (draggable is CardView)
        {
            _hasCardOver = false;
            UpdateSprite();
            
            // Volta ao normal
            _trashContainer.DOKill();
            _trashContainer.DOScale(_originalScale, 0.2f).SetEase(Ease.InOutQuad);
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
            (TrashState.Visible, TrashState.Disappearing) => true,
            (TrashState.Disappearing, TrashState.Hidden) => true,
            (TrashState.Appearing, TrashState.Disappearing) => true, 
            _ => false
        };
    }

    private void ChangeState(TrashState newState)
    {
        if (!CanTransition(_currentState, newState))
        {
            return;
        }

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
            case TrashState.Disappearing:
                EnterDisappearing();
                break;
        }
    }
}
