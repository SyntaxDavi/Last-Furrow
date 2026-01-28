using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase 3: Análise de Padrões.
    /// Percorre os padrões detectados, incrementa score e dispara popups.
    /// </summary>
    public class PatternAnalysisPhase : IAnalysisPhase
    {
        private readonly PatternUIManager _uiManager;

        public PatternAnalysisPhase(PatternUIManager uiManager)
        {
            _uiManager = uiManager;
        }

        public string Name => "Pattern Analysis";

        public async UniTask<PhaseResult> ExecuteAsync(
            AnalysisContext context, 
            IProgress<PhaseProgress> progress, 
            CancellationToken ct)
        {
            if (context.PreCalculatedResult?.PatternMatches == null || context.PreCalculatedResult.PatternMatches.Count == 0)
            {
                return new PhaseResult { Success = true, Message = "No patterns detected." };
            }

            var matches = context.PreCalculatedResult.PatternMatches;
            int total = matches.Count;
            int scoreDelta = 0;

            for (int i = 0; i < total; i++)
            {
                if (ct.IsCancellationRequested) 
                    throw new OperationCanceledException();

                var match = matches[i];

                // 1. Atualiza Score
                scoreDelta += match.BaseScore;
                context.RunningScore += match.BaseScore;

                // 2. Dispara evento HUD
                context.Events.Pattern.TriggerScoreIncremented(
                    match.BaseScore,
                    context.RunningScore,
                    context.RunData.WeeklyGoalTarget
                );

                // 3. Dispara Highlights nos slots
                context.Events.Pattern.TriggerPatternSlotCompleted(match);

                // 4. Mostra Popup (Awaiting se necessário)
                if (_uiManager != null)
                {
                    await _uiManager.ShowPatternPopupRoutine(match).AttachExternalCancellation(ct);
                }

                // 5. Efeitos de Decay/Recreation (Apenas visual/log via eventos)
                if (match.DaysActive > 1)
                {
                    float decayMultiplier = Mathf.Pow(0.9f, match.DaysActive - 1);
                    context.Events.Pattern.TriggerPatternDecayApplied(match, match.DaysActive, decayMultiplier);
                }

                if (match.HasRecreationBonus)
                {
                    context.Events.Pattern.TriggerPatternRecreated(match);
                }

                progress?.Report(new PhaseProgress
                {
                    PhaseName = Name,
                    Percentage = (float)i / total,
                    CurrentAction = $"Pattern completed: {match.DisplayName} (+{match.BaseScore} pts)"
                });
            }

            return new PhaseResult
            {
                Success = true,
                ScoreDelta = scoreDelta,
                Message = $"Processed {total} patterns for a total of {scoreDelta} points."
            };
        }
    }
}
