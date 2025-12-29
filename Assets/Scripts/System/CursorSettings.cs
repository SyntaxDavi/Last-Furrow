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

    // Retorna a posição do mouse no Mundo 2D (Para Gameplay)
    public Vector2 GetWorldPosition(Camera cam, Vector3 mouseScreenPos)
    {
        if (cam == null) return Vector2.zero;
        return cam.ScreenToWorldPoint(mouseScreenPos);
    }

    // Retorna o Offset para a Câmera (Para Visual/Juice)
    // Baseado no seu script de exemplo
    public Vector3 CalculateCameraSway(Vector3 mouseScreenPos)
    {
        // 1. Normaliza (0 a 1)
        float xNorm = mouseScreenPos.x / Screen.width;
        float yNorm = mouseScreenPos.y / Screen.height;

        // 2. Centraliza (-0.5 a 0.5)
        float x = xNorm - 0.5f;
        float y = yNorm - 0.5f;

        // 3. Distância do centro
        float distFromCenter = Mathf.Sqrt(x * x + y * y);

        // 4. Deadzone
        if (distFromCenter < _settings.deadZone)
            return Vector3.zero;

        // 5. Cálculo da força
        Vector2 direction = new Vector2(x, y).normalized;
        float adjustedStrength = (distFromCenter - _settings.deadZone) * 2f;

        return (Vector3)direction * adjustedStrength * _settings.swayStrength;
    }
}