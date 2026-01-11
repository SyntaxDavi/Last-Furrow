using System.Collections;

public interface IFlowStep
{
    // Agora recebe o "Controle Remoto" do pipeline
    //Nenhum Step deve assumir que sempre rodará até o fim.
    IEnumerator Execute(FlowControl control);
}