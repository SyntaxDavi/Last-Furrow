using Cysharp.Threading.Tasks;
using System; // Necessário para TimeSpan
using UnityEngine;

public class ScreenFadeStep : IFlowStep
{
    private readonly bool _fadeIn; // true = clarear (mostra o jogo), false = escurecer (preto)
    private readonly float _duration;

    public ScreenFadeStep(bool fadeIn, float duration = 0.5f)
    {
        _fadeIn = fadeIn;
        _duration = duration;
    }

    public async UniTask Execute(FlowControl control)
    {
        // Se você tiver um sistema de Fade no AppCore, chame aqui com await.
        // await AppCore.Instance.ScreenFader.FadeAsync(_fadeIn, _duration);

        // POR ENQUANTO (Simulação):
        Debug.Log($"[Step] Iniciando Fade {(_fadeIn ? "In" : "Out")} ({_duration}s)...");

        // Converte float (segundos) para TimeSpan
        await UniTask.Delay(TimeSpan.FromSeconds(_duration));

        Debug.Log($"[Step] Fade {(_fadeIn ? "In" : "Out")} Completo.");
    }
}