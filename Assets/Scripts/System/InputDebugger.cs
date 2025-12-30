using UnityEngine;

public class InputDebugger : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        // Se o jogo estiver rodando, desenha a bolinha
        if (Application.isPlaying)
        {
            // Pega a posição que seu InputManager está calculando
            Vector2 mousePos = AppCore.Instance.InputManager.MouseWorldPosition;

            // Desenha uma esfera vermelha onde o clique está "caindo"
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(mousePos, 0.5f);

            // Desenha uma linha amarela da câmera até o mouse
            if (Camera.main != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(Camera.main.transform.position, mousePos);
            }
        }
    }
}