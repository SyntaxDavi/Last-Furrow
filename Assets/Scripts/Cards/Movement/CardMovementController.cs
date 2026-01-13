using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class CardMovementController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SortingGroup _sortingGroup;

    // Estado Interno de Física (Velocidades do SmoothDamp)
    private Vector3 _currentVelocityPos;
    private float _currentVelocityScale;

    // --- API PÚBLICA ---

    /// <summary>
    /// Teleporta imediatamente para o alvo e zera inércia.
    /// Use ao iniciar Drag ou spawnar.
    /// </summary>
    public void SnapTo(CardVisualTarget target)
    {
        transform.position = target.Position;
        transform.rotation = target.Rotation;
        transform.localScale = target.Scale;

        ResetPhysicsState();
    }

    /// <summary>
    /// Move suavemente em direção ao alvo usando o perfil fornecido.
    /// </summary>
    public void MoveTo(CardVisualTarget target, CardMovementProfile profile)
    {
        float dt = Time.deltaTime;
        if (dt <= 0.0001f) return;

        // 1. Posição (SmoothDamp)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            target.Position,
            ref _currentVelocityPos,
            profile.PositionSmoothTime
        );

        // 2. Escala (SmoothDamp)
        float newScale = Mathf.SmoothDamp(
            transform.localScale.x,
            target.Scale.x,
            ref _currentVelocityScale,
            profile.ScaleSmoothTime
        );

        // Proteção contra NaN (muito comum em escalas 0)
        if (!float.IsNaN(newScale))
        {
            transform.localScale = Vector3.one * newScale;
        }

        // 3. Rotação (Slerp - Melhor para rotações contínuas)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target.Rotation,
            dt * profile.RotationSpeed
        );
    }

    /// <summary>
    /// Define a ordem de renderização separadamente do movimento.
    /// </summary>
    public void SetSortingOrder(int order)
    {
        if (_sortingGroup.sortingOrder != order)
        {
            _sortingGroup.sortingOrder = order;
        }
    }

    /// <summary>
    /// Zera as velocidades internas para evitar "efeito estilingue" ao trocar de estados.
    /// </summary>
    public void ResetPhysicsState()
    {
        _currentVelocityPos = Vector3.zero;
        _currentVelocityScale = 0f;
    }
}