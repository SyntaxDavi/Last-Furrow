using UnityEngine;

/// <summary>
/// Sistema responsável APENAS por detectar e processar cliques.
/// 
/// Responsabilidades:
/// - Detectar clique (mouse down + up sem drag)
/// - Encontrar objeto clicável sob o mouse
/// - Disparar OnClick no objeto
/// 
/// Diferença de Hover: Click precisa de um "alvo" no momento do mouseDown,
/// não apenas no mouseUp. Isso evita cliques acidentais.
/// 
/// NÃO decide se pode ou não clicar - isso é do InteractionPolicy.
/// </summary>
public class ClickSystem
{
    private readonly LayerMask _clickableLayer;
    
    // Estado
    private IInteractable _clickTarget;
    private bool _hasClickTarget;

    public ClickSystem(LayerMask clickableLayer)
    {
        _clickableLayer = clickableLayer;
    }

    /// <summary>
    /// Registra o alvo do clique quando mouse é pressionado.
    /// </summary>
    public void RegisterClickTarget(Vector2 worldPos)
    {
        Collider2D col = Physics2D.OverlapPoint(worldPos, _clickableLayer);
        
        if (col != null)
        {
            _clickTarget = col.GetComponent<IInteractable>();
            _hasClickTarget = _clickTarget != null;
        }
        else
        {
            _clickTarget = null;
            _hasClickTarget = false;
        }
    }

    /// <summary>
    /// Tenta executar o clique. Retorna true se clicou em algo.
    /// Só executa se o mouse ainda está sobre o mesmo objeto do mouseDown.
    /// </summary>
    public bool TryExecuteClick(Vector2 worldPos)
    {
        if (!_hasClickTarget || _clickTarget == null)
        {
            Clear();
            return false;
        }

        // Verifica se ainda está sobre o mesmo objeto
        Collider2D col = Physics2D.OverlapPoint(worldPos, _clickableLayer);
        IInteractable currentTarget = col != null ? col.GetComponent<IInteractable>() : null;

        bool clicked = false;
        
        if (currentTarget == _clickTarget && IsObjectAlive(_clickTarget))
        {
            _clickTarget.OnClick();
            clicked = true;
        }

        Clear();
        return clicked;
    }

    /// <summary>
    /// Limpa o estado (ex: drag começou, cancela click pendente).
    /// </summary>
    public void Clear()
    {
        _clickTarget = null;
        _hasClickTarget = false;
    }

    /// <summary>
    /// Verifica se há um click pendente.
    /// </summary>
    public bool HasPendingClick => _hasClickTarget;

    private bool IsObjectAlive(object obj)
    {
        return obj != null && !((obj is Object unityObj) && unityObj == null);
    }
}
