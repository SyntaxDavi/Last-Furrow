using System;
using System.Collections.Generic;

/// <summary>
/// Implementação padrão determinística baseada em System.Random.
/// Cada instância é isolada e seu estado depende apenas da seed inicial.
/// </summary>
public class SeededRandomProvider : IRandomProvider
{
    private readonly System.Random _rng;
    public int Seed { get; private set; }

    public SeededRandomProvider(int seed)
    {
        Seed = seed;
        _rng = new System.Random(seed);
    }

    public int Range(int min, int max)
    {
        if (min >= max) return min; // Segurança contra argumentos inválidos
        return _rng.Next(min, max);
    }

    public float Value()
    {
        return (float)_rng.NextDouble();
    }

    public bool NextBool(float chance = 0.5f)
    {
        return Value() < chance;
    }

    public T Select<T>(IList<T> list)
    {
        if (list == null || list.Count == 0)
            return default;
            
        int index = _rng.Next(0, list.Count);
        return list[index];
    }

    public void Shuffle<T>(IList<T> list)
    {
        if (list == null || list.Count <= 1) return;

        // Fisher-Yates Shuffle
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
