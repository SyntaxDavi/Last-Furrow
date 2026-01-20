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
? ??????                              ????????????????????   ?
?                                      ? Meta: 115 / 500  ?   ?
?                                      ? Dia 5 - Semana 1 ?   ?
?                                      ? $10              ?   ?
?                                      ????????????????????   ?
?                                                              ?
?           ???????????????????????    ????????????????????  ?
?           ?                     ?    ? Padrões: +0      ?  ? ? ÁREA POP-UPS
?           ?      GRID 5x5       ?    ? (0 detectados)   ?  ?
?           ?                     ?    ?                  ?  ?
?           ?     (Centro)        ?    ? [ESPAÇO LIVRE]   ?  ? ? Tradições (futuro)
?           ?                     ?    ?                  ?  ?
?           ???????????????????????    ????????????????????  ?
?                                                              ?
?                    ???????????????                          ?
?                    ?   [Cartas]  ?                          ?
?                    ???????????????                          ?
?  [??]              [Shuffle] [Sleep]                        ?
???????????????????????????????????????????????????????????????
```

### Nova Organização Visual
```
???????????????????????????????????????????????????????????????
? ?????? [Combo x3 ??]              ????????????????????   ? ? Combo aparece aqui
?                                      ? Meta: 115 / 500  ?   ?
?                                      ? Dia 5 - Semana 1 ?   ?
?                                      ? $10              ?   ?
?                                      ????????????????????   ?
?                                                              ?
?           ???????????????????????    ????????????????????  ?
?           ?  [? LINHA! ?]     ?    ? Padrões: +45     ?  ? ? Pop-up aqui
?           ?      GRID 5x5       ?    ? (3 detectados)   ?  ?
?           ?   (Com Highlight)   ?    ?                  ?  ?
?           ?   [Aura Sinergia]   ?    ? [Tradições]      ?  ? ? Futuro
?           ?                     ?    ?                  ?  ?
?           ???????????????????????    ????????????????????  ?
?                 ?                                            ?
?          [Partículas]                                        ?
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
??? PatternHighlightController.cs      ? Highlights de slots
??? PatternTextPopupController.cs      ? Pop-ups "LINHA!"
??? GridBreathingController.cs         ? Animação do grid
??? FarmerDiaryPanel.cs                ? Tabela do fazendeiro
??? PatternComboCounter.cs             ? Combo display
??? PatternSynergyVisual.cs            ? Explosion + aura
??? PatternParticleManager.cs          ? Gerenciador de partículas
```

### Event-Driven (SOLID)
Todos os componentes escutam `PatternEvents`:
- `OnPatternsDetected(matches, totalPoints)` ? Trigger visuals
- `OnPatternDecayApplied(match, days, multiplier)` ? Decay warning
- `OnPatternRecreated(match)` ? Recreation celebration

**Nenhum acoplamento direto entre componentes!**

---

## ?? PRIORIZAÇÃO DE IMPLEMENTAÇÃO

### Sprint 1 - Core Juice (2-3 horas)
1. ? **PatternHighlightController** - Impacto visual máximo
2. ? **PatternTextPopupController** - Clareza de feedback
3. ? **GridBreathingController** - Vida ao grid

### Sprint 2 - Information Clarity (2-3 horas)
4. ? **FarmerDiaryPanel** - Tabela do fazendeiro (new flow)
5. ? **Decay Warning Visual** - Integrar com highlights

### Sprint 3 - Polish & Special FX (2-3 horas)
6. ? **PatternComboCounter** - Streak tracking
7. ? **PatternSynergyVisual** - Explosion + aura
8. ? **PatternParticleManager** - Sparkles stylized

**Total estimado:** 6-9 horas de implementação

---

## ?? PRINCÍPIOS DE DESIGN

1. **Customizável** - Tudo ajustável no Inspector
2. **Performático** - Pooling de objetos, LOD de partículas
3. **Modular** - Cada feature pode ser desabilitada individualmente
4. **Responsivo** - Feedback imediato a ações do jogador
5. **Educativo** - Jogador aprende mecânicas através do visual
6. **Satisfatório** - "Game feel" AAA mesmo sendo indie

---

## ?? FUTURO (Fora de Escopo)

- Click em slot ? Mostra breakdown individual do padrão
- Tradições system integration (já reservamos espaço no HUD)
- Animações específicas por tipo de crop
- Sound effects (reactive audio)
- Mobile optimization (quando necessário)

---

**Versão:** 1.0  
**Última Atualização:** 2026-01-20  
**Status:** Ready for Implementation ??
