# ?? Plano de Implementação: Main Menu

> **Branch:** `feature/main-menu-implementation`  
> **Autor:** Copilot + Dev  
> **Status:** ?? Planejamento

---

## ?? Análise da Arquitetura Atual

### Padrões Identificados

| Aspecto | Padrão Usado | Localização |
|---------|-------------|-------------|
| **Entry Point** | Singleton `AppCore` + DontDestroyOnLoad | `App/AppCore.cs` |
| **Service Locator** | `ServiceRegistry` centralizado | `App/ServiceRegistry.cs` |
| **State Machine** | `GameStateManager` com enum `GameState` | `App/GameStateManager.cs` |
| **UI Pattern** | `UIView` base class (Show/Hide/Fade) | `UI/View/UIView.cs` |
| **Modular Init** | `BaseModule` + módulos específicos | `App/Modules/` |
| **Event System** | `GameEvents` com canais separados | `Infrastructure/Events/` |

### Estados de Jogo Existentes
```csharp
public enum GameState
{
    Initialization,
    MainMenu,        // ? JÁ EXISTE - apenas não implementado
    Playing,
    Shopping,
    Paused,
    GameOver,
    Analyzing
}
```

### Fluxo Atual (Sem Main Menu)
```
AppCore.Awake() ? InitializeModularArchitecture() 
                ? GameStateManager.Initialize() ? SetState(MainMenu)
                ? [Imediatamente] GameplayBootstrapper ? SetState(Playing)
```

---

## ?? Objetivos da Implementação

### MVP (Primeira Build de Teste)
- [x] Tela de Menu com UI básica
- [x] Botão "Novo Jogo" funcional
- [x] Botão "Continuar" (se houver save)
- [x] Botão "Sair" 
- [ ] Background simples (estático ou animado leve)
- [ ] Transição suave Menu ? Gameplay

### Futuro (Pós-Teste)
- [ ] Botão "Configurações" (Volume, Resolução)
- [ ] Botão "Créditos"
- [ ] Tutorial/Onboarding
- [ ] Seleção de Dificuldade
- [ ] Perfis de Save múltiplos

---

## ??? Arquitetura Proposta

### Princípio: **Respeitar os Padrões Existentes**

O projeto já possui uma arquitetura sólida. O Main Menu será implementado como:

1. **Nova Scene** (`MainMenu`) - Separação clara de responsabilidades
2. **MainMenuView** - Herda de `UIView`, mantendo consistência
3. **MainMenuController** - Orquestra ações sem lógica de negócio
4. **Uso do `GameStateManager`** - Estado `MainMenu` já existe

### Estrutura de Pastas Proposta

```
Assets/
??? Scenes/
?   ??? MainMenu.unity        # ?? Nova cena
?   ??? Game.unity            # Existente (gameplay)
?
??? Scripts/
?   ??? App/
?   ?   ??? AppCore.cs        # ?? Pequenas modificações
?   ?   ??? Modules/
?   ?       ??? MainMenuModule.cs  # ?? (Opcional)
?   ?
?   ??? UI/
?       ??? MainMenu/         # ?? Nova pasta
?           ??? MainMenuView.cs
?           ??? MainMenuController.cs
?           ??? MainMenuButtonConfig.cs  # (Opcional - ScriptableObject)
?
??? Prefabs/
?   ??? UI/
?       ??? MainMenu/         # ?? Nova pasta
?           ??? MainMenuCanvas.prefab
?           ??? MenuButton.prefab
?
??? Audio/
    ??? Music/
        ??? MainMenuTheme.wav  # ?? (Opcional para MVP)
```

---

## ?? Plano de Implementação Detalhado

### Fase 1: Infraestrutura Base

#### 1.1 Criar Cena `MainMenu`
```
Hierarquia:
- MainMenuRoot (Empty)
  ??? MainMenuCanvas (Canvas)
  ?   ??? Background (Image)
  ?   ??? TitleText (TextMeshPro)
  ?   ??? ButtonContainer (VerticalLayoutGroup)
  ?   ?   ??? NewGameButton
  ?   ?   ??? ContinueButton
  ?   ?   ??? QuitButton
  ?   ??? VersionText (TextMeshPro - canto inferior)
  ?
  ??? MainMenuController (MonoBehaviour)
  ??? EventSystem
```

#### 1.2 Modificar `AppCore.cs`

**Mudança necessária:** O `AppCore` precisa saber qual cena carregar primeiro.

```csharp
// Adicionar campo serializado
[Header("Scene Config")]
[SerializeField] private string _mainMenuSceneName = "MainMenu";
[SerializeField] private string _gameplaySceneName = "Game";

// Novo método público
public void LoadMainMenu()
{
    GameStateManager.SetState(GameState.MainMenu);
    SceneManager.LoadScene(_mainMenuSceneName);
}

public void LoadGameplay()
{
    SceneManager.LoadScene(_gameplaySceneName);
}
```

#### 1.3 Modificar Build Settings
- Adicionar `MainMenu` como Scene 0 (primeira a carregar)
- `Game` como Scene 1

---

### Fase 2: Scripts do Main Menu

#### 2.1 `MainMenuView.cs`

```csharp
// Herda UIView para consistência com o resto do projeto
public class MainMenuView : UIView
{
    [Header("Botões")]
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _quitButton;
    
    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _versionText;
    
    // Eventos para o Controller ouvir
    public event Action OnNewGameClicked;
    public event Action OnContinueClicked;
    public event Action OnQuitClicked;
    
    // Controla estado visual do botão Continue
    public void SetContinueAvailable(bool available);
    
    // Versão automática do build
    public void SetVersion(string version);
}
```

#### 2.2 `MainMenuController.cs`

```csharp
// Controller orquestra, não contém lógica de negócio
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private MainMenuView _view;
    
    private void Start()
    {
        // Verifica se há save existente
        bool hasSave = AppCore.Instance.SaveManager.Data?.CurrentRun != null;
        _view.SetContinueAvailable(hasSave);
        
        // Wiring de eventos
        _view.OnNewGameClicked += HandleNewGame;
        _view.OnContinueClicked += HandleContinue;
        _view.OnQuitClicked += HandleQuit;
        
        // Garante estado correto
        AppCore.Instance.GameStateManager.SetState(GameState.MainMenu);
    }
    
    private void HandleNewGame()
    {
        // Limpa save anterior (se existir) e inicia run nova
        AppCore.Instance.SaveManager.ClearCurrentRun();
        AppCore.Instance.RunManager.StartNewRun();
        AppCore.Instance.LoadGameplay();
    }
    
    private void HandleContinue()
    {
        // Apenas carrega a cena - RunData já existe no SaveManager
        AppCore.Instance.LoadGameplay();
    }
    
    private void HandleQuit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
```

---

### Fase 3: Integrações

#### 3.1 Retorno ao Menu (Game Over / Pause)

O `GameOverView.cs` já tem `OnMainMenuClicked()` que chama `AppCore.Instance.ReturnToMainMenu()`.

**Verificar/Implementar em `AppCore.cs`:**
```csharp
public void ReturnToMainMenu()
{
    // Limpa estado de gameplay
    UnregisterGridService();
    
    // Reseta sistemas visuais
    GameStateManager.SetState(GameState.MainMenu);
    
    // Carrega cena do menu
    SceneManager.LoadScene(_mainMenuSceneName);
}
```

#### 3.2 Áudio (Opcional para MVP)

O `AudioManager` já suporta contextos de música:
```csharp
// No MainMenuController.Start():
AppCore.Instance.AudioManager.SetMusicContext(_menuTheme, shouldBePlaying: true);
```

#### 3.3 Transições Suaves (Opcional)

Usar o pattern existente de `ScreenFadeStep.cs` para transições:
- Fade out ao sair do menu
- Fade in ao entrar no gameplay

---

## ?? Pontos de Atenção

### 1. Race Conditions no Carregamento
O `GameplayBootstrapper` assume que `AppCore.Instance` existe. Com duas cenas:
- `AppCore` deve estar em uma cena que persiste (DontDestroyOnLoad ?)
- Ou deve existir em ambas as cenas com lógica de singleton

**Recomendação:** Manter `AppCore` em um Prefab na pasta `Resources` e usar `[RuntimeInitializeOnLoadMethod]` para garantir que exista antes de qualquer cena.

### 2. SaveManager e Dados de Teste
- Verificar se `SaveManager.Data` retorna null corretamente quando não há save
- Implementar `SaveManager.ClearCurrentRun()` se não existir

### 3. TimeScale
O `GameStateManager.HandleTimeScale()` já pausa o tempo no `MainMenu`:
```csharp
if (state == GameState.Paused || state == GameState.MainMenu)
    Time.timeScale = 0f;
```
**Atenção:** Animações no menu precisam usar `Time.unscaledDeltaTime`.

### 4. Input no Menu
O `InputManager` pode estar capturando inputs de gameplay. Verificar se:
- Input do menu é separado (UI EventSystem)
- Ou `InputManager` respeita o `GameState.MainMenu`

---

## ?? Checklist de Testes

### Fluxo Básico
- [ ] Abrir o jogo ? Menu aparece
- [ ] Clicar "Novo Jogo" ? Gameplay inicia
- [ ] Clicar "Continuar" (com save) ? Gameplay restaura estado
- [ ] Clicar "Continuar" (sem save) ? Botão desabilitado/invisível
- [ ] Clicar "Sair" ? Aplicação fecha

### Transições
- [ ] Menu ? Gameplay: Sem erros de console
- [ ] Gameplay ? Menu (Game Over): Estado limpo
- [ ] Gameplay ? Menu (Pause): Estado preservado

### Edge Cases
- [ ] Abrir menu, esperar 5min, clicar play ? Funciona
- [ ] Iniciar jogo, morrer, voltar menu, novo jogo ? Sem dados antigos
- [ ] Fechar jogo durante gameplay, reabrir ? Menu com "Continuar" disponível

---

## ?? Estimativa de Tempo

| Tarefa | Tempo Estimado |
|--------|----------------|
| Criar cena e hierarquia UI | 1-2h |
| `MainMenuView.cs` | 30min |
| `MainMenuController.cs` | 30min |
| Modificações no `AppCore` | 30min |
| Wiring e teste básico | 1h |
| Polish visual básico | 1-2h |
| **Total MVP** | **4-6h** |

---

## ?? Próximos Passos Imediatos

1. **Criar a cena `MainMenu.unity`** com hierarquia básica
2. **Implementar `MainMenuView.cs`** seguindo o padrão `UIView`
3. **Implementar `MainMenuController.cs`** com wiring de eventos
4. **Modificar `AppCore.cs`** com métodos de navegação
5. **Testar fluxo completo** Menu ? Game ? Menu
6. **Ajustar Build Settings** com ordem correta de cenas

---

## ?? Referências do Projeto

- **Padrão UIView:** `Assets/Scripts/UI/View/UIView.cs`
- **Exemplo de View:** `Assets/Scripts/UI/View/GameOverView.cs`
- **Event System:** `Assets/Scripts/Infrastructure/Events/GameEvents.cs`
- **State Machine:** `Assets/Scripts/App/GameStateManager.cs`
- **Entry Point:** `Assets/Scripts/App/AppCore.cs`

---

*Documento criado para manter consistência arquitetural e servir como guia durante a implementação.*
