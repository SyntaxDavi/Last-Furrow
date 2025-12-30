using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }

    [Header("Configurações do Grid")]
    public float gridWidth = 5f;
    public float gridHeight = 7f;

    private Camera _cam;

    private void Awake()
    {
        // 1. Singleton Pattern: Garante que só existe UMA câmera sistema no jogo todo
        if (Instance != null && Instance != this)
        {
            // Se já existe uma câmera ativa (vinda da cena anterior), destrói esta nova duplicata
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 2. Persistência: Impede que o Unity destrua este objeto ao trocar de cena
        DontDestroyOnLoad(gameObject);

        // 3. Setup Inicial
        _cam = GetComponent<Camera>();

        transform.position = new Vector3(0, 0, -10);

        AdjustCamera();
    }

    public void Initialize()
    {
        AdjustCamera();
    }

    public void AdjustCamera()
    {
        if (_cam == null) _cam = GetComponent<Camera>();

        float targetRatio = gridWidth / gridHeight;
        float screenRatio = (float)Screen.width / (float)Screen.height;

        if (screenRatio >= targetRatio)
        {
            _cam.orthographicSize = gridHeight / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            _cam.orthographicSize = (gridHeight / 2) * differenceInSize;
        }

        Debug.Log($"[CameraSystem] Ajustado. Ratio Tela: {screenRatio:F2}, Size: {_cam.orthographicSize}");
    }
}