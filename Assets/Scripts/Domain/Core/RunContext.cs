using UnityEngine;

/// <summary>
/// Contexto de IDENTIDADE - Imutável durante a run.
/// 
/// Contém serviços que:
/// - Definem a identidade da run
/// - NÃO mudam durante a run
/// - NÃO dependem de cena
/// 
/// É IMUTÁVEL: criado uma vez, morre uma vez.
/// Grid NÃO está aqui - Grid é RunRuntimeContext.
/// </summary>
public readonly struct RunIdentityContext
{
    // --- SERVIÇOS CORE ---
    public readonly IRunManager RunManager;
    public readonly ISaveManager SaveManager;
    public readonly IEconomyService Economy;

    // --- SERVIÇOS DE DADOS ---
    public readonly IGameLibrary Library;

    // --- ALEATORIEDADE ---
    public readonly IRandomProvider Random;

    // --- EVENTOS ---
    public readonly PlayerEvents PlayerEvents;
    public readonly GameEvents GameEvents;

    public RunIdentityContext(
        IRunManager runManager,
        ISaveManager saveManager,
        IEconomyService economy,
        IGameLibrary library,
        IRandomProvider random,
        PlayerEvents playerEvents,
        GameEvents gameEvents)
    {
        // Validação robusta contra nulls
        if (runManager == null)
            throw new System.ArgumentNullException(nameof(runManager), "[RunIdentityContext] RunManager não pode ser null");
        if (saveManager == null)
            throw new System.ArgumentNullException(nameof(saveManager), "[RunIdentityContext] SaveManager não pode ser null");
        if (economy == null)
            throw new System.ArgumentNullException(nameof(economy), "[RunIdentityContext] EconomyService não pode ser null");
        if (library == null)
            throw new System.ArgumentNullException(nameof(library), "[RunIdentityContext] GameLibrary não pode ser null");
        if (random == null)
            throw new System.ArgumentNullException(nameof(random), "[RunIdentityContext] IRandomProvider não pode ser null.");
        if (playerEvents == null)
            throw new System.ArgumentNullException(nameof(playerEvents), "[RunIdentityContext] PlayerEvents não pode ser null");
        if (gameEvents == null)
            throw new System.ArgumentNullException(nameof(gameEvents), "[RunIdentityContext] GameEvents não pode ser null");

        RunManager = runManager;
        SaveManager = saveManager;
        Economy = economy;
        Library = library;
        Random = random;
        PlayerEvents = playerEvents;
        GameEvents = gameEvents;

        Debug.Log("[RunIdentityContext] Inicializado com sucesso. Todos os serviços validados.");
    }
}


