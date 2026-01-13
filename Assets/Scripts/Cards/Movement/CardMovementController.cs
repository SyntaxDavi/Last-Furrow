using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class CardMovementController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SortingGroup _sortingGroup;

    // Dependências (Injetadas pelo CardView)
    private CardVisualConfig _config;

    // Variáveis de Controle Físico
    private Vector3 _currentVelocityPos;
    private float _currentVelocityScale;
    private float _randomSeed; // Para o efeito de flutuação individual

    public void Initialize(CardVisualConfig config)
    {
        _config = config;
        _randomSeed = Random.Range(0f, 225f);
    }

    // --- MÉTODOS PÚBLICOS CHAMADOS PELO CARDVIEW ---

    public void HandleIdle(HandLayoutCalculator.CardTransformTarget layoutTarget, bool isHovered)
    {
        Vector3 targetPos = layoutTarget.Position;
        Quaternion targetRot = layoutTarget.Rotation;
        Vector3 targetScale = Vector3.one;
        int targetSort = layoutTarget.SortingOrder;

        // 1. Aplica Flutuação (Balatro)
        ApplyFloatEffect(ref targetPos, ref targetRot);

        // 2. Modificadores de Hover (Peek)
        if (isHovered)
        {
            targetPos += Vector3.up * _config.PeekYOffset;
            targetPos.z = _config.HoverZ;
            targetScale = Vector3.one * _config.PeekScale;
            targetSort = CardSortingConstants.HOVER_LAYER;
        }
        else
        {
            targetPos.z = _config.IdleZ;
        }

        ApplySmoothing(targetPos, targetRot, targetScale, targetSort, _config.PositionSmoothTime);
    }

    public void HandleSelected(HandLayoutCalculator.CardTransformTarget layoutTarget, Vector3 focusPoint)
    {
        // Posição Base (Ancorada no slot, mas subida)
        Vector3 anchorPos = layoutTarget.Position + (Vector3.up * _config.SelectedYOffset);

        // Vetor Direção
        Vector3 dirToFocus = focusPoint - anchorPos;
        dirToFocus = Vector3.ClampMagnitude(dirToFocus, _config.MaxInteractionDistance);

        // Efeito Magnético (Olhar e Puxar)
        float lookZ = -dirToFocus.x * _config.LookRotationStrength;
        Vector3 magneticOffset = dirToFocus * _config.MagneticPullStrength;

        // Definição dos Alvos
        Vector3 targetPos = anchorPos + magneticOffset;
        targetPos.z = _config.SelectedZ;

        // Rotação foca no cursor, ignorando o arco da mão
        Quaternion targetRot = Quaternion.Euler(0, 0, lookZ);
        Vector3 targetScale = Vector3.one * _config.SelectedScale;
        int targetSort = CardSortingConstants.HOVER_LAYER;

        ApplySmoothing(targetPos, targetRot, targetScale, targetSort, _config.PositionSmoothTime);
    }

    public void HandleDrag(Vector2 worldMousePos)
    {
        // Drag é físico e direto, bypass no sistema de SmoothDamp para responsividade
        _sortingGroup.sortingOrder = CardSortingConstants.DRAG_LAYER;

        // Zera velocidades para não ter "inércia" estranha ao soltar
        _currentVelocityPos = Vector3.zero;

        Vector3 targetPos = new Vector3(worldMousePos.x, worldMousePos.y, _config.DragZ);

        // Movimento rápido mas não instantâneo (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f);

        // Tilt Calculation (Inclinação baseada na velocidade horizontal)
        float deltaX = (targetPos.x - transform.position.x) * -2f;
        float tiltZ = Mathf.Clamp(deltaX * _config.DragTiltAmount, -_config.DragTiltAmount, _config.DragTiltAmount);

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, tiltZ), Time.deltaTime * _config.DragTiltSpeed);
    }

    // --- CÁLCULOS INTERNOS ---

    private void ApplyFloatEffect(ref Vector3 pos, ref Quaternion rot)
    {
        float time = Time.time + _randomSeed;
        float floatY = Mathf.Sin(time * _config.IdleFloatSpeed) * _config.IdleFloatAmount;
        float floatRot = Mathf.Cos(time * (_config.IdleFloatSpeed * 0.5f)) * _config.IdleRotationAmount;

        pos.y += floatY;
        rot *= Quaternion.Euler(0, 0, floatRot);
    }

    private void ApplySmoothing(Vector3 tPos, Quaternion tRot, Vector3 tScale, int tSort, float smoothTime)
    {
        if (Time.deltaTime <= 0.0001f) return;

        // Posição
        transform.position = Vector3.SmoothDamp(transform.position, tPos, ref _currentVelocityPos, smoothTime);

        // Escala
        float newScale = Mathf.SmoothDamp(transform.localScale.x, tScale.x, ref _currentVelocityScale, _config.ScaleSmoothTime);
        if (!float.IsNaN(newScale))
        {
            transform.localScale = Vector3.one * newScale;
        }

        // Rotação
        transform.rotation = Quaternion.Slerp(transform.rotation, tRot, Time.deltaTime * _config.RotationSpeed);

        // Layer de Renderização
        if (_sortingGroup.sortingOrder != tSort)
            _sortingGroup.sortingOrder = tSort;
    }
}