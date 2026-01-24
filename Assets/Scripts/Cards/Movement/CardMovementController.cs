using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class CardMovementController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SortingGroup _sortingGroup;

    // Estado Interno de F�sica
    private Vector3 _currentVelocityPos;
    private Vector3 _currentVelocityScale; // Agora � Vector3 para suportar deforma��o
    private float _currentVelocityRot;     // Agora � float (velocidade angular Z)

    // --- API P�BLICA ---

    public void SnapTo(CardVisualTarget target)
    {
        // Prote��o contra escala zero
        if (target.Scale.x == 0) target.Scale = Vector3.one * 0.01f;

        transform.position = target.Position;
        transform.rotation = target.Rotation;
        transform.localScale = target.Scale;

        ResetPhysicsState();
    }

    // Agora aceita deltaTime explicitamente para f�sica determin�stica
    public void MoveTo(CardVisualTarget target, CardMovementProfile profile, float deltaTime)
    {
        if (deltaTime <= 0.0001f) return;

        // 1. Posi��o (SmoothDamp)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            target.Position,
            ref _currentVelocityPos,
            profile.PositionSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        // 2. Rota��o (SmoothDampAngle)
        // Substitui o Slerp antigo por algo que tem in�rcia real
        float currentZ = transform.eulerAngles.z;
        float targetZ = target.Rotation.eulerAngles.z;

        float newZ = Mathf.SmoothDampAngle(
            currentZ,
            targetZ,
            ref _currentVelocityRot,
            profile.RotationSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        transform.rotation = Quaternion.Euler(0, 0, newZ);

        // 3. Escala + Stretch (Juice)
        Vector3 finalTargetScale = target.Scale;

        // Efeito de Esticar baseado na velocidade atual
        if (profile.MovementStretchAmount > 0)
        {
            float speed = _currentVelocityPos.magnitude;
            // Estica levemente (Clamp para n�o exagerar)
            float stretchFactor = Mathf.Clamp(speed * profile.MovementStretchAmount, 0f, 0.2f);

            // Soma � escala original (Efeito Gelatina)
            finalTargetScale += Vector3.one * stretchFactor;
        }

        // SmoothDamp na Escala
        transform.localScale = Vector3.SmoothDamp(
            transform.localScale,
            finalTargetScale,
            ref _currentVelocityScale,
            profile.ScaleSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        ValidateTransform();
    }

    public void SetSortingOrder(int order)
    {
        if (_sortingGroup.sortingOrder != order)
            _sortingGroup.sortingOrder = order;
    }

    public void ResetPhysicsState()
    {
        _currentVelocityPos = Vector3.zero;
        _currentVelocityScale = Vector3.zero;
        _currentVelocityRot = 0f;
    }

    // Seguran�a contra NaN (Erros de matem�tica que fazem o objeto sumir)
    private void ValidateTransform()
    {
        if (float.IsNaN(transform.position.x)) transform.position = Vector3.zero;
        if (float.IsNaN(transform.localScale.x)) transform.localScale = Vector3.one;
    }
}