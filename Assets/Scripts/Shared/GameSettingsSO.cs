using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configurações gerais do jogo (Gameplay e Engine).
/// ScriptableObject que centraliza valores técnicos e regras de negócio base.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Last Furrow/Game Settings")]
public class GameSettingsSO : ScriptableObject
{
    [Header("Engine & Visuals")]
    [Tooltip("Pixels Per Unit (PPU) oficial do projeto")]
    public int TargetPPU = 24;
    
    [Tooltip("Força alinhamento perfeito de pixels")]
    public bool UsePixelPerfect = false;
    
    [Header("Gameplay - Hand & Draw")]
    [Tooltip("Tamanho máximo da mão do jogador")]
    public int DefaultMaxHandSize = 10;
    
    [Tooltip("Cartas sacadas por dia")]
    public int DefaultCardsDrawPerDay = 3;
    
    [Header("Gameplay - Lives & Goals")]
    [Tooltip("Vidas iniciais ao começar uma run")]
    public int InitialMaxLives = 3;
    
    [Tooltip("Meta semanal inicial (overridden por ProgressionSettingsSO se configurado)")]
    public int InitialWeeklyGoal = 150;
    
    [Header("Gameplay - Starting Deck")]
    [Tooltip("IDs das cartas que compõem a mão inicial em uma nova run")]
    public List<string> StartingDeckIDs = new() 
    { 
        "card_carrot", 
        "card_corn", 
        "card_harvest", 
        "card_shovel", 
        "card_water" 
    };
    
    [Header("RunDeck Configuration")]
    [Tooltip("Percentual do total de MaxCopies usado como target deck size")]
    [Range(0.5f, 1.0f)]
    public float RunDeckSizeMultiplier = 0.8f;
    
    [Header("Debug & Determinismo")]
    [Tooltip("Se diferente de 0, força o MasterSeed para toda nova run (útil para testes)")]
    public int MasterSeedOverride = 0;
    
    [Tooltip("Se diferente de 0, força o UnlockPatternSeed para toda nova run")]
    public int UnlockPatternSeedOverride = 0;
    
    [Header("Versioning")]
    [Tooltip("Incrementar ao fazer mudanças breaking. Usado para validar compatibilidade de saves.")]
    public int ConfigVersion = 1;
}
