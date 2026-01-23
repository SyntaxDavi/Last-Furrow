using UnityEngine;

/// <summary>
/// Classe base para módulos do AppCore.
/// Permite inicialização organizada e registro de serviços.
/// </summary>
public abstract class BaseModule
{
    protected readonly ServiceRegistry Registry;
    protected readonly AppCore App;

    protected BaseModule(ServiceRegistry registry, AppCore app)
    {
        Registry = registry;
        App = app;
    }

    /// <summary>
    /// Chamado para inicializar o módulo e seus serviços.
    /// </summary>
    public abstract void Initialize();
}
