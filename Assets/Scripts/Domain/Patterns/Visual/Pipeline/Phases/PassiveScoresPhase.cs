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
                    // Dispara evento para o VisualHandler (Levitação + Popup)
                    // Nota: O evento antigo era OnCropPassiveScore(slotIndex, points, newTotal, goal)
                    context.Events.Grid.TriggerCropPassiveScore(
                        passive.SlotIndex, 
                        passive.Points, 
                        context.RunningScore + passive.Points, 
                        context.RunData.WeeklyGoalTarget
                    );

                    // Atualiza o score acumulado no contexto
                    scoreDelta += passive.Points;
                    context.RunningScore += passive.Points;

                    // Aguarda a animação de levitação (configurada centralmente)
                    float waitTime = _config != null ? _config.pulseDuration : 0.5f;
                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: ct);
                }

                progress?.Report(new PhaseProgress
                {
                    PhaseName = Name,
                    Percentage = (float)i / total,
                    CurrentAction = $"Applying passive score: {passive.Points} pts"
                });

                // Pequeno delay entre slots se configurado
                // if (_config.analyzingSlotDelay > 0) ...
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
