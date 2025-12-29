using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private float _previousTimeScale = 1f;
    private bool _isPaused = false;

    // Propriedade pública para leitura segura
    public bool IsPaused => _isPaused;

    public void Initialize()
    {
        _isPaused = false;
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        if (_isPaused) return; 

        // 1. Salva a velocidade atual antes de pausar
        _previousTimeScale = Time.timeScale;

        // 2. Trava tudo
        Time.timeScale = 0f;
        _isPaused = true;

        Debug.Log("[TimeManager] Jogo Pausado.");
    }

    public void ResumeGame()
    {
        if (!_isPaused) return; // Não estava pausado, ignora

        // 3. Restaura a velocidade que estava antes (blindagem contra perda de estado)
        Time.timeScale = _previousTimeScale;
        _isPaused = false;

        Debug.Log("[TimeManager] Jogo Resumido.");
    }

    // Método utilitário caso queira mudar a velocidade base no futuro (sem quebrar o pause)
    public void SetBaseGameSpeed(float speed)
    {
        if (_isPaused)
        {
            // Se estiver pausado, atualizamos apenas a variável de backup
            _previousTimeScale = speed;
        }
        else
        {
            // Se rodando, aplica direto
            Time.timeScale = speed;
        }
    }
}