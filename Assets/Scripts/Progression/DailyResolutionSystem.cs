using UnityEngine;
using System.Collections;

public class DailyResolutionSystem : MonoBehaviour
{
    [Header("Configuração Visual")]
    [SerializeField] private float _baseDelayPerSlot = 0.3f;
    [SerializeField] private float _fastDelayPerSlot = 0.05f; 

    // --- ESTADO INTERNO ---
    private bool _isProcessing = false;

    // --- DEPENDÊNCIAS CACHEADAS ---
    private RunManager _runManager;
    private SaveManager _saveManager;
    private InputManager _inputManager;
    private GameEvents _events;

    // Flag de segurança
    private bool _isInitialized = false;

    // Chamado pelo AppCore no boot
    public void Initialize()
    {
        if (AppCore.Instance != null)
        {
            _runManager = AppCore.Instance.RunManager;
            _saveManager = AppCore.Instance.SaveManager;
            _inputManager = AppCore.Instance.InputManager;
            _events = AppCore.Instance.Events;

            _isInitialized = true;
        }
        else
        {
            Debug.LogError("[DailyResolution] AppCore não encontrado na inicialização!");
        }
    }

    public void StartEndDaySequence()
    {
        // 1. Segurança contra inicialização esquecida
        if (!_isInitialized)
        {
            Debug.LogWarning("[DailyResolution] Não inicializado! Tentando inicializar agora...");
            Initialize();

            if (!_isInitialized)
            {
                Debug.LogError("[DailyResolution] Falha fatal: Dependências não resolvidas.");
                return;
            }
        }

        // 2. Proteção contra execução dupla
        if (_isProcessing) return;

        // 3. Validação de dados da Run
        if (_runManager == null || _saveManager.Data.CurrentRun == null)
        {
            Debug.LogError("[DailyResolution] RunData inexistente.");
            return;
        }

        StartCoroutine(ResolveDayRoutine());
    }

    private IEnumerator ResolveDayRoutine()
    {
        _isProcessing = true;

        // Bloqueia interações e avisa UI
        _events.Time.TriggerResolutionStarted();

        IGridService gridService = AppCore.Instance.GetGridLogic();
        var runData = _saveManager.Data.CurrentRun;

        // -----------------------------------------------------------------------
        // 1. LÓGICA VISUAL DO GRID (Crescimento / Morte)
        // -----------------------------------------------------------------------
        for (int i = 0; i < runData.GridSlots.Length; i++)
        {
            _events.Grid.TriggerAnalyzeSlot(i);

            // O GridService processa a lógica biológica da planta
            gridService.ProcessNightCycleForSlot(i);

            // Controle de ritmo para o jogador ver o que aconteceu
            float delay = _inputManager.IsPrimaryButtonHeld ? _fastDelayPerSlot : _baseDelayPerSlot;
            yield return new WaitForSeconds(delay);
        }

        // Pequena pausa após terminar o grid
        yield return new WaitForSeconds(0.5f);

        // -----------------------------------------------------------------------
        // 1.5. VERIFICAÇÃO DE COLAPSO (GDD: Morte por Sufocamento > 80%)
        // -----------------------------------------------------------------------
        float contamination = gridService.GetGridContaminationPercentage();
        if (contamination >= 0.8f)
        {
            Debug.LogError($"GAME OVER: Colapso do Grid ({contamination * 100:F0}% contaminado).");

            // Encerra a run. Isso dispara o evento OnRunEnded -> UIManager -> GameOverView
            // Certifique-se que RunEndReason.WitheredOverload existe no seu Enum
            AppCore.Instance.RunManager.EndRun(RunEndReason.WitheredOverload);

            _isProcessing = false; // Garante que a flag seja liberada
            yield break; // Interrompe o resto do dia imediatamente
        }

        // -----------------------------------------------------------------------
        // 2. PONTUAÇÃO E METAS (Weekly Goal System)
        // -----------------------------------------------------------------------

        // Passo A: Calcular Pontos Passivos da Noite (baseado no Grid já processado)
        AppCore.Instance.WeeklyGoalSystem.ProcessNightlyScoring(runData);

        // Passo B: Verificar se a Semana acabou e obter o Relatório
        // O CheckEndOfWeek NÃO aplica dano, apenas retorna o resultado (Struct)
        WeekEvaluationResult weekResult = AppCore.Instance.WeeklyGoalSystem.CheckEndOfProduction(runData);

        if (weekResult.IsWeekEnd) // Isso significa "Fim do ciclo de produção"
        {
            Debug.Log($"[DailyResolution] Fim da Produção (Dia {runData.CurrentDay})! Avaliando Meta...");

            if (weekResult.IsSuccess)
            {
                Debug.Log("<color=green>META BATIDA!</color>");
            }
            else
            {
                runData.CurrentLives--;
                Debug.Log($"<color=red>META FALHOU! -1 Vida.</color>");
                _events.Progression.TriggerLivesChanged(runData.CurrentLives);

                if (runData.CurrentLives <= 0)
                {
                    // Game Over por falta de vidas (pode ser tratado aqui ou via evento)
                    /* Game Over Logic */
                }
            }
            // Zeramos a pontuação AGORA, pois entramos no fim de semana.
            runData.CurrentWeeklyScore = 0;

            // Mas mantemos a meta VISUALMENTE antiga ou mostramos a nova?
            // Geralmente no fim de semana mostramos "Próxima Meta: X"
            runData.WeeklyGoalTarget = weekResult.NextGoal;

            // Importante: Não incrementamos runData.CurrentWeek aqui se o RunManager já faz isso.
            // Mas atualizamos os valores de meta.

            _events.Progression.TriggerScoreUpdated(0, runData.WeeklyGoalTarget);
            _events.Progression.TriggerWeeklyGoalEvaluated(weekResult.IsSuccess, runData.CurrentLives);

            yield return new WaitForSeconds(2.0f);
        }

        // -----------------------------------------------------------------------
        // 3. AVANÇO DE DIA E CARTAS
        // -----------------------------------------------------------------------

        // 1. Avança o dia numérico (Ex: 5 -> 6, ou 7 -> 1)
        // O AdvanceDay() também gerencia a troca de Fase (Production <-> Weekend) internamente.
        _runManager.AdvanceDay();

        // Pega a referência ATUALIZADA dos dados da Run
        var currentRun = _saveManager.Data.CurrentRun;

        // --- A LÓGICA DE CONTROLE DE CARTAS ---
        // Verificamos a Fase APÓS avançar o dia. Se for dias de PRODUÇÃO, sim, compramos cartas.
        if (_runManager.CurrentPhase == RunPhase.Production)
        {
            Debug.Log("[DailyResolution] Dia de Produção: Iniciando Draw Diário...");

            // Processa o Draw e o Overflow para as cartas da mão
            AppCore.Instance.DailyHandSystem.ProcessDailyDraw(currentRun);

            // Delay para a animação visual das cartas entrando (Fan Out)
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            // Se a fase é Weekend, apenas logamos que não haverá draw.
            Debug.Log("[DailyResolution] Fim de Semana: Sem draw de novas cartas.");
            // Um pequeno delay para não pular direto para o save, dando um respiro visual
            yield return new WaitForSeconds(0.5f);
        }

        // -----------------------------------------------------------------------
        // 4. PERSISTÊNCIA E ENCERRAMENTO
        // -----------------------------------------------------------------------

        // Salva o estado atualizado do jogo
        _saveManager.SaveGame();

        // Libera o fluxo visual e de jogo
        _events.Time.TriggerResolutionEnded();
        _isProcessing = false;

        Debug.Log("=== Resolução do Dia Concluída ===");
    }

    private void OnDisable()
    {
        _isProcessing = false;
    }
}