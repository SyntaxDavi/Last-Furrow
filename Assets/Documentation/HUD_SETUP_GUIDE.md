# HUD SETUP GUIDE - Last Furrow

## ?? ARQUIVOS CRIADOS

```
Assets/Scripts/UI/HUD/
??? Hearts/
?   ??? HeartDisplayManager.cs  ?
?   ??? HeartView.cs             ?
??? DayWeekDisplay.cs            ?
??? SleepButtonController.cs     ?

Assets/Scripts/Debug/
??? CheatManager.cs              ?? REFATORADO

Assets/Documentation/
??? PROJECT_ARCHITECTURE.md      ?? ATUALIZADO
```

---

## ?? INSTRUÇÕES DE SETUP NA UNITY

### 1?? **HEARTS SYSTEM (Sistema de Vidas)**

#### **A. Criar Prefab do Coração**
1. Crie um GameObject vazio: `HeartPrefab`
2. Adicione componente `Image` (UnityEngine.UI)
3. Configure:
   - Color: Vermelho `#FF0000` (será sobrescrito por script)
   - Raycast Target: **OFF** (não precisa detectar clique)
4. Adicione componente `CanvasGroup`
5. Adicione componente `HeartView.cs`
6. **Importante**: Verifique no Inspector os valores:
   - Full Color: Vermelho `(1, 0, 0, 1)`
   - Empty Color: Cinza escuro `(0.3, 0.3, 0.3, 1)`
   - Spawn Duration: `0.3s`
   - Lose Duration: `0.4s`
   - Heal Duration: `0.5s`
7. Salve como Prefab: `Assets/Prefabs/UI/HeartPrefab.prefab`

#### **B. Configurar Manager na Cena**
1. Na hierarquia, localize: `CANVAS ? MainPanel` (ou onde você colocou o quadrado rosa)
2. Crie GameObject filho: `HeartContainer`
3. Configure RectTransform:
   - Anchors: Top-Left `(0, 1)`
   - Pivot: `(0, 1)`
   - Pos X: `20`, Pos Y: `-20` (ajuste conforme layout)
   - Width: `200`, Height: `75`
4. Adicione componente `HeartDisplayManager.cs`
5. Configure no Inspector:
   - Heart Prefab: Arraste `HeartPrefab` aqui
   - Container: Auto-preenchido (ou deixe vazio para usar `this`)
   - **Spacing**: `50` (ajuste no Inspector para espaçamento horizontal)
   - **Spawn Delay**: `0.5s` (ajuste para sequência mais rápida/lenta)
   - Show Debug Logs: `true` (ative para ver logs)

#### **C. Teste**
1. Play na cena
2. Os 3 corações devem aparecer em sequência (pop-up)
3. Abra CheatManager (F1) e teste:
   - "+1 Vida" ? Coração cinza deve ficar vermelho com bounce
   - "-1 Vida" ? Coração direito deve ficar cinza
4. Se não aparecer:
   - Verifique Console por erros
   - Verifique se `RunData.CurrentLives` e `MaxLives` estão corretos
   - Certifique-se que eventos `OnLivesChanged` estão funcionando

---

### 2?? **DAY/WEEK DISPLAY (Dia/Semana)**

#### **A. Localizar TextMeshPro Existente**
1. Na hierarquia: `CANVAS ? TOP-LAYER ? Day/WeekText` (conforme sua screenshot)
2. Certifique-se que tem componente `TextMeshProUGUI`

#### **B. Adicionar Script**
1. Selecione `Day/WeekText`
2. Add Component ? `DayWeekDisplay.cs`
3. Configure no Inspector:
   - **Pulse Duration**: `0.4s`
   - **Pulse Scale**: `1.2` (escala máxima durante animação)
   - **Text Format**: `"Dia {0} - Semana {1}"` (já preenchido)
   - Show Debug Logs: `true` (para ver atualizações)

#### **C. Teste**
1. Play na cena
2. Texto deve aparecer: `"Dia 1 - Semana 1"`
3. Use CheatManager (F1) ou botão Sleep para avançar dia
4. Ao mudar, texto deve fazer pulse/bounce

---

### 3?? **SLEEP BUTTON (Botão Dormir)**

#### **A. Localizar Botão Existente**
1. Na hierarquia: `CANVAS ? NextDayButton` ou `SleepButton` (conforme sua print)
2. Certifique-se que tem:
   - Componente `Button` (UnityEngine.UI)
   - TextMeshProUGUI filho com o texto

#### **B. Adicionar Script**
1. Selecione o botão
2. Add Component ? `SleepButtonController.cs`
3. Configure no Inspector:
   - **Button Text**: Arraste o `TextMeshProUGUI` filho aqui
   - **Normal Text**: `"Sleep"`
   - **Processing Text**: `"Sleeping..."`
   - **Disable On Weekend**: `true` ? (desabilita Dia 6-7)
   - Show Debug Logs: `true`

#### **C. Teste**
1. Play na cena
2. Clique no botão "Sleep"
3. Deve:
   - Texto mudar para "Sleeping..."
   - Grid começar a processar (GrowGridStep)
   - Dia avançar
   - Botão voltar a "Sleep"
4. No Dia 6 (fim de semana):
   - Botão deve ficar desabilitado
   - Use "Trabalhar" no Shop para avançar semana

---

### 4?? **CHEAT MANAGER (F1)**

#### **A. Já Configurado na Cena**
1. Na hierarquia: Deve ter um GameObject com `CheatManager.cs`
2. Se não tiver, crie:
   - GameObject vazio: `CheatManager`
   - Add Component: `CheatManager.cs`

#### **B. Configurar (Opcional)**
- **Toggle Key**: `F1` (padrão)
- **UI Position**: `X:20, Y:200` (lateral esquerda, meio)
- **Money Amount**: `100` (valor padrão)
- **Quick Spawn Card IDs**: 
  ```
  card_corn
  card_water
  card_fertilizer
  ```

#### **C. Usar**
1. Play na cena
2. Pressione **F1** ? Menu aparece na lateral esquerda
3. Funcionalidades:
   - ?? **Add Money**: Digite valor customizado + clique
   - ? **+1 Vida / -1 Vida**: Testa sistema de corações
   - ?? **Desbloquear/Limpar/Regar/Amadurecer Grid**
   - ?? **Spawn Card**: Adiciona carta específica na mão
   - ?? **Deletar Save**: Reseta jogo imediatamente

---

## ?? TROUBLESHOOTING

### Corações não aparecem
```
? Verifique Console: "[HeartDisplayManager] ? Inicializado. Lives: X/3"
? Certifique-se que HeartPrefab tem HeartView.cs
? Verifique se RunData.CurrentLives > 0
? Teste com CheatManager: +1 Vida deve forçar refresh
```

### Dia/Semana não atualiza
```
? Verifique Console: "[DayWeekDisplay] ? Inicializado. Dia X, Semana Y"
? Certifique-se que TimeEvents.OnDayChanged está sendo disparado
? Teste avançando dia com Sleep Button ou CheatManager
```

### Sleep Button não funciona
```
? Verifique se está em GameState.Playing (não Shopping)
? Verifique se não é fim de semana (Dia 6-7)
? Console deve mostrar: "[SleepButtonController] Iniciando ciclo..."
? Se nada acontecer, verifique DailyResolutionSystem.StartEndDaySequence()
```

### CheatManager não abre (F1)
```
? Certifique-se que está em UNITY_EDITOR ou DEVELOPMENT_BUILD
? Verifique se tecla não está sendo usada por outro script
? Verifique se GameObject com CheatManager está ativo na hierarquia
```

---

## ?? CUSTOMIZAÇÃO VISUAL

### Cores dos Corações
Edite em `HeartView.cs` (Inspector ou código):
```csharp
Full Color: (1, 0, 0, 1)     // Vermelho
Empty Color: (0.3, 0.3, 0.3, 1) // Cinza escuro
```

### Espaçamento dos Corações
Ajuste em `HeartDisplayManager` (Inspector):
```
Spacing: 50  // Pixels entre cada coração
```

### Animações
Ajuste durações em `HeartView` (Inspector):
```
Spawn Duration: 0.3s
Lose Duration: 0.4s
Heal Duration: 0.5s
```

### Formato do Texto Dia/Semana
Edite em `DayWeekDisplay` (Inspector):
```
Text Format: "Dia {0} - Semana {1}"
Alternativas:
- "D{0} S{1}"
- "Week {1}, Day {0}"
- "S{1} | D{0}"
```

---

## ? CHECKLIST FINAL

- [ ] Corações aparecem em sequência ao iniciar jogo
- [ ] Perder vida: Coração direito fica cinza
- [ ] Ganhar vida: Coração cinza fica vermelho com bounce
- [ ] Texto Dia/Semana atualiza e faz pulse ao mudar
- [ ] Botão Sleep avança o dia e mostra "Sleeping..."
- [ ] Botão Sleep desabilita no fim de semana (Dia 6-7)
- [ ] CheatManager abre com F1
- [ ] Todas funcionalidades do CheatManager funcionam
- [ ] Logs de debug aparecem no Console (se habilitados)

---

## ?? PRÓXIMOS PASSOS

1. **Sprites**: Substituir Image genérica por sprite de coração
2. **Animações**: Adicionar efeito de "quebrar" ao perder vida (partículas)
3. **SFX**: Sons para ganhar/perder vida, pulse do dia
4. **Localization**: Suporte para múltiplos idiomas no texto
5. **Expandir MaxLives**: Testar cartas que aumentam vida máxima

---

## ?? DOCUMENTAÇÃO

Consulte `PROJECT_ARCHITECTURE.md` seção **"E. Visuals / UI (Views)"** para detalhes da arquitetura.
