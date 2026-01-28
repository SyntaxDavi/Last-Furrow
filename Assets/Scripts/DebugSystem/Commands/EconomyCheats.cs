[Cheat("add_money","Economy","Adicionar dinheiro ao jogador")]
public class AddMoneyCheat : ICheatCommand
{
    public string ID => "add_money";
    public string Category => "Economy";
    public string Description => "Adicionar dinheiro ao jogador. Uso: add_money [quantidade]";

public bool ValidateArgs(string[] args, out string error)
    {
       if(args.Length == 0 || !int.TryParse(args[0], out _))
       {
           error = "Argumento deve ser um numero inteiro";
           return false;
       }
       error = null;
       return true;
    }
public bool Execute(string[] args, out string feedback)
    {
        int amount = int.Parse(args[0]);
        var run = AppCore.Instance?.SaveManager?.Data?.CurrentRun;
        if(run == null)
        {
            feedback = "Nao foi possivel encontrar um run valido";
            return false;
        }
        AppCore.Instance.EconomyService.Earn(amount, TransactionType.Debug);
        feedback = $"Adicionado {amount} moedas ao jogador";
        return true;
    }
