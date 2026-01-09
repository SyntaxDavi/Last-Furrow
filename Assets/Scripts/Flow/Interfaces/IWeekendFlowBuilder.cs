using System.Collections.Generic;

public interface IWeekendFlowBuilder
{
    // Constrói a lista de passos para ENTRAR no fim de semana (Fade Out -> UI -> Loja -> Fade In)
    List<IFlowStep> BuildEnterPipeline(RunData runData);

    // Constrói a lista de passos para SAIR do fim de semana (Fade Out -> UI -> Gameplay -> Fade In)
    List<IFlowStep> BuildExitPipeline(RunData runData);
}