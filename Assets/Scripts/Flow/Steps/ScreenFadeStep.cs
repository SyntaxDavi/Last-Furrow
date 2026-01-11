using System.Collections;
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

    public IEnumerator Execute(FlowControl control)
    {
        // Se você tiver um sistema de Fade no AppCore, chame aqui.
        // yield return AppCore.Instance.ScreenFader.Fade(_fadeIn, _duration);

        // POR ENQUANTO (Simulação para não quebrar seu código):
        Debug.Log($"[Step] Iniciando Fade {(_fadeIn ? "In" : "Out")} ({_duration}s)...");

        yield return new WaitForSeconds(_duration);

        Debug.Log($"[Step] Fade {(_fadeIn ? "In" : "Out")} Completo.");
    }
}