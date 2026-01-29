using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configurações gerais do projeto (Gameplay e Engine).
/// Centraliza valores técnicos e regras de negócio base.
/// </summary>
public static class GameSettings
{
    [Header("Engine & Visuals")]
    /// <summary>
    /// Pixels Per Unit (PPU) oficial do projeto.
    /// </summary>
    public const int TARGET_PPU = 24;

    /// <summary>
    /// Indica se o projeto deve tentar forçar alinhamento perfeito de pixels.
    /// </summary>
    public const bool USE_PIXEL_PERFECT = false;

    [Header("Gameplay - Balanceamento Base")]
    public const int INITIAL_MAX_LIVES = 3;
    public const int INITIAL_WEEKLY_GOAL = 150;
    public const int DEFAULT_MAX_HAND_SIZE = 10;
    public const int DEFAULT_CARDS_DRAW_PER_DAY = 3;

    [Header("Gameplay - Deck Inicial")]
    /// <summary>
    /// IDs das cartas que compõem o deck inicial em uma nova run.
    /// </summary>
    public static readonly string[] STARTING_DECK_IDS = new string[] 
    { 
        "card_carrot", 
        "card_corn", 
        "card_harvest", 
        "card_shovel", 
        "card_water" 
    };

    [Header("Debug & Determinismo")]
    /// <summary>
    /// Se diferente de 0, força o MasterSeed para toda nova run.
    /// Útil para testes de consistência.
    /// </summary>
    public const int MASTER_SEED_OVERRIDE = 0;

    /// <summary>
    /// Se diferente de 0, força o UnlockPatternSeed para toda nova run.
    /// </summary>
    public const int UNLOCK_PATTERN_SEED_OVERRIDE = 0;
}
