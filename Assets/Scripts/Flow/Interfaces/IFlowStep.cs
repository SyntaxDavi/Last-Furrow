using Cysharp.Threading.Tasks;

public interface IFlowStep
{
    // Agora recebe o "Controle Remoto" do pipeline
    //Nenhum Step deve assumir que sempre rodará até o fim.
    UniTask Execute(FlowControl control);
}