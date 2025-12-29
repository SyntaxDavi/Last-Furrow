using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask _interactionLayer;

    private Camera _cam;
    private InputManager _input;

    // Referências de Hover
    private IInteractable _currentHover;
    private Object _currentHoverObject; 

    private void Start()
    {
        _cam = Camera.main;

        if (AppCore.Instance != null)
        {
            _input = AppCore.Instance.InputManager;
            if (_input != null)
            {
                _input.OnPrimaryClick += HandleClick;
            }
        }
    }

    private void OnDestroy()
    {
        if (_input != null) _input.OnPrimaryClick -= HandleClick;
    }

    private void Update()
    {
        // Se tínhamos um hover, mas o objeto foi destruído, limpa tudo antes de prosseguir.
        if (_currentHover != null && _currentHoverObject == null)
        {
            _currentHover = null;
            _currentHoverObject = null;
        }

        HandleHover();
    }

    // Método auxiliar seguro para pegar mouse (Resolve ERRO 4)
    private Vector2 GetSafeMousePosition()
    {
        if (_input != null)
            return _input.MouseWorldPosition;

        if (_cam != null)
            return _cam.ScreenToWorldPoint(Input.mousePosition);

        return Vector2.zero;
    }

    private void HandleHover()
    {
        if (_cam == null) return;

        Vector2 mousePos = GetSafeMousePosition();
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, _interactionLayer);

        IInteractable newHover = null;

        if (hit.collider != null)
        {
            newHover = hit.collider.GetComponent<IInteractable>();
        }

        if (newHover != _currentHover)
        {
            // Sai do antigo (se ainda existir)
            if (_currentHover != null && _currentHoverObject != null)
            {
                _currentHover.OnHoverExit();
            }

            // Entra no novo
            _currentHover = newHover;
            _currentHoverObject = newHover as Object; // Cast seguro para Unity Object

            if (_currentHover != null)
            {
                _currentHover.OnHoverEnter();
            }
        }
    }

    private void HandleClick()
    {
        Vector2 mousePos = GetSafeMousePosition();
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, _interactionLayer);

        if (hit.collider != null)
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null && (interactable as UnityEngine.Object) != null)
            {
                // Ação local
                interactable.OnClick();
                // Útil para UI de tutorial ("Clique num slot") ou sons globais
                AppCore.Instance.Events.TriggerInteraction(interactable);
            }
        }
    }
}