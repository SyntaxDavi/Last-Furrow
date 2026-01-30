using System.Collections.Generic;
using System.Linq;
using LastFurrow.Traditions;
using UnityEngine;

/// <summary>
/// Cheats para testar o sistema de tradições.
/// </summary>

[Cheat("tradition_add", "Traditions", "Adiciona uma tradição aleatória")]
public class AddRandomTraditionCheat : ICheatCommand
{
    public string Id => "tradition_add";
    public string Category => "Traditions";
    public string Description => "Adiciona uma tradição aleatória";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var library = ctx.Library;
        
        // Por enquanto, acessa diretamente já que o serviço ainda não está no AppCore
        var traditions = library.GetRandomTraditions(1);
        if (traditions.Count == 0)
        {
            feedback = "❌ Nenhuma tradição disponível no GameDatabase";
            return false;
        }
        
        var run = ctx.RunData;
        if (run == null)
        {
            feedback = "❌ Nenhuma run ativa";
            return false;
        }
        
        if (run.ActiveTraditionIDs.Count >= run.MaxTraditionSlots)
        {
            feedback = $"❌ Máximo de tradições atingido ({run.MaxTraditionSlots})";
            return false;
        }
        
        var tradition = traditions[0];
        run.ActiveTraditionIDs.Add(tradition.ID);
        ctx.SaveManager?.SaveGame();
        
        feedback = $"✅ Tradição adicionada: {tradition.DisplayName}. Recarregue a scene.";
        return true;
    }
}

[Cheat("tradition_list", "Traditions", "Lista todas as tradições ativas")]
public class ListTraditionsCheat : ICheatCommand
{
    public string Id => "tradition_list";
    public string Category => "Traditions";
    public string Description => "Lista todas as tradições ativas";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.RunData;
        
        if (run == null)
        {
            feedback = "❌ Nenhuma run ativa";
            return false;
        }
        
        if (run.ActiveTraditionIDs.Count == 0)
        {
            feedback = "📋 Nenhuma tradição ativa";
            return true;
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 Tradições Ativas ({run.ActiveTraditionIDs.Count}/{run.MaxTraditionSlots}):");
        
        for (int i = 0; i < run.ActiveTraditionIDs.Count; i++)
        {
            var id = run.ActiveTraditionIDs[i];
            var tradId = new TraditionID(id);
            string name = id;
            
            if (ctx.Library.TryGetTradition(tradId, out var data))
            {
                name = data.DisplayName;
            }
            
            sb.AppendLine($"  [{i}] {name} (ID: {id})");
        }
        
        feedback = sb.ToString();
        return true;
    }
}

[Cheat("tradition_remove", "Traditions", "Remove tradição por índice")]
public class RemoveTraditionCheat : ICheatCommand
{
    public string Id => "tradition_remove";
    public string Category => "Traditions";
    public string Description => "Remove tradição por índice. Uso: tradition_remove <index>";
    
    public bool ValidateArgs(string[] args, out string error)
    {
        if (args.Length == 0 || !int.TryParse(args[0], out _))
        {
            error = "Uso: tradition_remove <índice>";
            return false;
        }
        error = null;
        return true;
    }

    public bool Execute(string[] args, out string feedback)
    {
        var run = CheatContext.Instance.RunData;
        
        if (run == null)
        {
            feedback = "❌ Nenhuma run ativa";
            return false;
        }
        
        int index = int.Parse(args[0]);
        
        if (index < 0 || index >= run.ActiveTraditionIDs.Count)
        {
            feedback = $"❌ Índice inválido (0-{run.ActiveTraditionIDs.Count - 1})";
            return false;
        }
        
        var removedId = run.ActiveTraditionIDs[index];
        run.ActiveTraditionIDs.RemoveAt(index);
        CheatContext.Instance.SaveManager?.SaveGame();
        
        feedback = $"✅ Tradição removida: {removedId}. Recarregue a scene.";
        return true;
    }
}

[Cheat("tradition_slots", "Traditions", "Define slots de tradições")]
public class SetTraditionSlotsCheat : ICheatCommand
{
    public string Id => "tradition_slots";
    public string Category => "Traditions";
    public string Description => "Define slots de tradições. Uso: tradition_slots <amount>";
    
    public bool ValidateArgs(string[] args, out string error)
    {
        if (args.Length == 0 || !int.TryParse(args[0], out int amount) || amount < 1)
        {
            error = "Uso: tradition_slots <quantidade> (mínimo 1)";
            return false;
        }
        error = null;
        return true;
    }

    public bool Execute(string[] args, out string feedback)
    {
        var run = CheatContext.Instance.RunData;
        
        if (run == null)
        {
            feedback = "❌ Nenhuma run ativa";
            return false;
        }
        
        int oldMax = run.MaxTraditionSlots;
        int newMax = int.Parse(args[0]);
        run.MaxTraditionSlots = newMax;
        CheatContext.Instance.SaveManager?.SaveGame();
        
        feedback = $"✅ Slots de tradições: {oldMax} → {newMax}";
        return true;
    }
}

[Cheat("tradition_available", "Traditions", "Lista tradições disponíveis no database")]
public class ListAvailableTraditionsCheat : ICheatCommand
{
    public string Id => "tradition_available";
    public string Category => "Traditions";
    public string Description => "Lista tradições disponíveis no database";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var allTraditions = CheatContext.Instance.Library.GetAllTraditions().ToList();
        
        if (allTraditions.Count == 0)
        {
            feedback = "📋 Nenhuma tradição no GameDatabase. Rode Auto Populate.";
            return true;
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 Tradições Disponíveis ({allTraditions.Count}):");
        
        foreach (var t in allTraditions)
        {
            sb.AppendLine($"  • {t.ID}: {t.DisplayName} ({t.Rarity})");
        }
        
        feedback = sb.ToString();
        return true;
    }
}
