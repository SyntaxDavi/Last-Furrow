using UnityEngine;
using TMPro;
using System.Collections;

public class FeedbackText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _cg;

    [Header("Configuração Animação")]
    [SerializeField] private float _floatDistance = 50f;
    [SerializeField] private float _duration = 1.5f;

    private void Awake()
    {
        if (_cg != null)
        {
            _cg.alpha = 0f;
            _cg.blocksRaycasts = false; 
        }
    }

    public void ShowFeedback(string message, Color color)
    {
        if (_text == null || _cg == null) return;

        _text.text = message;
        _text.color = color;

        StopAllCoroutines();
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        // 1. Setup Inicial da Animação
        _cg.alpha = 1;
        transform.localPosition = Vector3.zero;

        float time = 0;

        while (time < _duration)
        {
            time += Time.deltaTime;
            float t = time / _duration;

            // Sobe suavemente
            transform.localPosition = Vector3.up * (_floatDistance * t);

            // Fade out nos últimos 30% do tempo
            if (t > 0.7f)
            {
                // Mapeia t (0.7 a 1.0) para alpha (1.0 a 0.0)
                float fadeProgress = (t - 0.7f) / 0.3f;
                _cg.alpha = 1 - fadeProgress;
            }

            yield return null;
        }

        // 2. Finalização
        _cg.alpha = 0;
        transform.localPosition = Vector3.zero;
    }
}