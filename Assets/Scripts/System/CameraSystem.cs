using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSystem : MonoBehaviour
{
    [Header("Configurações do Grid")]
    public float gridWidth = 5f;  
    public float gridHeight = 7f; 

    private Camera _cam;

    public void Initialize()
    {
        _cam = GetComponent<Camera>();
        AdjustCamera();
    }

    // Chama isso no Update ou quando mudar resolução
    public void AdjustCamera()
    {
        float targetRatio = gridWidth / gridHeight;
        float screenRatio = (float)Screen.width / (float)Screen.height;

        if (screenRatio >= targetRatio)
        {
            // Tela mais larga que o grid (sobra espaço nos lados)
            _cam.orthographicSize = gridHeight / 2;
        }
        else
        {
            // Tela mais alta/estreita que o grid (sobra espaço em cima/baixo)
            float differenceInSize = targetRatio / screenRatio;
            _cam.orthographicSize = (gridHeight / 2) * differenceInSize;
        }
    }
}