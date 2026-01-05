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

                if (runData.CurrentLives <= 0) { /* Game Over Logic */ }
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

        // Avança dia numérico (Dia 1 -> 2)
        _runManager.AdvanceDay();

        // Atualiza a referência local (boa prática após alterar dados estruturais)
        var currentRun = _saveManager.Data.CurrentRun;

        // Distribui novas cartas e processa Overflow (Venda automática)
        AppCore.Instance.DailyHandSystem.ProcessDailyDraw(currentRun);

        // Delay para animação das cartas entrando na mão
        yield return new WaitForSeconds(0.8f);

        // -----------------------------------------------------------------------
        // 4. PERSISTÊNCIA E ENCERRAMENTO
        // -----------------------------------------------------------------------

        // Salva tudo (Grid atualizado, Pontos somados, Cartas na mão, Vidas atualizadas)
        _saveManager.SaveGame();

        // Libera o jogo
        _events.Time.TriggerResolutionEnded();
        _isProcessing = false;
    }

    private void OnDisable()
    {
        _isProcessing = false;
    }
}