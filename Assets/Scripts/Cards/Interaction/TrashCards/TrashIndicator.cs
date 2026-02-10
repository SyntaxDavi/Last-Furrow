using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Indicador visual "TRASH" que aparece quando o jogador começa a arrastar uma carta.
/// </summary>
public class TrashIndicator : MonoBehaviour, IInteractable
{
    [Header("Visual")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _trashIcon;
    
    [Header("Configuração")]
    [SerializeField] private float _fadeDuration = 0.3f;
    [SerializeField] private float _hideDelay = 2f;

    [Header("Referências")]
    [SerializeField] private DiscartCardsLogic _trashLogic;

    private Coroutine _hideCoroutine;
    private bool _isVisible = false;
    private DragDropSystem _dragDropSystem;

    public int InteractionPriority => 500;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        // Acessa o DragDropSystem através do PlayerInteraction
        var playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        
        if (playerInteraction != null)
        {
            _dragDropSystem = playerInteraction.DragSystem;
            
            if (_dragDropSystem != null)
            {
                _dragDropSystem.OnDragStarted += HandleDragStarted;
                _dragDropSystem.OnDragEnded += HandleDragEnded;
                Debug.Log("[TrashIndicator] Inscrito nos eventos de drag");
            }
            else
            {
                Debug.LogError("[TrashIndicator] DragSystem não encontrado no PlayerInteraction!");
            }
        }
        else
        {
            Debug.LogError("[TrashIndicator] PlayerInteraction não encontrado na cena!");
        }
    }

    private void OnDestroy()
    {
        if (_dragDropSystem != null)
        {
            _dragDropSystem.OnDragStarted -= HandleDragStarted;
            _dragDropSystem.OnDragEnded -= HandleDragEnded;
        }
    }

    // --- EVENTOS DE DRAG ---

    private void HandleDragStarted(IDraggable draggable)
    {
        if (draggable is CardView)
        {
            Debug.Log("[TrashIndicator] Drag iniciado - Mostrando indicador");
            Show();
        }
    }

    private void HandleDragEnded(IDraggable draggable)
    {
        if (draggable is CardView)
        {
            Debug.Log("[TrashIndicator] Drag finalizado - Iniciando timer");
            StartHideTimer();
        }
    }

    // --- CONTROLE DE VISIBILIDADE ---

    public void Show()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        if (!_isVisible)
        {
            _isVisible = true;
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f));
            
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
            }
        }
    }

    private void StartHideTimer()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }

        _hideCoroutine = StartCoroutine(HideDelayedRoutine());
    }

    private IEnumerator HideDelayedRoutine()
    {
        Debug.Log($"[TrashIndicator] Timer iniciado ({_hideDelay}s)");
        yield return new WaitForSeconds(_hideDelay);
        Debug.Log("[TrashIndicator] Timer expirado - Escondendo");
        Hide();
    }

    private void Hide()
    {
        if (_isVisible)
        {
            _isVisible = false;
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f));
            
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
            }

            if (_trashLogic != null)
            {
                _trashLogic.HideTrash();
            }
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (_canvasGroup == null) yield break;

        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeDuration;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }

    // --- INTERFACE IINTERACTABLE ---

    public void OnClick()
    {
        // Não faz nada
    }

    public void OnHoverEnter()
    {
        Debug.Log("[TrashIndicator] Hover ENTER - Mostrando lixeira");
        
        if (_trashLogic != null && _isVisible)
        {
            _trashLogic.ShowTrash();
        }
    }

    public void OnHoverExit()
    {
        Debug.Log("[TrashIndicator] Hover EXIT - Escondendo lixeira");
        
        if (_trashLogic != null)
        {
            _trashLogic.HideTrash();
        }
    }
}


