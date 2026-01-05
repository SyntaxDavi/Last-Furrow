using UnityEngine;
using System.Collections.Generic; 

public class GridStateFeedback : MonoBehaviour
{
    private List<SpriteRenderer> _gridRenderers = new List<SpriteRenderer>();

    [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.GameState.OnStateChanged += HandleStateChanged;
            // Força atualização inicial
            HandleStateChanged(AppCore.Instance.GameStateManager.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.GameState.OnStateChanged -= HandleStateChanged;
    }

    // --- NOVO MÉTODO: O GridManager chama isso ao nascer ---
    public void UpdateRenderers(List<SpriteRenderer> renderers)
    {
        _gridRenderers = renderers;

        // Aplica o estado atual imediatamente nos novos renderers
        HandleStateChanged(AppCore.Instance.GameStateManager.CurrentState);
    }

    private void HandleStateChanged(GameState newState)
    {
        bool isPlaying = (newState == GameState.Playing);

        // Lógica Sprites (Agora usa a lista dinâmica)
        if (_gridRenderers != null && _gridRenderers.Count > 0)
        {
            Color targetColor = isPlaying ? Color.white : _disabledColor;
            foreach (var sprite in _gridRenderers)
            {
                if (sprite != null) sprite.color = targetColor;
            }
        }
    }
}