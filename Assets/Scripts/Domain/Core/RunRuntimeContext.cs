using UnityEngine;

/// <summary>
/// Contexto RUNTIME - Muda durante a run.
/// 
/// Contém serviços que:
/// - Dependem de cena
/// - Podem ser destruídos/recriados
/// - NÃO definem identidade da run
/// 
/// É MUTÁVEL e pode ser atualizado conforme cenas carregam.
/// </summary>
public class RunRuntimeContext
{
    public IGridService GridService { get; set; }

    public RunRuntimeContext(IGridService gridService = null)
    {
        GridService = gridService;
    }

    /// <summary>
    /// Atualiza o GridService quando cena carrega.
    /// </summary>
    public void SetGridService(IGridService gridService)
    {
        if (gridService == null)
        {
            Debug.LogWarning("[RunRuntimeContext] GridService é null!");
            return;
        }

        GridService = gridService;
        Debug.Log("[RunRuntimeContext] GridService atualizado.");
    }

    /// <summary>
    /// Cleanup quando cena descarrega.
    /// </summary>
    public void Cleanup()
    {
        GridService = null;
        Debug.Log("[RunRuntimeContext] Limpeza concluída.");
    }

    /// <summary>
    /// Query: Verifica se GridService está disponível.
    /// </summary>
    public bool HasGridService => GridService != null;
}
