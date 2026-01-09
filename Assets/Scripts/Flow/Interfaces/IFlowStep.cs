using System.Collections;

public interface IFlowStep
{
    // Executa a lógica do passo.
    // Retorna IEnumerator para permitir animações, fades e esperas.
    IEnumerator Execute();
}