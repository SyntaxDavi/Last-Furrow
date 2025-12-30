using UnityEngine;

[System.Serializable]
public class CursorSettings
{
    public float swayStrength = 2.0f; // O quanto a câmera "segue" o mouse
    public float deadZone = 0.1f;     // Zona morta no centro da tela
}

// Classe pura, testável, sem MonoBehaviour
public class CursorLogic
{
    private readonly CursorSettings _settings;

    public CursorLogic(CursorSettings settings)
    {
        _settings = settings;
    }

    // Sobrecarga Padrão (assume chão em Z=0)
    public Vector2 GetWorldPosition(Camera cam, Vector2 screenPos)
    {
        return GetWorldPosition(cam, screenPos, 0f);
    }

    // Sobrecarga Completa (permite definir a altura do plano de interação)
    public Vector2 GetWorldPosition(Camera cam, Vector2 screenPos, float targetZWorldPos)
    {
        if (cam == null) return Vector2.zero;

        // A distância Z que o ScreenToWorldPoint precisa é:
        // Distância da Câmera até o Plano Alvo.
        // Se Câmera está em -10 e Alvo em 0 -> Distância = 10
        // Se Câmera está em -10 e Alvo em -2 -> Distância = 8
        float distanceFromCamera = Mathf.Abs(cam.transform.position.z - targetZWorldPos);

        Vector3 screenPosWithDepth = new Vector3(screenPos.x, screenPos.y, distanceFromCamera);

        return cam.ScreenToWorldPoint(screenPosWithDepth);
    }

    public Vector3 CalculateCameraSway(Vector2 screenPos)
    {
        float xNorm = screenPos.x / Screen.width;
        float yNorm = screenPos.y / Screen.height;

        float x = xNorm - 0.5f;
        float y = yNorm - 0.5f;

        float distFromCenter = Mathf.Sqrt(x * x + y * y);

        if (distFromCenter < _settings.deadZone) return Vector3.zero;

        Vector2 direction = new Vector2(x, y).normalized;
        float strength = (distFromCenter - _settings.deadZone) * 2f;

        return (Vector3)direction * strength * _settings.swayStrength;
    }
}