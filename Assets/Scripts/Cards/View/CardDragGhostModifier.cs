using UnityEngine;

/// <summary>
/// Modificador visual que aplica transparência "ghost" à carta durante drag sobre grid.
/// Usa sistema de MULTIPLICADOR de alpha (não absoluto) para composição com outros efeitos.
/// </summary>
public class CardDragGhostModifier
{
    private readonly SpriteRenderer[] _renderers;
    private readonly float[] _baseAlphas;
    private readonly CardVisualConfig _config;
    
    private bool _isGhostMode;
    private float _currentMultiplier = 1f;
    private float _lastAppliedMultiplier = 1f;
    
    public CardDragGhostModifier(SpriteRenderer[] renderers, CardVisualConfig config)
    {
        _config = config;
        
        // Filtra renderers nulos e cacheia alphas base
        var validRenderers = new System.Collections.Generic.List<SpriteRenderer>();
        var validAlphas = new System.Collections.Generic.List<float>();
        
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    validRenderers.Add(r);
                    validAlphas.Add(r.color.a); // Captura alpha original
                }
            }
        }
        
        _renderers = validRenderers.ToArray();
        _baseAlphas = validAlphas.ToArray();
    }
    
    /// <summary>
    /// Ativa/desativa o modo ghost (transparente).
    /// </summary>
    public void SetGhostMode(bool isGhost)
    {
        _isGhostMode = isGhost;
    }
    
    /// <summary>
    /// Força reset imediato do alpha (sem transição).
    /// </summary>
    public void ForceReset()
    {
        _isGhostMode = false;
        _currentMultiplier = 1f;
        ApplyMultiplier(1f);
    }
    
    /// <summary>
    /// Atualiza a transição de alpha suave.
    /// </summary>
    public void Update()
    {
        float targetMultiplier = _isGhostMode ? _config.DragGhostAlpha : 1f;
        
        // Robustez: garante aplicação exata ao atingir o target
        if (Mathf.Approximately(_currentMultiplier, targetMultiplier))
        {
            if (!Mathf.Approximately(_currentMultiplier, targetMultiplier))
            {
                _currentMultiplier = targetMultiplier;
                ApplyMultiplier(_currentMultiplier);
            }
            return;
        }
        
        _currentMultiplier = Mathf.MoveTowards(
            _currentMultiplier, 
            targetMultiplier, 
            _config.DragGhostTransitionSpeed * Time.deltaTime
        );
        
        ApplyMultiplier(_currentMultiplier);
    }
    
    private void ApplyMultiplier(float multiplier)
    {
        // Evita escrita redundante
        if (Mathf.Approximately(multiplier, _lastAppliedMultiplier)) return;
        _lastAppliedMultiplier = multiplier;
        
        for (int i = 0; i < _renderers.Length; i++)
        {
            var renderer = _renderers[i];
            if (renderer == null) continue;
            
            // Multiplica sobre o alpha BASE capturado (não o atual)
            var color = renderer.color;
            color.a = _baseAlphas[i] * multiplier;
            renderer.color = color;
        }
    }
}
