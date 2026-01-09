using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoBehaviour, IRunManager
{
    private ISaveManager _saveManager;

    // O estado agora é privado com setter privado para garantir integridade
    private RunPhase _currentPhase;
    public RunPhase CurrentPhase => _currentPhase;

    private const int DAYS_IN_PRODUCTION = 5;
    private const int DAY_WEEKEND_START = 6;
    // Dia 7 seria tecnicamente o domingo, onde ocorre a transição para a próxima semana

    [Header("Configuração de Lojas")]
    [SerializeField] private ShopProfileSO _defaultWeekendShop;
    [SerializeField] private List<ShopProfileSO> _specialShops;

    // --- INICIALIZAÇÃO ---
    public void Initialize(ISaveManager saveManager)
    {
        _saveManager = saveManager;

        // CRUCIAL: Se carregarmos um jogo, precisamos restaurar a Fase correta
        if (IsRunActive)
        {
            RefreshPhaseState(_saveManager.Data.CurrentRun);
        }
    }

    public bool IsRunActive => _saveManager.Data.CurrentRun != null;

    // --- CONTROLE DE FLUXO ---

    public void StartNewRun()
    {
        // 1. Criação Pura de Dados (Domain)
        RunData newRun = RunData.CreateNewRun();

        // 2. Persistência
        _saveManager.Data.CurrentRun = newRun;
        _saveManager.SaveGame();

        // 3. Atualização de Estado Interno
        RefreshPhaseState(newRun);

        // 4. Notificação Global (UI e outros sistemas reagem a isso)
        // Dica: O ideal seria ter um "RunController" ouvindo isso, mas por enquanto mantemos aqui
        AppCore.Instance.Events.Time.TriggerRunStarted();

        // Setup Inicial
        AppCore.Instance.GameStateManager.SetState(GameState.Playing);
        AppCore.Instance.Events.Time.TriggerDayChanged(newRun.CurrentDay);
        AppCore.Instance.Events.UI.TriggerToggleHand(true);
    }

    public void AdvanceDay()
    {
        if (!CanAdvanceDay()) return;

        var run = _saveManager.Data.CurrentRun;

        // Incrementa o dia
        run.CurrentDay++;

        // AVALIA O NOVO DIA E DECIDE A FASE
        // Isso garante que ao sair daqui, a Phase esteja correta para o DailyResolutionSystem checar
        ProcessDayPhase(run);

        _saveManager.SaveGame();
    }

    // --- LÓGICA DE FASE ---

    private void ProcessDayPhase(RunData run)
    {
        // Caso 1: Dias de Trabalho (1 a 5)
        if (run.CurrentDay <= DAYS_IN_PRODUCTION)
        {
            HandleProductionDay(run);
        }
        // Caso 2: Chegou o Sábado (Dia 6)
        else if (run.CurrentDay == DAY_WEEKEND_START)
        {
            StartWeekendPhase(run); // Agora passando o RunData
        }
        // Caso 3: Passou do Fim de Semana (Dia 7 virando Dia 1 da próxima)
        else
        {
            // O jogador clicou em "Próxima Semana" na loja (que chama AdvanceDay)
            // O dia virou 7. Detectamos isso e resetamos para 1 imediatamente.
            StartNextWeek(run);
        }
    }

    private void HandleProductionDay(RunData run)
    {
        _currentPhase = RunPhase.Production;
        AppCore.Instance.Events.Time.TriggerDayChanged(run.CurrentDay);

        // Alerta de Gameplay
        if (run.CurrentDay == DAYS_IN_PRODUCTION)
        {
            Debug.Log(">>> ALERTA: Último dia de colheita da semana! <<<");
        }
    }

    // Agora recebe RunData (Injeção correta)
    public void StartWeekendPhase(RunData run)
    {
        Debug.Log($"Iniciando Fim de Semana (Semana {run.CurrentWeek})");

        _currentPhase = RunPhase.Weekend;

        // Estratégia de Loja
        if (_defaultWeekendShop == null)
        {
            Debug.LogError("[RunManager] ERRO: ShopProfile não configurado.");
            return;
        }

        // Exemplo de lógica futura: if (run.CurrentWeek % 4 == 0) ...
        var strategy = new ConfigurableShopStrategy(_defaultWeekendShop);
        AppCore.Instance.ShopService.OpenShop(strategy);

        // Mudança de Estado Global
        AppCore.Instance.GameStateManager.SetState(GameState.Shopping);
        AppCore.Instance.Events.Time.TriggerWeekendStarted();
        AppCore.Instance.Events.UI.TriggerToggleHand(false);
    }

    public void StartNextWeek(RunData run)
    {
        // 1. Atualização Lógica
        run.CurrentWeek++;
        run.CurrentDay = 1; // Reseta o dia para Segunda-Feira

        Debug.Log($"=== INICIANDO SEMANA {run.CurrentWeek} ===");

        // 2. Atualização de Fase (CRUCIAL PARA O DRAW DE CARTAS)
        _currentPhase = RunPhase.Production;

        // 3. Atualização de Estado Global
        AppCore.Instance.GameStateManager.SetState(GameState.Playing);

        // Notificações
        AppCore.Instance.Events.Time.TriggerWeekChanged(run.CurrentWeek);
        AppCore.Instance.Events.Time.TriggerDayChanged(1);
        AppCore.Instance.Events.UI.TriggerToggleHand(true);

        // AQUI ESTÁ A CORREÇÃO:
        // Como _currentPhase agora é PRODUCTION, quando o DailyResolutionSystem
        // ler essa variável logo após o AdvanceDay(), ele vai entrar no IF e dar as cartas.
    }

    public void EndRun(RunEndReason reason)
    {
        Debug.Log($"Run Finalizada: {reason}");
        _saveManager.Data.CurrentRun = null;
        _saveManager.SaveGame();

        AppCore.Instance.GameStateManager.SetState(GameState.GameOver);
        AppCore.Instance.Events.Time.TriggerRunEnded(reason);
    }

    // Helper para garantir consistência ao carregar o jogo ou iniciar
    private void RefreshPhaseState(RunData run)
    {
        if (run == null) return;

        if (run.CurrentDay <= DAYS_IN_PRODUCTION)
        {
            _currentPhase = RunPhase.Production;
        }
        else
        {
            _currentPhase = RunPhase.Weekend;
        }
        Debug.Log($"[RunManager] Estado Restaurado: {_currentPhase} (Dia {run.CurrentDay})");
    }

    private bool CanAdvanceDay()
    {
        if (!IsRunActive) return false;
        var s = AppCore.Instance.GameStateManager.CurrentState;
        return s == GameState.Playing || s == GameState.Shopping;
    }
}