using UnityEngine;
using System;

/// <summary>
/// Implementação do serviço de vida.
/// Resolve regras de negócio relacionadas a cura e dano.
/// </summary>
public class HealthService : IHealthService
{
    private readonly ISaveManager _saveManager;

    public int CurrentLives => _saveManager.Data.CurrentRun?.CurrentLives ?? 0;
    public int MaxLives => _saveManager.Data.CurrentRun?.MaxLives ?? 0;
    public bool IsAtFullHealth => CurrentLives >= MaxLives;

    public event Action<int, int> OnHealthChanged;

    public HealthService(ISaveManager saveManager)
    {
        _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
    }

    public void Heal(int amount)
    {
        var run = _saveManager.Data.CurrentRun;
        if (run == null || amount <= 0) return;

        if (IsAtFullHealth) return;

        int oldLives = run.CurrentLives;
        run.CurrentLives = Mathf.Min(run.CurrentLives + amount, run.MaxLives);

        if (oldLives != run.CurrentLives)
        {
            Debug.Log($"[HealthService] Curado: {oldLives} -> {run.CurrentLives}");
            OnHealthChanged?.Invoke(run.CurrentLives, run.MaxLives);
        }
    }

    public void TakeDamage(int amount)
    {
        var run = _saveManager.Data.CurrentRun;
        if (run == null || amount <= 0) return;

        int oldLives = run.CurrentLives;
        run.CurrentLives = Mathf.Max(run.CurrentLives - amount, 0);

        if (oldLives != run.CurrentLives)
        {
            Debug.Log($"[HealthService] Dano: {oldLives} -> {run.CurrentLives}");
            OnHealthChanged?.Invoke(run.CurrentLives, run.MaxLives);
            
            if (run.CurrentLives <= 0)
            {
                Debug.LogWarning("[HealthService] Vida chegou a zero! (Game Over a ser tratado pelo Flow)");
            }
        }
    }
}
