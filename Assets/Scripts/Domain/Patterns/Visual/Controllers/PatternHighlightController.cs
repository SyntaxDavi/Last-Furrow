using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks; 
using System.Threading; 

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
    
    // Rastreia efeitos ativos por slot para evitar conflito (Jitter Fix)
    private Dictionary<int, (CancellationTokenSource cts, int effectId)> _activeEffects = new Dictionary<int, (CancellationTokenSource, int)>();
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
        CancelAllEffects();
    }
    
    private void CancelAllEffects()
    {
        foreach (var effect in _activeEffects.Values)
        {
            effect.cts.Cancel();
            effect.cts.Dispose();
        }
        _activeEffects.Clear();
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
                // 1. Cancela efeito anterior no mesmo slot (Jitter Fix)
                if (_activeEffects.TryGetValue(slotIndex, out var existing))
                {
                    existing.cts.Cancel();
                    existing.cts.Dispose();
                    _activeEffects.Remove(slotIndex);
                }

                // 2. Gera ID único para este efeito
                int effectId = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

                // 3. Cria novo token controlado por nós
                var cts = new CancellationTokenSource();
                _activeEffects[slotIndex] = (cts, effectId);

                Debug.LogWarning($"START slot {slotIndex} | EffectID {effectId}");

                // 4. Linka com destruição do objeto para segurança
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cts.Token, this.GetCancellationTokenOnDestroy());

                // 5. Inicia animação
                HighlightSlotAsync(slot, finalColor, slotIndex, effectId, linkedCts.Token).Forget();
            }
        }
    }

    private async UniTaskVoid HighlightSlotAsync(GridSlotView slot, Color color, int slotIndex, int effectId, CancellationToken token)
    {
        if (slot == null) return;

        bool shouldCleanup = true;

        try
        {
            float elapsed = 0f;
            float duration = _config.highlightDuration;

            // FASE 1: Loop de Animação (PingPong)
            while (elapsed < duration)
            {
                if (slot == null) return;

                elapsed += Time.deltaTime;

                float t = Mathf.PingPong(elapsed * _config.highlightPulseSpeed, 1f);
                Color pulsedColor = color;
                pulsedColor.a = Mathf.Lerp(0.3f, 0.8f, t);

                slot.SetPatternHighlight(pulsedColor, true);
                slot.SetElevationFactor(1f);

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
        }
        catch (System.OperationCanceledException)
        {
            // Esperado quando o token é cancelado - não propaga o erro
        }
        finally
        {
            Debug.LogWarning($"FINALLY slot {slotIndex} | EffectID {effectId}");

            // CONTROLE DE AUTORIDADE: Verifica se este efeito ainda é o ativo
            if (_activeEffects.TryGetValue(slotIndex, out var current) && current.effectId != effectId)
            {
                Debug.LogWarning($"SKIPPED cleanup slot {slotIndex} | EffectID {effectId} (current is {current.effectId})");
                shouldCleanup = false;
            }

            // GARANTIA: Apenas limpa se este efeito ainda for o dono do slot
            if (shouldCleanup)
            {
                if (slot != null)
                {
                    slot.ClearPatternHighlight(); // Isso já chama SetElevationFactor(0f)
                }
                
                // Remove do dicionário apenas se ainda for o efeito atual
                if (_activeEffects.TryGetValue(slotIndex, out var final) && final.effectId == effectId)
                {
                    _activeEffects.Remove(slotIndex);
                }
            }
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
