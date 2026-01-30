using UnityEngine;

[ExecuteAlways]
public class ScreenAnchor : MonoBehaviour
{
    [Header("Alinhamento (Viewport)")]
    [Tooltip("0,0 = Canto Inf Esq | 0.5,0.5 = Centro | 1,1 = Canto Sup Dir")]
    [Range(0f, 1f)] public float AnchorX = 0.5f;
    [Range(0f, 1f)] public float AnchorY = 0.5f;

    [Header("Ajuste Fino (World Units)")]
    public Vector2 Offset = Vector2.zero;

    private Camera _cachedCam;
    private Vector2Int _lastResolution;
    private bool _isOrthographicWarningShown = false;

    private void OnEnable()
    {
        _cachedCam = Camera.main;

        // Cache inicial da resolução para evitar cálculo no primeiro frame
        _lastResolution = new Vector2Int(Screen.width, Screen.height);

        Align();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (Screen.width != _lastResolution.x || Screen.height != _lastResolution.y)
            {
                _lastResolution.x = Screen.width;
                _lastResolution.y = Screen.height;
                Align();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // S alinha se estiver ativo na hierarquia para evitar erros de serializao
        if (isActiveAndEnabled)
        {
            Align();
        }
    }
#endif

    public void Align()
    {
        if (_cachedCam == null)
        {
            _cachedCam = Camera.main;
            if (_cachedCam == null) return;
        }

        // Aviso de segurana (uma nica vez)
        if (!_cachedCam.orthographic && !_isOrthographicWarningShown)
        {
            Debug.LogWarning($"[ScreenAnchor] Cmera '{_cachedCam.name}'  Perspectiva. Alinhamento pode variar com Z.", gameObject);
            _isOrthographicWarningShown = true;
        }

        // Lgica de Posicionamento Preservando Z
        float currentZ = transform.position.z;
        float distFromCam = currentZ - _cachedCam.transform.position.z;

        Vector3 targetViewportPos = new Vector3(AnchorX, AnchorY, distFromCam);
        Vector3 newWorldPos = _cachedCam.ViewportToWorldPoint(targetViewportPos);

        // Garante que o Z não flutuou devido a imprecisão de float
        newWorldPos.z = currentZ;

        // Aplica a posição final
        transform.position = newWorldPos + (Vector3)Offset;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}