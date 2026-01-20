# ?? PATTERN VISUAL JUICE SYSTEM - Design Document

**Branch:** `feature/pattern-visual-juice`  
**Objetivo:** Adicionar feedback visual e animações responsivas ao Pattern System  
**Filosofia:** "Um jogo de baralho precisa de JUICE" - Feedback visual claro, satisfatório e educativo

---

## ?? VISÃO GERAL

### Pilares do Sistema
1. **Responsividade Visual** - Grid reage a ações do jogador
2. **Clareza de Informação** - Jogador entende o que está acontecendo
3. **Satisfação (Juice)** - Cada ação tem feedback gratificante
4. **Performance** - Customizável e otimizado para PC

### Escopo
- ? Pattern Highlight System (slots brilham quando formam padrões)
- ? Pop-up de Texto Flutuante (ex: "LINHA!", "CRUZ!", etc)
- ? Grid Breathing Animation (terra "viva" que respira)
- ? Tabela do Fazendeiro (end-of-day summary)
- ? Decay Warning Visual (padrões perdem brilho com tempo)
- ? Combo Counter (padrões consecutivos)
- ? Sinergia Visual (explosion + aura quando múltiplos padrões)
- ? Particle System (sparkles stylized - Hollow Knight/Stardew style)

---

## ?? ARQUITETURA & LAYOUT HUD

### HUD Atual (Baseado em Screenshot)
```
???????????????????????????????????????????????????????????????
? ??????                                ????????????????????   ?
?                                      ? Meta: 115 / 500  ?   ?
?                                      ? Dia 5 - Semana 1 ?   ?
?                                      ? $10              ?   ?
?                                      ????????????????????   ?
?                                                             ?
?           ???????????????????????    ????????????????????   ?
?           ?                     ?    ? Padrões: +0      ?   ? ? ÁREA POP-UPS
?           ?      GRID 5x5       ?    ? (0 detectados)   ?   ?
?           ?                     ?    ?                  ?   ?
?           ?     (Centro)        ?    ? [ESPAÇO LIVRE]   ?   ? ? Tradições (futuro)
?           ?                     ?    ?                  ?   ?
?           ???????????????????????    ????????????????????   ?
?                                                             ?
?                    ???????????????                          ?
?                    ?   [Cartas]  ?                          ?
?                    ???????????????                          ?
?  [??]              [Shuffle] [Sleep]                        ?
???????????????????????????????????????????????????????????????
```

### Nova Organização Visual
```
???????????????????????????????????????????????????????????????
? ?????? [Combo x3 ??]                  ????????????????????   ? ? Combo aparece aqui
?                                      ? Meta: 115 / 500  ?   ?
?                                      ? Dia 5 - Semana 1 ?   ?
?                                      ? $10              ?   ?
?                                      ????????????????????   ?
?                                                             ?
?           ???????????????????????    ????????????????????   ?
?           ?  [? LINHA! ?]     ?   ? Padrões: +45     ?   ? ? Pop-up aqui
?           ?      GRID 5x5       ?    ? (3 detectados)   ?   ?
?           ?   (Com Highlight)   ?    ?                  ?   ?
?           ?   [Aura Sinergia]   ?    ? [Tradições]      ?   ? ? Futuro
?           ?                     ?    ?                  ?   ?
?           ???????????????????????    ????????????????????   ?
?                 ?                                           ?
?          [Partículas]                                       ?
?                    ???????????????                          ?
?                    ?   [Cartas]  ?                          ?
?                    ???????????????                          ?
?  [??]              [Shuffle] [Sleep] ? [Tabela Fazendeiro]  ?
???????????????????????????????????????????????????????????????
```

---

## ?? FEATURES DETALHADAS

### 1?? Pattern Highlight System
**Responsabilidade:** Mostrar visualmente quais slots formam cada padrão detectado

**Comportamento:**
- Slots que formam padrão recebem highlight colorido baseado em Tier
- Animação de pulse (respira) durante 1.5s
- Se múltiplos padrões, mostra sequencialmente (delay 0.15s entre cada)
- Cor varia com decay (perde saturação/brilho)

**Configuração Inspector:**
```csharp
[Header("Tier Colors")]
Color tier1Color = Prata/Cinza      // Tier 1: 5-15 pts
Color tier2Color = Verde            // Tier 2: 15-35 pts
Color tier3Color = Dourado          // Tier 3: 35-60 pts
Color tier4Color = Roxo/Místico     // Tier 4: 80-150 pts

[Header("Animation")]
float pulseSpeed = 2f               // Velocidade do pulse
float highlightDuration = 1.5f      // Quanto tempo fica visível
float sequentialDelay = 0.15f       // Delay entre padrões
```

**Decay Integration:**
- Dia 1-2: Cor vibrante (100%)
- Dia 3: -20% saturação (warning sutil)
- Dia 4+: -40% saturação + tint vermelho (crítico)

---

### 2?? Pop-up de Texto Flutuante
**Responsabilidade:** Mostrar nome do padrão detectado (ex: "LINHA!", "CRUZ!")

**Posição:** Área direita (onde está "Padrões: +0"), acima do texto existente

**Comportamento:**
- Um por vez, sequencial (se 3 padrões, mostra 3 pop-ups em sequência)
- Fade in rápido (0.2s) ? Hold (0.8s) ? Fade out rápido (0.2s)
- Cor do texto = cor do Tier do padrão
- Cor muda com decay (mesmo sistema de highlight)
- Tamanho do texto proporcional ao score (padrões valiosos = texto maior)

**Configuração Inspector:**
```csharp
[Header("Positioning")]
Vector2 popupOffset = (0, 50)       // Offset do texto "Padrões"
bool stackVertical = true           // Empilhar se múltiplos

[Header("Animation Timing")]
float fadeInDuration = 0.2f
float holdDuration = 0.8f
float fadeOutDuration = 0.2f

[Header("Text Style")]
float baseTextSize = 36f            // Tamanho base
float scaleBonusPerTier = 1.2f      // Tier 4 = 1.2x maior que Tier 1
bool decayAffectsOpacity = true     // Decay reduz opacidade
```

**Exemplos:**
- Tier 1: "Par!" (pequeno, prata)
- Tier 2: "LINHA!" (médio, verde)
- Tier 3: "DIAGONAL!" (grande, dourado)
- Tier 4: "GRID PERFEITO!" (enorme, roxo brilhante)

---

### 3?? Grid Breathing Animation
**Responsabilidade:** Grid "respira" suavemente + reage a ações do jogador

**Comportamento Base (Idle):**
- Escala do grid oscila: 1.0 ? 1.02 ? 1.0 (ciclo lento ~3s)
- Simula terra "viva"

**Reações a Ações:**
- **Plantar:** Pequeno "thump" (escala 1.0 ? 0.98 ? 1.02 ? 1.0)
- **Colher:** Leve "bounce" (escala 1.0 ? 1.03 ? 1.0)
- **Padrão detectado:** Pulse mais forte (escala 1.0 ? 1.05 ? 1.0)

**Configuração Inspector:**
```csharp
[Header("Breathing (Idle)")]
float breathingAmount = 0.02f       // Amplitude (1.0 ± 0.02)
float breathingSpeed = 0.3f         // Velocidade (ciclos/segundo)
AnimationCurve breathingCurve       // Curva (ease in/out)

[Header("Reactions")]
float plantReactionStrength = 0.02f
float harvestReactionStrength = 0.03f
float patternReactionStrength = 0.05f
float reactionDuration = 0.3f
```

---

### 4?? Tabela do Fazendeiro (End-of-Day Summary)
**Responsabilidade:** Mostrar resumo detalhado dos padrões do dia ANTES de passar o dia

**Novo Flow:**
```
Sleep ? Verify Grid ? [TABELA DO FAZENDEIRO] ? (Player fecha) ? Save ? Next Day
```

**Conteúdo:**
```
???????????????????????????????????????????
?      ?? DIÁRIO DO FAZENDEIRO            ?
?         Dia 5 - Semana 1                ?
???????????????????????????????????????????
?                                         ?
?  ?? PADRÕES HOJE:                       ?
?                                         ?
?  ? 2x Linha Completa        50 pts     ?
?  ? 1x Cruz Simples           30 pts    ?
?  ? 3x Par Adjacente          15 pts    ?
?                              ?????????  ?
?  Subtotal:                   95 pts    ?
?  ?? Sinergia (5 padrões):   +19 pts    ?
?                              ?????????  ?
?  TOTAL DO DIA:              114 pts    ?
?                                         ?
?  ? Decay ativo em 2 padrões            ?
?                                         ?
?     [CONTINUAR] [ESC para fechar]      ?
???????????????????????????????????????????
```

**Estilo Visual:**
- Background: Pergaminho/papel envelhecido (rústico)
- Fontes: Moderna mas com serifa leve (personalidade)
- Ícones: Stylized (manter consistência)

**Configuração Inspector:**
```csharp
[Header("Display")]
bool showDetailedBreakdown = true   // Mostrar cada padrão
bool showSynergyCalculation = true  // Mostrar cálculo de sinergia
bool showDecayWarnings = true       // Avisar sobre decay

[Header("Interaction")]
bool requireConfirmation = true     // Obriga clicar "Continuar"
bool allowESCClose = true           // ESC fecha rápido
float autoCloseDuration = 0f        // 0 = nunca fecha auto

[Header("Visual Style")]
Sprite backgroundTexture            // Papel/pergaminho
Color textPrimaryColor
Color textAccentColor               // Destaque (números)
Color decayWarningColor             // Vermelho/Laranja
```

---

### 5?? Combo Counter
**Responsabilidade:** Contar e exibir quantos padrões estão ativos

**Lógica:**
- Cada padrão detectado = +1 combo
- Cada dia com sinergia (2+ padrões) = mantém combo
- Combo reseta: Novo dia SEM padrões OU fim de semana

**Posição:** Topo esquerdo, ao lado dos corações (aparece dinamicamente)

**Visual:**
```
??????  [?? COMBO x5]
```

**Estados:**
- Combo 1-2: Texto simples (branco)
- Combo 3-5: Texto dourado + leve pulse
- Combo 6+: Texto roxo + particle trail

**Configuração Inspector:**
```csharp
[Header("Display")]
int minComboToShow = 2              // Só mostra se combo >= 2
bool showFireEmoji = true           // Emoji de fogo
Vector2 position = TopLeft + Offset

[Header("Visual Tiers")]
int comboTier2Threshold = 3         // Dourado
int comboTier3Threshold = 6         // Roxo épico
Color tier1Color, tier2Color, tier3Color

[Header("Animation")]
bool pulseOnIncrease = true
float pulseDuration = 0.5f
bool spawnParticlesOnTier3 = true
```

---

### 6?? Sinergia Visual
**Responsabilidade:** Mostrar visualmente quando há bonus de sinergia (2+ padrões)

**Componentes:**
1. **Explosion no centro do grid** (burst rápido)
2. **Aura dourada envolvendo grid** (persiste 1.5s)

**Intensidade Escalável:**
- 2 padrões: Explosion pequena + aura leve
- 4 padrões: Explosion média + aura vibrante
- 6+ padrões: Explosion grande + aura pulsante

**Configuração Inspector:**
```csharp
[Header("Activation")]
int minPatternsForVisual = 2        // Threshold

[Header("Explosion")]
GameObject explosionPrefab          // Particle system
float explosionIntensityPerPattern = 0.2f
Vector3 explosionSpawnOffset        // Centro do grid

[Header("Aura")]
SpriteRenderer auraRenderer         // Sprite circular
float auraDuration = 1.5f
float auraIntensityPerPattern = 0.15f
AnimationCurve auraFadeCurve

[Header("Colors")]
Gradient synergyGradient            // 2?4?6+ padrões
```

---

### 7?? Particle System
**Responsabilidade:** Sparkles e efeitos visuais stylized (Hollow Knight/Stardew)

**Tipos de Partículas:**
1. **Pattern Detected** - Sparkles nos slots do padrão
2. **Synergy Burst** - Explosion no centro
3. **Combo Increase** - Trail no combo counter
4. **Recreation Bonus** - Confete leve (+10%)

**Estilo Visual:**
- Formas: Estrelas simples, círculos, folhas stylized
- Cores: Seguem Tier colors do padrão
- Movimento: Suave, orgânico (não caótico)
- Lifetime: 0.5-1.5s

**Configuração Inspector:**
```csharp
[Header("Performance")]
int maxParticlesSimultaneous = 100
bool enableParticles = true
ParticleQualityLevel quality = High // High/Medium/Low

[Header("Pattern Particles")]
GameObject patternSparklesPrefab
int sparklesPerSlot = 3
bool onlyTier3AndAbove = false      // Apenas padrões valiosos

[Header("Special Effects")]
GameObject synergyBurstPrefab
GameObject comboPrefab
GameObject recreationPrefab
```

---

## ??? ARQUITETURA TÉCNICA

### Estrutura de Arquivos
```
Assets/Scripts/UI/Patterns/Visual/
??? Core/
?   ??? PatternVisualConfig.cs         ? Configuração centralizada (SO)
?   ??? VisualQueueSystem.cs           ? Fila de priorização de eventos
?   ??? PatternObjectPool.cs           ? Pooling centralizado (pop-ups, partículas)
??? PatternHighlightController.cs      ? Highlights de slots
??? PatternTextPopupController.cs      ? Pop-ups "LINHA!"
??? GridBreathingController.cs         ? Animação do grid (com priority system)
??? FarmerDiaryPanel.cs                ? Tabela do fazendeiro
??? PatternComboCounter.cs             ? Combo display
??? PatternSynergyVisual.cs            ? Explosion + aura
??? PatternParticleManager.cs          ? Gerenciador de partículas (pooling)
```

### Event-Driven (SOLID)
Todos os componentes escutam `PatternEvents` e **SEMPRE fazem unsubscribe em OnDestroy/OnDisable**:

```csharp
private void OnEnable()
{
    AppCore.Instance?.Events.Pattern.OnPatternsDetected += HandlePatternsDetected;
}

private void OnDisable()
{
    if (AppCore.Instance != null)
    {
        AppCore.Instance.Events.Pattern.OnPatternsDetected -= HandlePatternsDetected;
    }
}
```

**Eventos:**
- `OnPatternsDetected(matches, totalPoints)` ? Trigger visuals (via queue)
- `OnPatternDecayApplied(match, days, multiplier)` ? Decay warning
- `OnPatternRecreated(match)` ? Recreation celebration

**?? CRITICAL:** Nenhum acoplamento direto entre componentes! Tudo via eventos.

---

## ?? PRIORIZAÇÃO DE IMPLEMENTAÇÃO

### Sprint 0 - Fundação (1-2 horas) **[NOVO]**
0.1. ? **PatternVisualConfig.cs** (ScriptableObject centralizado)
0.2. ? **PatternObjectPool.cs** (Pooling system)
0.3. ? **VisualQueueSystem.cs** (Event queue com prioridades)

**Objetivo:** Infraestrutura sólida antes de features visuais.

---

### Sprint 1 - Core Juice (2-3 horas)
1. ? **PatternHighlightController** - Impacto visual máximo
   - Integrar com VisualQueueSystem
   - Usar PatternObjectPool para highlights temporários
   - Implementar decay threshold
2. ? **PatternTextPopupController** - Clareza de feedback
   - Pooling de TextMeshPro prefabs
   - Queue sequencial via VisualQueueSystem
3. ? **GridBreathingController** - Vida ao grid
   - Priority system (Pattern > Harvest > Plant)
   - AnimationCurve customizável

---

### Sprint 2 - Information Clarity (2-3 horas)
4. ? **FarmerDiaryPanel** - Tabela do fazendeiro (new flow)
   - Integrar PatternScoreTotalResult metadata
   - ESC to close + confirmation button
5. ? **Decay Warning Visual** - Integrar com highlights
   - Threshold configurável (apenas padrões importantes)
   - Gradiente sutil ? crítico

---

### Sprint 3 - Polish & Special FX (2-3 horas)
6. ? **PatternComboCounter** - Streak tracking
   - Tracking em RunData
   - Visual tiers (white ? gold ? purple)
7. ? **PatternSynergyVisual** - Explosion + aura
   - Intensidade escalável (2+ ? 4+ ? 6+)
   - Pooling de particle systems
8. ? **PatternParticleManager** - Sparkles stylized
   - Performance budget system
   - LOD scaling automático

---

### Sprint 4 - Polish & Testing (1-2 horas) **[NOVO]**
9. ? **Debug Mode** - Ferramentas de teste
   - PatternVisualConfig debug flags
   - Performance metrics overlay
10. ? **Edge Case Testing** - Garantir robustez
    - Combo persistence
    - Decay + Recreation bonus
    - Memory leak checks (profiler)

**Total estimado:** 8-12 horas de implementação (com fundação + polish)

---

## ?? PRINCÍPIOS DE DESIGN

1. **Customizável** - Tudo ajustável no Inspector
2. **Performático** - Pooling de objetos, LOD de partículas
3. **Modular** - Cada feature pode ser desabilitada individualmente
4. **Responsivo** - Feedback imediato a ações do jogador
5. **Educativo** - Jogador aprende mecânicas através do visual
6. **Satisfatório** - "Game feel" AAA mesmo sendo indie

---

## ?? CONSIDERAÇÕES CRÍTICAS & SOLUÇÕES

### 1?? Visual Queue System - Evitar Sobrecarga Visual

**PROBLEMA:** Múltiplos padrões detectados ao mesmo tempo = highlights + pop-ups + sinergia + partículas = confusão.

**SOLUÇÃO: VisualQueueSystem.cs**
```csharp
public enum VisualEventPriority
{
    Critical = 0,    // Sinergia, Recreation Bonus
    High = 1,        // Highlights de padrões Tier 3-4
    Normal = 2,      // Pop-ups, Highlights Tier 1-2
    Low = 3          // Partículas ambientais
}

// Enfileira eventos e processa sequencialmente
public class VisualQueueSystem : MonoBehaviour
{
    private Queue<VisualEvent> _eventQueue;
    private bool _isProcessing;
    
    public void Enqueue(VisualEvent visualEvent)
    {
        // Ordena por prioridade antes de enfileirar
        // Critical events podem interromper Low priority
    }
    
    private IEnumerator ProcessQueue()
    {
        // Processa sequencialmente com delays configuráveis
        // Agrupa eventos similares (ex: 5 highlights ? 1 burst)
    }
}
```

**Benefícios:**
- Evita explosão visual simultânea
- Agrupa eventos similares (5 highlights ? 1 sequência fluida)
- Jogador consegue processar informação

---

### 2?? Grid Breathing Priority System

**PROBLEMA:** Plant + Harvest + Pattern trigger ao mesmo tempo = animação caótica.

**SOLUÇÃO: Priority Queue na GridBreathingController**
```csharp
private enum ReactionPriority
{
    Pattern = 0,     // Prioridade máxima
    Harvest = 1,     // Média
    Plant = 2        // Baixa
}

private void TriggerReaction(ReactionType type)
{
    // Se já há reação ativa, só sobrescreve se priority maior
    if (_activeReaction != null && type.Priority > _activeReaction.Priority)
    {
        return; // Ignora reação de menor prioridade
    }
    
    // Cancela animação anterior suavemente
    StopCurrentReaction();
    StartCoroutine(PlayReaction(type));
}
```

**Regra:** Pattern > Harvest > Plant. Padrão sempre "vence".

---

### 3?? Decay Visual Threshold

**PROBLEMA:** Muitos padrões com decay ? grid vira "red mess" confuso.

**SOLUÇÃO: Destacar apenas padrões importantes**
```csharp
[Header("Decay Visual Threshold")]
bool onlyHighlightImportantDecay = true;
int importantDecayThreshold = 30;        // Apenas padrões > 30pts
int criticalDecayDaysThreshold = 4;      // Dia 4+ sempre mostra

private bool ShouldShowDecayWarning(PatternMatch match)
{
    if (!onlyHighlightImportantDecay) return match.DaysActive > 1;
    
    // Mostra decay apenas se:
    return match.BaseScore >= importantDecayThreshold ||  // Padrão valioso
           match.DaysActive >= criticalDecayDaysThreshold; // Crítico
}
```

**Benefício:** Foco visual apenas no que importa.

---

### 4?? Object Pooling Centralizado

**PROBLEMA:** Instanciar/Destruir prefabs repetidamente = GC spikes.

**SOLUÇÃO: PatternObjectPool.cs (Singleton)**
```csharp
public class PatternObjectPool : MonoBehaviour
{
    private Dictionary<string, Queue<GameObject>> _pools;
    
    [Header("Pre-Warm")]
    int popupPoolSize = 5;
    int particlePoolSize = 20;
    
    private void Awake()
    {
        // Pre-instancia objetos comuns
        PreWarmPool("PopupText", popupPrefab, popupPoolSize);
        PreWarmPool("Sparkles", sparklesPrefab, particlePoolSize);
    }
    
    public GameObject Get(string poolKey)
    {
        // Retorna objeto ativo ou cria novo se pool vazio
    }
    
    public void Return(string poolKey, GameObject obj)
    {
        // Desativa e retorna ao pool
        obj.SetActive(false);
        _pools[poolKey].Enqueue(obj);
    }
}
```

**Configurável no Inspector:**
- Tamanhos de pool ajustáveis
- Pre-warm opcional (spawn no Awake)
- Auto-grow se pool esvaziar

---

### 5?? Memory Leak Prevention

**PROBLEMA:** Event subscriptions não removidas = memory leaks.

**SOLUÇÃO: Template padrão para TODOS os controllers**
```csharp
public class ExampleController : MonoBehaviour
{
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    private void OnDestroy()
    {
        // Double-check unsubscribe
        UnsubscribeFromEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected += HandlePatterns;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= HandlePatterns;
        }
    }
}
```

**? Checklist:** OnEnable ? Subscribe | OnDisable + OnDestroy ? Unsubscribe

---

### 6?? HUD Visual Hierarchy

**PROBLEMA:** Combo + Pop-ups + Padrões + Sinergia ao mesmo tempo = overload.

**SOLUÇÃO: Escalonamento de Importância**
```
Prioridade Visual (Z-order / Timing):
1. Tabela do Fazendeiro (fullscreen, bloqueia tudo)
2. Sinergia Aura (background layer, não obstrui)
3. Pop-ups de Padrão (texto grande, centro-direita)
4. Combo Counter (discreto, canto)
5. Highlights (grid layer, integrado)
6. Partículas (lowest, decorativo)
```

**Configuração Inspector:**
```csharp
[Header("Sorting Layers")]
int highlightLayer = 5;
int popupLayer = 10;
int comboLayer = 8;
int synergyLayer = 3;
int particleLayer = 1;
```

**Regra:** Elementos que bloqueiam interação (Tabela) têm prioridade absoluta.

---

### 7?? Particle Performance Budget

**PROBLEMA:** Combos + Sinergia + Recreation disparam 100+ partículas = framerate drop.

**SOLUÇÃO: Budget System**
```csharp
[Header("Performance Budget")]
int maxActiveParticles = 50;            // Hard limit
int maxParticlesPerBurst = 20;          // Por evento
float particleLOD = 1.0f;               // 1.0 = full, 0.5 = half

private void SpawnParticles(int amount)
{
    // Respect budget
    int currentActive = CountActiveParticles();
    int available = maxActiveParticles - currentActive;
    int toSpawn = Mathf.Min(amount, available);
    
    // LOD scaling
    toSpawn = Mathf.RoundToInt(toSpawn * particleLOD);
    
    for (int i = 0; i < toSpawn; i++)
    {
        SpawnSingleParticle();
    }
}
```

**Configurável:** Ajustar budget se FPS < 30 (auto-scale opcional).

---

### 8?? Edge Cases - Combo & Sinergia

**EDGE CASE 1:** Combo se mantém por dias sem padrões?
- **FIX:** Combo reseta se `patternCount == 0` por 1 dia completo.

**EDGE CASE 2:** Decay em sinergia múltipla mostra valores errados?
- **FIX:** `PatternScoreTotalResult` já calcula corretamente (ScoreBeforeSynergy vs Total).

**EDGE CASE 3:** Recreation bonus + Decay no mesmo padrão?
- **FIX:** Bonus só aplica no primeiro dia (DaysActive == 1 && HasRecreationBonus).

**EDGE CASE 4:** Grid breathing acumula pulses infinitamente?
- **FIX:** Priority system cancela animação anterior antes de iniciar nova.

---

## ?? DEBUG MODE

**PatternVisualConfig.cs (ScriptableObject centralizado)**
```csharp
[Header("Debug Mode")]
public bool debugMode = false;
public bool showTierColors = true;          // Mostra cor sem animação
public bool logVisualEvents = false;        // Console logs de eventos
public bool disablePooling = false;         // Força instanciar sempre (testar GC)
public bool freezeAnimations = false;       // Pausa animações (testar estados)
public bool showPerformanceMetrics = true;  // FPS, particle count, pool usage

[Header("Visual Overrides (Debug)")]
public bool forceDecayVisual = false;       // Testa Dia 4+ em qualquer padrão
public int forceTierOverride = 0;           // 0 = normal, 1-4 = força tier
```

**Uso:**
- **Cena de teste:** Ativa debugMode ? Spawna padrões com configurações específicas
- **Performance profiling:** Desativa pooling ? Mede impact
- **Visual tuning:** Freeze animations ? Ajusta cores/posições frame-a-frame

---

## ?? FUTURO (Fora de Escopo)

- Click em slot ? Mostra breakdown individual do padrão
- Tradições system integration (já reservamos espaço no HUD)
- Animações específicas por tipo de crop
- Sound effects (reactive audio)
- Mobile optimization (quando necessário)

---

**Versão:** 1.1  
**Última Atualização:** 2026-01-20  
**Status:** Ready for Implementation ??

---

## ?? CHANGELOG

### v1.1 (2026-01-20) - Refinamento Arquitetural
**Adicionado:**
- ? Visual Queue System (priorização de eventos)
- ? Grid Breathing Priority System (Pattern > Harvest > Plant)
- ? Decay Visual Threshold (foco em padrões importantes)
- ? Object Pooling Centralizado (PatternObjectPool)
- ? Memory Leak Prevention (template de subscribe/unsubscribe)
- ? HUD Visual Hierarchy (sorting layers)
- ? Particle Performance Budget (LOD scaling)
- ? Edge Cases documentados e solucionados
- ? Debug Mode (PatternVisualConfig SO)
- ? Sprint 0 (fundação) e Sprint 4 (polish/testing)

**Melhorado:**
- Estrutura de arquivos com pasta Core/ (config, queue, pooling)
- Priorização de implementação (8-12h ao invés de 6-9h)
- Event-driven architecture com checklist de cleanup
- Considerações de performance e robustez

### v1.0 (2026-01-20) - Versão Inicial
- Design completo de 8 features visuais
- HUD layout definido
- Priorização em 3 sprints
