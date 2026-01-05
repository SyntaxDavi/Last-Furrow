using UnityEngine;

public class GridStateFeedback : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Se usar UI (Canvas)")]
    [SerializeField] private CanvasGroup _gridCanvasGroup;

    [Tooltip("Se usar Sprites no mundo (2D)")]
    [SerializeField] private SpriteRenderer[] _gridRenderers;
    [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.GameState.OnStateChanged += HandleStateChanged;
            // Força atualização inicial caso o estado já tenha mudado antes do script ligar
            HandleStateChanged(AppCore.Instance.GameStateManager.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.GameState.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        bool isPlaying = (newState == GameState.Playing);

        // Lógica para UI (Canvas)
        if (_gridCanvasGroup != null)
        {
            _gridCanvasGroup.alpha = isPlaying ? 1f : 0.6f; // Fica meio transparente no shop
            _gridCanvasGroup.interactable = isPlaying;
            _gridCanvasGroup.blocksRaycasts = isPlaying;
        }

        // Lógica para World Space (Sprites)
        if (_gridRenderers != null && _gridRenderers.Length > 0)
        {
            Color targetColor = isPlaying ? Color.white : _disabledColor;
            foreach (var sprite in _gridRenderers)
            {
                if (sprite != null) sprite.color = targetColor;
            }
        }
    }
}