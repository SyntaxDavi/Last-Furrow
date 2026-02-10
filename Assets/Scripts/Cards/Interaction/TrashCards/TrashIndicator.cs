using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Indicador visual "TRASH" que aparece quando o jogador começa a arrastar uma carta.
/// Serve como zona de hover para mostrar a lixeira física.
/// </summary>
/// 
public class TrashIndicator : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _trashIcon;

    [Header("Configuração")]
    [SerializeField] private float _fadeDuration = 0.2f;
    [SerializeField] private float _hideDelay = 2f;

    [Header("Referências")]
    [SerializeField] private DiscartCardsLogic _trashLogic;

    private Coroutine _hideCoroutine;
    private bool _isVisible = false;
    private DragDropSystem _dragDropSystem;
    public int InteractionPriority => 500; // Prioridade intermediária

    private void Awake()
    {
        if(_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        SubscribeToDragEvents();
    }
    private void Start()
    {
        if (AppCore.Instance?.InteractionManager != null)
        {
            _dragDropSystem = AppCore.Instance.DragDropSystem;

            if (_dragDropSystem != null)
            {
                _dragDropSystem.OnDragStarted += HandleDragStarted;
                _dragDropSystem.OnDragEnded += HandleDragEnded;
            }
        }
    }

    private void OnDestroy()
    {
        if(_dragDropSystem != null)
        {
            _dragDropSystem.OnDragStarted -= HandleDragStarted;
            _dragDropSystem.OnDragEnded -= HandleDragEnded;
        }
    }

    private void SubscribeToDragEvents() 
    {
        if (AppCore.Instance != null && AppCore.Instance.InputManager != null)
        {
            AppCore.Instance.InputManager.OnDragStarted += HandleDragStarted;
            AppCore.Instance.InputManager.OnDragEnded += HandleDragEnded;
        }
    }
}


