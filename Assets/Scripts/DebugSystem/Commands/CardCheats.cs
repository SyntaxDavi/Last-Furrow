using UnityEngine;
using UnityEngine.SceneManagement;

[Cheat("draw_cards", "Cards", "Compra 3 cartas.")]
public class DrawCardsCheat : ICheatCommand
{
    public string Id => "draw_cards";
    public string Category => "Cards";
    public string Description => "Compra 3 cartas.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        ctx.HandSystem?.ProcessDailyDraw(run);
        feedback = "✅ 3 cartas compradas.";
        return true;
    }
}

[Cheat("clear_hand", "Cards", "Limpa toda a mão do jogador.")]
public class ClearHandCheat : ICheatCommand
{
    public string Id => "clear_hand";
    public string Category => "Cards";
    public string Description => "Limpa toda a mão do jogador.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        run.Hand.Clear();
        ctx.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = "✅ Mão limpa. Recarregando...";
        return true;
    }
}

[Cheat("spawn_card", "Cards", "Adiciona uma carta específica à mão. Uso: spawn_card [card_id]")]
public class SpawnCardCheat : ICheatCommand
{
    public string Id => "spawn_card";
    public string Category => "Cards";
    public string Description => "Adiciona uma carta específica à mão. Uso: spawn_card [card_id]";

    public bool ValidateArgs(string[] args, out string error)
    {
        if (args.Length == 0) { error = "ID da carta é obrigatório."; return false; }
        error = null;
        return true;
    }

    public bool Execute(string[] args, out string feedback)
    {
        string cardId = args[0];
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        if (run.Hand.Count >= run.MaxHandSize) { feedback = "❌ Mão cheia."; return false; }

        if (!ctx.Library.TryGetCard(new CardID(cardId), out var cardData))
        {
            feedback = $"❌ Carta não encontrada: {cardId}";
            return false;
        }

        var instance = new CardInstance(new CardID(cardId));
        run.Hand.Add(instance);
        ctx.Events.Player.TriggerCardAdded(instance);
        feedback = $"✅ Carta adicionada: {cardId}";
        return true;
    }
}
