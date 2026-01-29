using System.Collections.Generic;

/// <summary>
/// Contrato para provedores de aleatoriedade no jogo.
/// Garante que sistemas de gameplay não dependam de implementações concretas (System.Random, Unity.Random).
/// Fundamental para Determinismo, Replays e Testes Unitários.
/// </summary>
public interface IRandomProvider
{
    /// <summary>
    /// Retorna o próximo inteiro no intervalo [min, max).
    /// </summary>
    int Range(int min, int max);

    /// <summary>
    /// Retorna um float entre 0.0 (inclusivo) e 1.0 (exclusivo).
    /// </summary>
    float Value();

    /// <summary>
    /// Retorna true ou false com 50% de chance (ou baseado em chance especificada 0-1).
    /// </summary>
    bool NextBool(float chance = 0.5f);

    /// <summary>
    /// Seleciona um item aleatório de uma lista.
    /// </summary>
    T Select<T>(IList<T> list);

    /// <summary>
    /// Embaralha uma lista in-place (Fisher-Yates).
    /// </summary>
    void Shuffle<T>(IList<T> list);
    
    /// <summary>
    /// Retorna a semente original usada para criar este estado.
    /// </summary>
    int Seed { get; }
}
