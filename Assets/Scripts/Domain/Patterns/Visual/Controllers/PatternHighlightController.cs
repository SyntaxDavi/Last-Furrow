using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks; // <--- Importante
using System.Threading; // Para CancellationToken

/// <summary>
/// Escuta eventos de padrões e aplica highlights visuais nos slots.
/// Versão: UniTask (Async/Await)
/// </summary>
public class PatternHighlightController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;

    [Header("References")]
    [SerializeField] private GridManager _gridManager;

    private Dictionary<int, GridSlotView> _slotCache = new Dictionary<int, GridSlotView>();

    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
    }

    private async UniTaskVoid Start()
    {
        // Garante que o AppCore e os eventos de Pattern estejam prontos (Onda 6 - Segurança)
        await UniTask.WaitUntil(() => AppCore.Instance?.Events?.Pattern != null);

        CacheSlots();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        // UniTask cuida do cancelamento automaticamente via GetCancellationTokenOnDestroy
    }

    private void CacheSlots()
    {
        if (_gridManager == null) return;

        _slotCache.Clear();
        var slots = _gridManager.GetComponentsInChildren<GridSlotView>();

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                _slotCache[slot.SlotIndex] = slot;
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted += OnPatternSlotCompleted;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted -= OnPatternSlotCompleted;
        }
    }

    private void OnPatternSlotCompleted(PatternMatch match)
    {
        if (match == null || _config == null || match.SlotIndices == null) return;

        // Fallback: Se o cache estiver vazio (ex: grid gerado após o Start), tenta preencher agora
        if (_slotCache.Count == 0) CacheSlots();

        int tier = CalculateTier(match.BaseScore);
        Color tierColor = _config.GetTierColor(tier);
        Color finalColor = _config.ApplyDecayToColor(tierColor, match.DaysActive);

        foreach (int slotIndex in match.SlotIndices)
        {
            if (_slotCache.TryGetValue(slotIndex, out GridSlotView slot))
            {
                // Inicia a animação sem esperar (Fire-and-Forget)
                HighlightSlotAsync(slot, finalColor).Forget();
            }
        }
    }

    private async UniTaskVoid HighlightSlotAsync(GridSlotView slot, Color color)
    {
        // Pega o token de cancelamento deste MonoBehaviour.
        // Se este objeto for destruído, a task cancela automaticamente.
        var token = this.GetCancellationTokenOnDestroy();

        if (slot == null) return;

        float elapsed = 0f;
        float duration = _config.highlightDuration;

        // FASE 1: Loop de Animação (PingPong)
        while (elapsed < duration)
        {
            // Verifica se o slot ainda existe (segurança extra)
            if (slot == null) return;

            elapsed += Time.deltaTime;

            float t = Mathf.PingPong(elapsed * _config.highlightPulseSpeed, 1f);
            Color pulsedColor = color;
            pulsedColor.a = Mathf.Lerp(0.3f, 0.8f, t);

            slot.SetPatternHighlight(pulsedColor, true);
            slot.SetElevationFactor(1f);

            // Espera 1 frame, mas cancela se o objeto morrer
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // FASE 2: Fade out
        float fadeElapsed = 0f;
        float fadeDuration = _config.highlightFadeOutDuration;

        while (fadeElapsed < fadeDuration)
        {
            if (slot == null) return;

            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration;
            slot.SetElevationFactor(1f - t);

            Color fadedColor = color;
            fadedColor.a = Mathf.Lerp(0.8f, 0f, t);

            slot.SetPatternHighlight(fadedColor, true);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // Limpeza final
        if (slot != null)
        {
            slot.ClearPatternHighlight();
            slot.SetElevationFactor(0f);
        }
    }

    private int CalculateTier(int baseScore)
    {
        if (baseScore >= 80) return 4;
        if (baseScore >= 35) return 3;
        if (baseScore >= 15) return 2;
        return 1;
    }
}
