using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using System.Linq;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase 2: Scores Passivos.
    /// Percorre os resultados pré-calculados de scores passivos e dispara eventos visuais.
    /// </summary>
    public class PassiveScoresPhase : IAnalysisPhase
    {
        private readonly GridVisualConfig _config;

        public PassiveScoresPhase(GridVisualConfig config)
        {
            _config = config;
        }

        public string Name => "Passive Scores";

        public async UniTask<PhaseResult> ExecuteAsync(
            AnalysisContext context, 
            IProgress<PhaseProgress> progress, 
            CancellationToken ct)
        {
            if (context.PreCalculatedResult?.PassiveScores == null || context.PreCalculatedResult.PassiveScores.Count == 0)
            {
                return new PhaseResult { Success = true, Message = "No passive scores to process." };
            }

            var passiveResults = context.PreCalculatedResult.PassiveScores;
            int total = passiveResults.Count;
            int scoreDelta = 0;

            for (int i = 0; i < total; i++)
            {
                if (ct.IsCancellationRequested) 
                    throw new OperationCanceledException();

                var passive = passiveResults[i];
                var slotView = context.SlotViews?.FirstOrDefault(v => v.SlotIndex == passive.SlotIndex);

                if (slotView != null)
                {
                    // 🔥 SINCRONIA: Revela o novo sprite (crescimento/morte) agora!
                    context.GridService.ForceVisualRefresh(passive.SlotIndex);

                    // Dispara evento para o VisualHandler (Levitação + Popup)
                    context.Events.Grid.TriggerCropPassiveScore(
                        passive.SlotIndex, 
                        passive.Points, 
                        context.RunningScore + passive.Points, 
                        context.RunData.WeeklyGoalTarget
                    );

                    // Atualiza o score acumulado no contexto
                    scoreDelta += passive.Points;
                    context.RunningScore += passive.Points;

                    // --- Lógica de Aceleração Dinâmica ---
                    // Começa no delay total da config (ou 0.5s) e reduz gradualmente
                    float initialWait = _config != null ? _config.pulseDuration : 0.5f;
                    float minWait = 0.05f; // Hard limit para não ser instantâneo
                    
                    // Fator de aceleração: quanto mais avançamos, mais rápido fica.
                    // i=0 -> wait = initialWait
                    // i=total-1 -> wait = minWait (ou próximo dele)
                    float progressFactor = (float)i / total;
                    float currentWait = Mathf.Lerp(initialWait, minWait, progressFactor);

                    await UniTask.Delay(TimeSpan.FromSeconds(currentWait), cancellationToken: ct);
                }

                progress?.Report(new PhaseProgress
                {
                    PhaseName = Name,
                    Percentage = (float)i / total,
                    CurrentAction = $"Applying passive score: {passive.Points} pts"
                });
            }

            return new PhaseResult
            {
                Success = true,
                ScoreDelta = scoreDelta,
                Message = $"Applied {total} passive scores for a total of {scoreDelta} points."
            };
        }
    }
}
