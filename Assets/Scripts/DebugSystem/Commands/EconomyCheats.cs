using UnityEngine;

[Cheat("add_money", "Economy", "Adiciona dinheiro ao jogador. Uso: add_money [quantidade]")]
public class AddMoneyCheat : ICheatCommand
{
    public string Id => "add_money";
    public string Category => "Economy";
    public string Description => "Adiciona dinheiro ao jogador. Uso: add_money [quantidade]";

    public bool ValidateArgs(string[] args, out string error)
    {
        if (args.Length == 0 || !int.TryParse(args[0], out _))
        {
            error = "Quantidade deve ser um número inteiro.";
            return false;
        }
        error = null;
        return true;
    }

    public bool Execute(string[] args, out string feedback)
    {
        int amount = int.Parse(args[0]);
        var ctx = CheatContext.Instance;

        if (ctx.SaveManager?.Data?.CurrentRun == null)
        {
            feedback = "❌ Nenhuma run ativa.";
            return false;
        }

        ctx.EconomyService.Earn(amount, TransactionType.Debug);
        feedback = $"✅ +${amount} adicionados.";
        return true;
    }
}
