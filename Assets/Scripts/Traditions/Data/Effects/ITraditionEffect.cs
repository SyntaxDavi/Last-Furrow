using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Interface base para todos os efeitos de Tradição.
    /// Efeitos são avaliados em fases específicas baseadas no TraditionEffectPhase.
    /// </summary>
    public interface ITraditionEffect
    {
        /// <summary>
        /// Nome do efeito para debug/UI.
        /// </summary>
        string EffectName { get; }
        
        /// <summary>
        /// Descrição formatada do efeito (pode usar placeholders para valores).
        /// </summary>
        string GetDescription();
        
        /// <summary>
        /// Define em qual fase da análise este efeito é avaliado.
        /// </summary>
        TraditionEffectPhase Phase { get; }
    }
    
    /// <summary>
    /// Define em qual fase do jogo/análise um efeito de tradição é avaliado.
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║                    ORDEM DE AVALIAÇÃO DE TRADIÇÕES                        ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                           ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ FASE 1: PRE_SCORING (Modificadores de Base)                         │  ║
    /// ║  │ Momento: Antes de calcular pontos passivos                          │  ║
    /// ║  │ Exemplos:                                                           │  ║
    /// ║  │   • "Milho dá +2 pontos base"                                       │  ║
    /// ║  │   • "Crops maduros valem o dobro"                                   │  ║
    /// ║  │   • "Slots da primeira linha dão +1"                                │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                              ↓                                            ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ [SISTEMA] PASSIVE SCORING                                           │  ║
    /// ║  │ → Calcula pontos base de cada slot ocupado                          │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                              ↓                                            ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ FASE 2: ON_PATTERN_DETECTED (Durante Detecção de Padrões)           │  ║
    /// ║  │ Momento: Quando cada padrão é encontrado, antes de pontuar          │  ║
    /// ║  │ Exemplos:                                                           │  ║
    /// ║  │   • "Rows dão +5 pontos extras"                                     │  ║
    /// ║  │   • "Padrões com 3+ culturas = x1.5"                                │  ║
    /// ║  │   • "Full Grid dá vida extra"                                       │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                              ↓                                            ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ [SISTEMA] PATTERN SCORING                                           │  ║
    /// ║  │ → Detecta e pontua todos os padrões                                 │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                              ↓                                            ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ FASE 3: POST_PATTERN (Após Padrões, Antes do Total)                 │  ║
    /// ║  │ Momento: Depois de todos os padrões serem pontuados                 │  ║
    /// ║  │ Exemplos:                                                           │  ║
    /// ║  │   • "+10 se você completou 2+ padrões"                              │  ║
    /// ║  │   • "Converte metade dos pontos de pattern em dinheiro"             │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                              ↓                                            ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ FASE 4: FINAL_MULTIPLIER (Multiplicadores Finais)                   │  ║
    /// ║  │ Momento: Última coisa antes de aplicar o score                      │  ║
    /// ║  │ Exemplos:                                                           │  ║
    /// ║  │   • "+15% do total"                                                 │  ║
    /// ║  │   • "Dobra se score > 100"                                          │  ║
    /// ║  │   • "x2 se vida cheia"                                              │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                              ↓                                            ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ [SISTEMA] APPLY SCORE                                               │  ║
    /// ║  │ → Adiciona pontos ao score semanal e salva                          │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                                                                           ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                           ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ FASE 5: POST_DAY (Após Resolução do Dia)                            │  ║
    /// ║  │ Momento: Depois de tudo, antes do draw de cartas                    │  ║
    /// ║  │ Exemplos:                                                           │  ║
    /// ║  │   • "A cada 3 dias, spawna milho aleatório"                         │  ║
    /// ║  │   • "Ganha $5 por crop maduro"                                      │  ║
    /// ║  │   • "Cura 1 vida se score > meta"                                   │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                                                                           ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                           ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐  ║
    /// ║  │ FASE 6: PERSISTENT (Sempre Ativo - Fora da Análise)                 │  ║
    /// ║  │ Momento: Enquanto a tradição estiver equipada                       │  ║
    /// ║  │ Exemplos:                                                           │  ║
    /// ║  │   • "Cartas na loja custam -10%"                                    │  ║
    /// ║  │   • "Mão máxima +2 cartas"                                          │  ║
    /// ║  │   • "Cartas de colheita dão +$5"                                    │  ║
    /// ║  │   • "Desbloqueia carta especial no deck"                            │  ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘  ║
    /// ║                                                                           ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// 
    /// NOTA: A ORDEM das tradições importa!
    /// Dentro de cada fase, tradições são avaliadas na ordem em que aparecem.
    /// Por isso o jogador pode reordená-las.
    /// </summary>
    public enum TraditionEffectPhase
    {
        /// <summary>
        /// Antes de calcular pontos passivos.
        /// Modifica valores base de crops/slots.
        /// </summary>
        PreScoring = 0,
        
        /// <summary>
        /// Quando cada padrão é detectado.
        /// Pode modificar pontos do padrão ou dar bônus extras.
        /// </summary>
        OnPatternDetected = 1,
        
        /// <summary>
        /// Depois de todos os padrões serem pontuados.
        /// Bônus condicionais baseados em quantos/quais padrões.
        /// </summary>
        PostPattern = 2,
        
        /// <summary>
        /// Última fase - multiplicadores finais no score total.
        /// A maioria das tradições deve usar esta fase.
        /// </summary>
        FinalMultiplier = 3,
        
        /// <summary>
        /// Após a resolução completa do dia.
        /// Efeitos de "fim de turno" como spawnar crops ou ganhar dinheiro.
        /// </summary>
        PostDay = 4,
        
        /// <summary>
        /// Sempre ativo enquanto equipado.
        /// Efeitos permanentes: descontos na loja, mão maior, etc.
        /// NÃO é chamado durante análise - é verificado sob demanda.
        /// </summary>
        Persistent = 5
    }

    /// <summary>
    /// Tipos de modificadores persistentes que tradições podem fornecer.
    /// Usado por sistemas externos para consultar bônus ativos.
    /// </summary>
    public enum PersistentModifierType
    {
        /// <summary>Modificador de preço na loja (negativo = desconto)</summary>
        ShopPriceModifier,
        
        /// <summary>Modificador de tamanho máximo da mão</summary>
        MaxHandSize,
        
        /// <summary>Modificador de cartas compradas por dia</summary>
        CardsPerDay,
        
        /// <summary>Modificador de dinheiro ganho ao colher</summary>
        HarvestMoneyBonus,
        
        /// <summary>Modificador de vidas máximas</summary>
        MaxLives
    }
    
    /// <summary>
    /// Interface para efeitos que fornecem modificadores persistentes.
    /// Implementada por efeitos com Phase = Persistent.
    /// </summary>
    public interface IPersistentModifier
    {
        int GetModifier(PersistentModifierType type);
    }
}
